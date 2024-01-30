using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.EventSourcing.Aggregates;
using Insperex.EventHorizon.EventStore.Interfaces;
using Insperex.EventHorizon.EventStore.Interfaces.Factory;
using Insperex.EventHorizon.EventStore.Interfaces.Stores;
using Insperex.EventHorizon.EventStore.Models;
using Insperex.EventHorizon.EventStreaming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Insperex.EventHorizon.EventSourcing.AggregateWorkflow
{
    public class AggregateWorkflow<TWrapper, T>
        where TWrapper : class, IStateWrapper<T>, new()
        where T : class, IState
    {
        private readonly ICrudStore<Snapshot<T>> _crudStore;
        private readonly Aggregator<TWrapper, T> _aggregator;
        private readonly StreamingClient _streamingClient;
        private readonly ILogger<AggregateWorkflow<TWrapper, T>> _logger;
        public AggregateWorkflowConfig<TWrapper, T> Config { get; }

        public AggregateWorkflow(IServiceProvider serviceProvider, AggregateWorkflowConfig<TWrapper, T> config)
        {
            _crudStore = serviceProvider.GetRequiredService<ISnapshotStoreFactory<T>>().GetSnapshotStore();
            _streamingClient = serviceProvider.GetRequiredService<StreamingClient>();
            _logger = serviceProvider.GetRequiredService<ILogger<AggregateWorkflow<TWrapper, T>>>();

            // Aggregator
            var aggregatorBuilder = serviceProvider.GetRequiredService<AggregatorBuilder<TWrapper, T>>();
            _aggregator = aggregatorBuilder.Build();

            Config = config;
        }

        public async Task RebuildAllAsync(CancellationToken ct)
        {
            var minDateTime = await _crudStore.GetLastUpdatedDateAsync(ct);

            // NOTE: return with one ms forward because mongodb rounds to one ms
            minDateTime = minDateTime == default? minDateTime : minDateTime.AddMilliseconds(1);

            var reader = _streamingClient.CreateReader<Event>().AddStream<T>().StartDateTime(minDateTime).Build();

            while (!ct.IsCancellationRequested)
            {
                var messages = await reader.GetNextAsync(1000);
                if (!messages.Any()) break;

                // Load Aggregates
                var streamIds = messages.Select(x => x.Data.StreamId).Distinct().ToArray();
                var aggDict = await _aggregator.GetAggregatesFromStatesAsync(streamIds, ct);

                // Apply Events
                foreach (var message in messages)
                    aggDict[message.Data.StreamId].Apply(message.Data);

                // Save Changes
                await _aggregator.SaveAllAsync(aggDict);
            }
        }

        public async Task<KeyValuePair<string, Aggregate<T>>> ConsumeAsync<TMessage>(TMessage message, CancellationToken ct) where TMessage : ITopicMessage
        {
            var responses = await ConsumeAsync(new[] { message }, ct);
            return responses.FirstOrDefault();
        }
        public async Task<Dictionary<string, Aggregate<T>>> ConsumeAsync<TMessage>(TMessage[] messages, CancellationToken ct) where TMessage : ITopicMessage
        {
            var aggregateDict = await LoadAsync(messages, ct);

            // Map/Apply Changes
            TriggerHandle(messages, aggregateDict);

            // Save Snapshots and Publish Events
            await SaveAsync(aggregateDict);

            return aggregateDict;
        }

        public Task TryPublishResponsesAsync(Dictionary<string, Aggregate<T>> aggregateDict)
        {
            var responses = aggregateDict.Values.SelectMany(x => x.Responses).ToArray();
            return responses.Any() ? _aggregator.PublishResponseAsync(responses) : Task.CompletedTask;
        }

        private async Task<Dictionary<string, Aggregate<T>>> LoadAsync<TMessage>(TMessage[] messages, CancellationToken ct) where TMessage : ITopicMessage
        {
            // Load Aggregate
            var streamIds = messages.Select(x => x.StreamId).Distinct().ToArray();
            var aggregateDict = await _aggregator.GetAggregatesFromStatesAsync(streamIds, ct);

            // OnLoad Hook
            SafeHook(() => Config.Middleware?.OnLoad(aggregateDict), aggregateDict);

            return aggregateDict;
        }

        private void TriggerHandle<TMessage>(TMessage[] messages, Dictionary<string, Aggregate<T>> aggregateDict) where TMessage : ITopicMessage
        {
            var sw = Stopwatch.StartNew();
            foreach (var message in messages)
            {
                var agg = aggregateDict.GetValueOrDefault(message.StreamId);
                if (agg.Error != null)
                    continue;
                try
                {
                    switch (message)
                    {
                        case Command command: agg.Handle(command); break;
                        case Request request: agg.Handle(request); break;
                        case Event @event: agg.Apply(@event, false); break;
                    }
                }
                catch (Exception e)
                {
                    agg.SetStatus(HttpStatusCode.InternalServerError, e.Message);
                }
            }

            _logger.LogInformation("TriggerHandled {Count} {Type} Aggregate(s) in {Duration}",
                aggregateDict.Count, typeof(T).Name, sw.ElapsedMilliseconds);
        }

        private async Task SaveAsync(Dictionary<string, Aggregate<T>> aggregateDict)
        {
            // AfterSave Hook
            SafeHook(() => Config.Middleware?.BeforeSave(aggregateDict), aggregateDict);

            // Save Successful Aggregates and Events
            await _aggregator.SaveAllAsync(aggregateDict);

            // AfterSave Hook
            SafeHook(() => Config.Middleware?.AfterSave(aggregateDict), aggregateDict);
        }

        private static void SafeHook(Action action, Dictionary<string, Aggregate<T>> aggregateDict)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                foreach (var agg in aggregateDict.Values)
                    agg.SetStatus(HttpStatusCode.InternalServerError, e.Message);
            }
        }

    }
}

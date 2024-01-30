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
using Insperex.EventHorizon.EventStore.Interfaces.Stores;
using Insperex.EventHorizon.EventStreaming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Insperex.EventHorizon.EventSourcing.AggregateWorkflow
{
    public class AggregateWorkflow<TWrapper, T>
        where TWrapper : class, IStateWrapper<T>, new()
        where T : class, IState
    {
        private readonly ICrudStore<TWrapper> _crudStore;
        private readonly Aggregator<TWrapper, T> _aggregator;
        private readonly StreamingClient _streamingClient;
        private readonly AggregateWorkflowConfig<TWrapper, T> _config;
        private readonly ILogger<AggregateWorkflow<TWrapper, T>> _logger;

        public AggregateWorkflow(IServiceProvider serviceProvider, AggregateWorkflowConfig<TWrapper, T> config)
        {
            _crudStore = serviceProvider.GetRequiredService<ICrudStore<TWrapper>>();
            _streamingClient = serviceProvider.GetRequiredService<StreamingClient>();
            _logger = serviceProvider.GetRequiredService<ILogger<AggregateWorkflow<TWrapper, T>>>();

            // Aggregator
            var aggregatorBuilder = serviceProvider.GetRequiredService<AggregatorBuilder<TWrapper, T>>();
            _aggregator = aggregatorBuilder.Build();

            _config = config;
        }

        public AggregateWorkflowConfig<TWrapper, T> Config => _config;

        public async Task RebuildAllAsync(CancellationToken ct)
        {
            var minDateTime = await _crudStore.GetLastUpdatedDateAsync(ct);

            // NOTE: return with one ms forward because mongodb rounds to one ms
            minDateTime = minDateTime == default? minDateTime : minDateTime.AddMilliseconds(1);

            var reader = _streamingClient.CreateReader<Event>().AddStream<T>().StartDateTime(minDateTime).Build();

            while (!ct.IsCancellationRequested)
            {
                var events = await reader.GetNextAsync(1000);
                if (!events.Any()) break;

                var lookup = events.ToLookup(x => x.Data.StreamId);
                var streamIds = lookup.Select(x => x.Key).ToArray();
                var models = await _crudStore.GetAllAsync(streamIds, ct);
                var modelsDict = models.ToDictionary(x => x.Id);
                var dict = new Dictionary<string, Aggregate<T>>();
                foreach (var streamId in streamIds)
                {
                    var agg = modelsDict.ContainsKey(streamId)
                        ? new Aggregate<T>(modelsDict[streamId])
                        : new Aggregate<T>(streamId);

                    foreach (var message in lookup[streamId])
                        agg.Apply(message.Data);

                    dict[agg.Id] = agg;
                }

                if(! dict.Any()) return;
                await _aggregator.SaveAllAsync(dict);
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
            SafeHook(() => _config.Middleware.OnLoad(aggregateDict), aggregateDict);

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
            SafeHook(() => _config.Middleware.BeforeSave(aggregateDict), aggregateDict);

            // Save Successful Aggregates and Events
            await _aggregator.SaveAllAsync(aggregateDict);

            // AfterSave Hook
            SafeHook(() => _config.Middleware.AfterSave(aggregateDict), aggregateDict);
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

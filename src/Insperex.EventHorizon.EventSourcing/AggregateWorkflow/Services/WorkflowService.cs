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
using Insperex.EventHorizon.EventSourcing.AggregateWorkflow.Workflows;
using Insperex.EventHorizon.EventStore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Insperex.EventHorizon.EventSourcing.AggregateWorkflow.Services
{
    public class WorkflowService<TWrapper, TState, TMessage>
        where TWrapper : class, IStateWrapper<TState>, new()
        where TState : class, IState
        where TMessage : class, ITopicMessage, new()
    {
        private readonly ILogger<WorkflowService<TWrapper, TState, TMessage>> _logger;
        private readonly Aggregator<TWrapper, TState> _aggregator;
        private readonly AggregateWorkflowConfigurator<TState, TMessage> _config;

        public WorkflowService(IServiceProvider serviceProvider, AggregateWorkflowConfigurator<TState, TMessage> config = null)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<WorkflowService<TWrapper, TState, TMessage>>>();

            // Aggregator
            var aggregatorBuilder = serviceProvider.GetRequiredService<AggregatorBuilder<TWrapper, TState>>();
            _aggregator = aggregatorBuilder.Build();

            _config = config;
        }

        public async Task<Dictionary<string, Aggregate<TState>>> LoadAsync(TMessage[] messages, CancellationToken ct)
        {
            // Load Aggregate
            var streamIds = messages.Select(x => x.StreamId).Distinct().ToArray();
            var aggregateDict = await _aggregator.GetAggregatesFromStatesAsync(streamIds, ct);

            // OnLoad Hook
            SafeHook(() => _config.Middleware?.OnLoad(aggregateDict), aggregateDict);

            return aggregateDict;
        }

        public void TriggerHandle(TMessage[] messages, Dictionary<string, Aggregate<TState>> aggregateDict)
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
                        case Event @event: agg.Handle(@event); break;
                    }
                }
                catch (Exception e)
                {
                    agg.SetStatus(HttpStatusCode.InternalServerError, e.Message);
                }
            }

            _logger.LogInformation("TriggerHandled {Count} {Type} Aggregate(s) in {Duration}",
                aggregateDict.Count, typeof(TState).Name, sw.ElapsedMilliseconds);
        }

        public void TriggerApplyEvents(TMessage[] messages, Dictionary<string, Aggregate<TState>> aggregateDict, bool isFirstTime)
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
                        case Event @event: agg.Apply(@event, isFirstTime); break;
                    }
                }
                catch (Exception e)
                {
                    agg.SetStatus(HttpStatusCode.InternalServerError, e.Message);
                }
            }

            _logger.LogInformation("TriggerHandled {Count} {Type} Aggregate(s) in {Duration}",
                aggregateDict.Count, typeof(TState).Name, sw.ElapsedMilliseconds);
        }

        public async Task SaveAsync(Dictionary<string, Aggregate<TState>> aggregateDict)
        {
            // AfterSave Hook
            SafeHook(() => _config.Middleware?.BeforeSave(aggregateDict), aggregateDict);

            // Save Successful Aggregates and Events
            await _aggregator.SaveAllAsync(aggregateDict);

            // AfterSave Hook
            SafeHook(() => _config.Middleware?.AfterSave(aggregateDict), aggregateDict);
        }

        public async Task TryAndPublishResponses(Dictionary<string, Aggregate<TState>> aggregateDict)
        {
            var responses = aggregateDict.Values.SelectMany(x => x.Responses).ToArray();
            if(responses.Any())
                await _aggregator.PublishResponseAsync(responses);
        }

        private static void SafeHook(Action action, Dictionary<string, Aggregate<TState>> aggregateDict)
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

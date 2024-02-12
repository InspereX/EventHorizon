using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.EventSourcing.Aggregates;
using Insperex.EventHorizon.EventSourcing.AggregateWorkflow.Services;
using Insperex.EventHorizon.EventStore.Interfaces;
using Insperex.EventHorizon.EventStreaming;
using Insperex.EventHorizon.EventStreaming.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Insperex.EventHorizon.EventSourcing.AggregateWorkflow.Workflows
{
    public class HandleAndApplyEvents<TWrapper, TState, TMessage> : BaseSubscriptionWorkflow<TState, TMessage>
        where TWrapper : class, IStateWrapper<TState>, new()
        where TState : class, IState
        where TMessage : class, ITopicMessage, new()
    {
        private readonly WorkflowService<TWrapper, TState, TMessage> _workflowService;

        public HandleAndApplyEvents(StreamingClient streamingClient,
            WorkflowService<TWrapper, TState, TMessage> workflowService,
            AggregateWorkflowConfigurator<TState, TMessage> config)
        {
            var subscriptionBuilder = streamingClient.CreateSubscription<TMessage>()
                .SubscriptionName($"Handle{typeof(TMessage).Name}AndApplyEvents-{typeof(TState).Name}")
                .AddStream<TState>()
                .OnBatch(OnBatch);

            config?.OnSubConfig?.Invoke(subscriptionBuilder);

            Subscription = subscriptionBuilder.Build();
            _workflowService = workflowService;
        }

        public override async Task<Dictionary<string, Aggregate<TState>>> HandleAsync(TMessage[] messages, CancellationToken ct)
        {
            var aggregateDict = await _workflowService.LoadAsync(messages, ct);

            // Map/Apply Changes
            _workflowService.TriggerHandle(messages, aggregateDict);

            // Save Snapshots and Publish Events
            await _workflowService.SaveAsync(aggregateDict);

            // Try to Publish Responses
            await _workflowService.TryAndPublishResponses(aggregateDict);

            return aggregateDict;
        }
    }
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.EventSourcing.Aggregates;
using Insperex.EventHorizon.EventSourcing.AggregateWorkflow.Services;
using Insperex.EventHorizon.EventStore.Interfaces;
using Insperex.EventHorizon.EventStreaming;

namespace Insperex.EventHorizon.EventSourcing.AggregateWorkflow.Workflows
{
    public class ApplyEvents<TWrapper, TState> : BaseSubscriptionWorkflow<TState, Event>
        where TWrapper : class, IStateWrapper<TState>, new()
        where TState : class, IState
    {
        private readonly WorkflowService<TWrapper, TState, Event> _workflowService;

        public ApplyEvents(StreamingClient streamingClient,
            WorkflowService<TWrapper, TState, Event> workflowService,
            AggregateWorkflowConfigurator<TState, Event> config)
        {
            var subscriptionBuilder = streamingClient.CreateSubscription<Event>()
                .SubscriptionName($"ApplyEvents-{typeof(TState).Name}")
                .AddStream<TState>()
                .OnBatch(OnBatch);

            config?.OnSubConfig?.Invoke(subscriptionBuilder);

            Subscription = subscriptionBuilder.Build();
            _workflowService = workflowService;
        }

        public override async Task<Dictionary<string, Aggregate<TState>>> HandleAsync(Event[] messages, CancellationToken ct)
        {
            var aggregateDict = await _workflowService.LoadAsync(messages, ct);

            // Map/Apply Changes
            _workflowService.TriggerApplyEvents(messages, aggregateDict, false);

            // Save Snapshots and Publish Events
            await _workflowService.SaveAsync(aggregateDict);

            return aggregateDict;
        }
    }
}

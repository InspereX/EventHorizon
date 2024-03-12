using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.EventSourcing.Aggregates;
using Insperex.EventHorizon.EventSourcing.AggregateWorkflows.Interfaces;
using Insperex.EventHorizon.EventSourcing.Extensions;
using Insperex.EventHorizon.EventStore.Interfaces;
using Insperex.EventHorizon.EventStreaming;
using Insperex.EventHorizon.EventStreaming.Subscriptions;

namespace Insperex.EventHorizon.EventSourcing.AggregateWorkflows.Workflows
{
    public class HandleAndApplyEvents<TWrapper, TState, TMessage> : IWorkflow
        where TWrapper : class, IStateParent<TState>, new()
        where TState : class, IState
        where TMessage : class, ITopicMessage, new()
    {
        private readonly WorkflowService<TWrapper, TState, TMessage> _workflowService;
        private readonly Subscription<TMessage> _subscription;

        public HandleAndApplyEvents(StreamingClient streamingClient,
            WorkflowService<TWrapper, TState, TMessage> workflowService, WorkflowConfigurator<TState> configurator)
        {
            var subscriptionBuilder = streamingClient.CreateSubscription<TMessage>()
                .SubscriptionName($"Handle{typeof(TMessage).Name}AndApplyEvents-{typeof(TState).Name}")
                .AddStateStream<TState>()
                .OnBatch(OnBatch)
                .BatchSize(configurator.BatchSize ?? 1000);

            _subscription = subscriptionBuilder.Build();
            _workflowService = workflowService;
        }

        private async Task OnBatch(SubscriptionContext<TMessage> batch)
        {
            // Setup
            var messages = batch.Messages.Select(m => m.Data).ToArray();
            var aggregateDict = await _workflowService.LoadAsync(messages, batch.CancellationToken);

            // Map/Apply Changes
            _workflowService.TriggerHandle(messages, aggregateDict);

            // Save Snapshots and Publish Events
            await _workflowService.SaveAsync(aggregateDict);

            // Try to Publish Responses
            await _workflowService.TryAndPublishResponses(aggregateDict);

            batch.NackFailedMessagesOnAggregates(aggregateDict);
        }

        public Task StartAsync(CancellationToken cancellationToken) => _subscription.StartAsync();
        public Task StopAsync(CancellationToken cancellationToken) => _subscription.StopAsync();
    }
}

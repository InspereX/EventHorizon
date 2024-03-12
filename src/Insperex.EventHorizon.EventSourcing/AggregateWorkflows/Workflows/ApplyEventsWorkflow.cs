using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.EventSourcing.AggregateWorkflows.Interfaces;
using Insperex.EventHorizon.EventSourcing.Extensions;
using Insperex.EventHorizon.EventStore.Interfaces;
using Insperex.EventHorizon.EventStreaming;
using Insperex.EventHorizon.EventStreaming.Subscriptions;

namespace Insperex.EventHorizon.EventSourcing.AggregateWorkflows.Workflows
{
    public class ApplyEventsWorkflow<TWrapper, TState> : IWorkflow
        where TWrapper : class, IStateParent<TState>, new()
        where TState : class, IState
    {
        private readonly WorkflowService<TWrapper, TState, Event> _workflowService;
        private readonly Subscription<Event> _subscription;

        public ApplyEventsWorkflow(StreamingClient streamingClient, WorkflowService<TWrapper, TState, Event> workflowService, WorkflowConfigurator<TState> configurator)
        {
            var subscriptionBuilder = streamingClient.CreateSubscription<Event>()
                .SubscriptionName($"ApplyEvents-{typeof(TState).Name}")
                .AddStateStream<TState>()
                .OnBatch(OnBatch)
                .BatchSize(configurator.BatchSize ?? 1000);

            _subscription = subscriptionBuilder.Build();
            _workflowService = workflowService;
        }

        private async Task OnBatch(SubscriptionContext<Event> batch)
        {
            // Setup
            var messages = batch.Messages.Select(m => m.Data).ToArray();
            var aggregateDict = await _workflowService.LoadAsync(messages, batch.CancellationToken);

            // Map/Apply Changes
            _workflowService.TriggerApplyEvents(messages, aggregateDict, false);

            // Save Snapshots and Publish Events
            await _workflowService.SaveAsync(aggregateDict);

            batch.NackFailedMessagesOnAggregates(aggregateDict);
        }

        public Task StartAsync(CancellationToken cancellationToken) => _subscription.StartAsync();
        public Task StopAsync(CancellationToken cancellationToken) => _subscription.StopAsync();
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.EventSourcing.Aggregates;
using Insperex.EventHorizon.EventSourcing.AggregateWorkflows.Interfaces;
using Insperex.EventHorizon.EventSourcing.Extensions;
using Insperex.EventHorizon.EventStore.Interfaces;
using Insperex.EventHorizon.EventStreaming;
using Insperex.EventHorizon.EventStreaming.Subscriptions;

namespace Insperex.EventHorizon.EventSourcing.AggregateWorkflows.Workflows
{
    public class RebuildAllWorkflow<TWrapper, TState> : IWorkflow
        where TWrapper : class, IStateParent<TState>, new()
        where TState : class, IState
    {
        private readonly Aggregator<TWrapper, TState> _aggregator;
        private readonly Subscription<Event> _subscription;

        public RebuildAllWorkflow(StreamingClient streamingClient, Aggregator<TWrapper, TState> aggregator, WorkflowConfigurator<TState> configurator)
        {
            var subscriptionBuilder = streamingClient.CreateSubscription<Event>()
                .SubscriptionName($"RebuildAll-{typeof(TState).Name}")
                .AddStateStream<TState>()
                .StopAtEnd(true)
                .OnBatch(OnBatch)
                .BatchSize(configurator.BatchSize ?? 1000);

            // config?.OnSubConfig?.Invoke(subscriptionBuilder);

            _subscription = subscriptionBuilder.Build();
            _aggregator = aggregator;
        }

        private async Task OnBatch(SubscriptionContext<Event> batch)
        {
            // Setup
            var messages = batch.Messages.Select(m => m.Data).ToArray();
            // Load Aggregates
            var streamIds = messages.Select(x => x.StreamId).Distinct().ToArray();
            var aggregateDict = await _aggregator.GetAggregatesFromStateAsync(streamIds, batch.CancellationToken);

            // Apply Events
            foreach (var message in messages)
                aggregateDict[message.StreamId].Apply(message, false);

            // Save Changes
            await _aggregator.SaveAllAsync(aggregateDict);

            batch.NackFailedMessagesOnAggregates(aggregateDict);
        }

        public Task StartAsync(CancellationToken cancellationToken) => _subscription.StartAsync();
        public Task StopAsync(CancellationToken cancellationToken) => _subscription.StopAsync();
    }
}

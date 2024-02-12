using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.EventSourcing.Aggregates;
using Insperex.EventHorizon.EventStore.Interfaces;
using Insperex.EventHorizon.EventStreaming;

namespace Insperex.EventHorizon.EventSourcing.AggregateWorkflow.Workflows
{
    public class RebuildAllWorkflow<TWrapper, TState> : BaseSubscriptionWorkflow<TState, Event>
        where TWrapper : class, IStateWrapper<TState>, new()
        where TState : class, IState
    {
        private readonly Aggregator<TWrapper, TState> _aggregator;

        public RebuildAllWorkflow(StreamingClient streamingClient,
            Aggregator<TWrapper, TState> aggregator,
            AggregateWorkflowConfigurator<TState, Event> config)
        {
            var subscriptionBuilder = streamingClient.CreateSubscription<Event>()
                .SubscriptionName($"RebuildAll-{typeof(TState).Name}")
                .AddStream<TState>()
                .StopAtEnd(true)
                .OnBatch(OnBatch);

            config?.OnSubConfig?.Invoke(subscriptionBuilder);

            Subscription = subscriptionBuilder.Build();
            _aggregator = aggregator;
        }

        public override async Task<Dictionary<string, Aggregate<TState>>> HandleAsync(Event[] messages, CancellationToken ct)
        {
            // Load Aggregates
            var streamIds = messages.Select(x => x.StreamId).Distinct().ToArray();
            var aggDict = await _aggregator.GetAggregatesFromStatesAsync(streamIds, ct);

            // Apply Events
            foreach (var message in messages)
                aggDict[message.StreamId].Apply(message, false);

            // Save Changes
            await _aggregator.SaveAllAsync(aggDict);

            return aggDict;
        }

    }
}

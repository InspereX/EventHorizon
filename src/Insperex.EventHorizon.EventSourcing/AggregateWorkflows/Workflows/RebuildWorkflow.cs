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
    public class RebuildAllWorkflow<TWrapper, TState> : BaseSubscriptionWorkflow<TWrapper, TState, Event>
        where TWrapper : class, IStateParent<TState>, new()
        where TState : class, IState
    {
        private readonly Aggregator<TWrapper, TState> _aggregator;

        public RebuildAllWorkflow(Aggregator<TWrapper, TState> aggregator, StreamingClient streamingClient, WorkflowService<TWrapper, TState, Event> workflowService, WorkflowConfigurator<TState> configurator) : base(streamingClient, workflowService, configurator)
        {
            _aggregator = aggregator;
        }

        public override async Task HandleBatch(Event[] messages, Dictionary<string, Aggregate<TState>> aggregateDict)
        {
            // Apply Events
            foreach (var message in messages)
                aggregateDict[message.StreamId].Apply(message, false);

            // Save Changes
            await _aggregator.SaveAllAsync(aggregateDict);
        }
    }
}

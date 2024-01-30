
using System;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.EventSourcing.Aggregates;
using Insperex.EventHorizon.EventSourcing.Interfaces;
using Insperex.EventHorizon.EventStore.Interfaces;
using Insperex.EventHorizon.EventStreaming.Subscriptions;

namespace Insperex.EventHorizon.EventSourcing.AggregateWorkflow
{
    public class AggregateWorkflowConfig<TWrapper, T>
        where TWrapper : class, IStateWrapper<T>, new()
        where T : class, IState
    {
        public int BatchSize { get; set; }
        public IAggregateMiddleware<T> Middleware { get; set; }
    }
}

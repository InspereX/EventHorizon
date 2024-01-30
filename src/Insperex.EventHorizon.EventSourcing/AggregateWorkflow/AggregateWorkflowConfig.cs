using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.EventSourcing.Interfaces;
using Insperex.EventHorizon.EventStore.Interfaces;

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

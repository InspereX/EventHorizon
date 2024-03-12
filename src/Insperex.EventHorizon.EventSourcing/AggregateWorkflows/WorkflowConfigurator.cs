using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.EventSourcing.AggregateWorkflows.Interfaces;

namespace Insperex.EventHorizon.EventSourcing.AggregateWorkflows
{
    public class WorkflowConfigurator<TState>
        where TState : IState
    {
        internal int? BatchSize { get; set; }
        internal IWorkflowMiddleware<TState> WorkflowMiddleware { get; set; }

        public WorkflowConfigurator<TState> WithBatchSize(int batchSize)
        {
            BatchSize = batchSize;
            return this;
        }

        public WorkflowConfigurator<TState> WithMiddleware(IWorkflowMiddleware<TState> middleware)
        {
            WorkflowMiddleware = middleware;
            return this;
        }
    }
}

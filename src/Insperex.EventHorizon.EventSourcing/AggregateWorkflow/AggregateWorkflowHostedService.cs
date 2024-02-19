using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Insperex.EventHorizon.EventSourcing.AggregateWorkflow.Workflows;
using Microsoft.Extensions.Hosting;

namespace Insperex.EventHorizon.EventSourcing.AggregateWorkflow
{
    public class AggregateWorkflowHostedService : IHostedService
    {
        private readonly IEnumerable<IAggregateWorkflow> _workflows;

        public AggregateWorkflowHostedService(IEnumerable<IAggregateWorkflow> workflows)
        {
            _workflows = workflows;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var workflow in _workflows)
                await workflow.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var workflow in _workflows)
                await workflow.StopAsync(cancellationToken);
        }
    }
}

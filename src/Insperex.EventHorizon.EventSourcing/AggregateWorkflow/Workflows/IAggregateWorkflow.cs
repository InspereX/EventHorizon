using System.Threading;
using System.Threading.Tasks;

namespace Insperex.EventHorizon.EventSourcing.AggregateWorkflow.Workflows
{
    public interface IAggregateWorkflow
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}

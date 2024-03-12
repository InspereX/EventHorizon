using System.Threading;
using System.Threading.Tasks;

namespace Insperex.EventHorizon.EventSourcing.AggregateWorkflows.Interfaces
{
    public interface IWorkflow
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.EventSourcing.Aggregates;

namespace Insperex.EventHorizon.EventSourcing.Interfaces
{
    public interface IAggregateMiddleware { }

    public interface IAggregateMiddleware<T> : IAggregateMiddleware
        where T : class, IState
    {
        public Task OnLoad(Dictionary<string, Aggregate<T>> aggregateDict);
        public Task BeforeSave(Dictionary<string, Aggregate<T>> aggregateDict);
        public Task AfterSave(Dictionary<string, Aggregate<T>> aggregateDict);
    }
}

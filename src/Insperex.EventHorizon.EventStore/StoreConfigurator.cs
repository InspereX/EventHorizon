using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.EventStore.Locks;
using Microsoft.Extensions.DependencyInjection;

namespace Insperex.EventHorizon.EventStore
{
    public class StoreConfigurator<TState> where TState : class, IState
    {
        public IServiceCollection Collection { get; set; }

        public StoreConfigurator(IServiceCollection collection)
        {
            Collection = collection;

            // Common
            collection.AddSingleton<LockFactory<TState>>();
        }
    }
}

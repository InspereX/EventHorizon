using Insperex.EventHorizon.EventStore.Locks;
using Microsoft.Extensions.DependencyInjection;

namespace Insperex.EventHorizon.EventStore
{
    public class StoreConfigurator
    {
        public IServiceCollection Collection { get; set; }

        public StoreConfigurator(IServiceCollection collection)
        {
            Collection = collection;

            // Common
            collection.AddSingleton(typeof(LockFactory<>));
        }
    }
}

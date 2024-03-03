using Insperex.EventHorizon.Abstractions;
using Insperex.EventHorizon.EventStore.Locks;
using Microsoft.Extensions.DependencyInjection;

namespace Insperex.EventHorizon.EventStore.InMemory.Extensions;

public static class EventHorizonConfiguratorExtensions
{

    public static EventHorizonConfigurator AddInMemoryClient(this EventHorizonConfigurator configurator)
    {
        configurator.Collection.AddSingleton(typeof(LockFactory<>));
        configurator.Collection.AddSingleton(typeof(InMemoryStoreClient));
        return configurator;
    }
}

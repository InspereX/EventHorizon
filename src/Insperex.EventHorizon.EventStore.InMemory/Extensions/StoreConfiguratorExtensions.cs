using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.EventStore.InMemory.Stores;
using Insperex.EventHorizon.EventStore.Interfaces.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Insperex.EventHorizon.EventStore.InMemory.Extensions
{
    public static class StoreConfiguratorExtensions
    {
        public static StoreConfigurator UseInMemorySnapshotStore<TState>(this StoreConfigurator configurator) where TState : IState
        {
            configurator.Collection.Replace(ServiceDescriptor.Describe(
                typeof(ISnapshotStore<TState>),
                typeof(InMemorySnapshotStore<TState>),
                ServiceLifetime.Singleton));
            configurator.Collection.Replace(ServiceDescriptor.Describe(
                typeof(ILockStore<TState>),
                typeof(InMemoryLockStore<TState>),
                ServiceLifetime.Singleton));
            return configurator;
        }

        public static StoreConfigurator UseInMemoryViewStore<TState>(this StoreConfigurator configurator) where TState : IState
        {
            configurator.Collection.Replace(ServiceDescriptor.Describe(
                typeof(IViewStore<TState>),
                typeof(InMemoryViewStore<TState>),
                ServiceLifetime.Singleton));
            return configurator;
        }
    }
}

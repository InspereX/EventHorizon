using System;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.EventStore.InMemory.Stores;
using Insperex.EventHorizon.EventStore.Interfaces.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Insperex.EventHorizon.EventStore.InMemory.Extensions
{
    public static class StoreConfiguratorExtensions
    {
        public static StoreConfigurator<TState> UseInMemorySnapshotStore<TState>(this StoreConfigurator<TState> configurator, Action<InMemoryCollectionConfigurator<TState>> onConfig = null)
            where TState : class, IState
        {
            ConfigureConfigurator(configurator, "Snapshot", onConfig);
            configurator.Collection.Replace(ServiceDescriptor.Describe(
                typeof(ISnapshotStore<TState>),
                typeof(InMemorySnapshotStore<TState>),
                ServiceLifetime.Singleton));
            return configurator;
        }

        public static StoreConfigurator<T> UseInMemoryLockStore<T>(this StoreConfigurator<T> configurator, Action<InMemoryCollectionConfigurator<T>> onConfig = null)
            where T : class, IState
        {
            ConfigureConfigurator(configurator, "Lock", onConfig);
            configurator.Collection.Replace(ServiceDescriptor.Describe(
                typeof(ILockStore<T>),
                typeof(InMemoryLockStore<T>),
                ServiceLifetime.Singleton));
            return configurator;
        }

        public static StoreConfigurator<TState> UseInMemoryViewStore<TState>(this StoreConfigurator<TState> configurator, Action<InMemoryCollectionConfigurator<TState>> onConfig = null)
            where TState : class, IState
        {
            ConfigureConfigurator(configurator, "View", onConfig);
            configurator.Collection.Replace(ServiceDescriptor.Describe(
                typeof(IViewStore<TState>),
                typeof(InMemoryViewStore<TState>),
                ServiceLifetime.Singleton));
            return configurator;
        }

        private static void ConfigureConfigurator<T>(StoreConfigurator<T> configurator, string name, Action<InMemoryCollectionConfigurator<T>> onConfig = null)
        {
            configurator.Collection.AddSingleton(x =>
            {

                var mongoCollectionConfigurator = new InMemoryCollectionConfigurator<T>();
                onConfig?.Invoke(mongoCollectionConfigurator);

                if (mongoCollectionConfigurator.Database == null)
                    mongoCollectionConfigurator.WithDatabase(typeof(T).Name);
                if (mongoCollectionConfigurator.Collection == null)
                    mongoCollectionConfigurator.WithCollection(name);

                return mongoCollectionConfigurator;
            });
        }
    }
}

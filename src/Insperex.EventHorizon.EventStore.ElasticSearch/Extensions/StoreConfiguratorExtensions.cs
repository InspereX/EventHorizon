using System;
using Insperex.EventHorizon.Abstractions;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.EventStore.ElasticSearch.Models;
using Insperex.EventHorizon.EventStore.ElasticSearch.Stores;
using Insperex.EventHorizon.EventStore.Interfaces.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Insperex.EventHorizon.EventStore.ElasticSearch.Extensions
{
    public static class StoreConfiguratorExtensions
    {
        public static StoreConfigurator<TState> UseElasticSnapshotStore<TState>(this StoreConfigurator<TState> configurator, Action<ElasticIndexConfigurator<TState>> onConfig = null)
            where TState : IState
        {
            ConfigureConfigurator(configurator, "Snapshot", onConfig);
            configurator.Collection.AddSingleton<ISnapshotStore<TState>, ElasticSnapshotStore<TState>>();
            return configurator;
        }

        public static StoreConfigurator<TState> UseElasticLockStore<TState>(this StoreConfigurator<TState> configurator, Action<ElasticIndexConfigurator<TState>> onConfig = null)
            where TState : IState
        {
            ConfigureConfigurator(configurator, "Lock", onConfig);
            configurator.Collection.AddSingleton<ILockStore<TState>, ElasticLockStore<TState>>();
            return configurator;
        }

        public static StoreConfigurator<TState> UseElasticViewStore<TState>(this StoreConfigurator<TState> configurator, Action<ElasticIndexConfigurator<TState>> onConfig = null)
            where TState : IState
        {
            ConfigureConfigurator(configurator, "View", onConfig);
            configurator.Collection.AddSingleton<IViewStore<TState>, ElasticViewStore<TState>>();
            return configurator;
        }

        private static void ConfigureConfigurator<T>(StoreConfigurator<T> configurator, string name, Action<ElasticIndexConfigurator<T>> onConfig = null)
        {
            configurator.Collection.AddSingleton(x =>
            {

                var mongoCollectionConfigurator = new ElasticIndexConfigurator<T>();
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

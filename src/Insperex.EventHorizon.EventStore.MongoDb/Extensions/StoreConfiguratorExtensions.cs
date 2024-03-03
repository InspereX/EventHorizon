using System;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.EventSourcing;
using Insperex.EventHorizon.EventStore.Interfaces.Stores;
using Insperex.EventHorizon.EventStore.MongoDb.Models;
using Insperex.EventHorizon.EventStore.MongoDb.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Insperex.EventHorizon.EventStore.MongoDb.Extensions
{
    public static class StoreConfiguratorExtensions
    {
        public static EventSourcingConfigurator<TState> UseMongoForStores<TState>(this EventSourcingConfigurator<TState> configurator, Action<MongoCollectionConfigurator<TState>> onConfig = null)
            where TState : class, IState
        {
            configurator.UseMongoSnapshotStore(onConfig);
            configurator.UseMongoLockStore(onConfig);
            configurator.UseMongoViewStore(onConfig);
            return configurator;
        }

        public static EventSourcingConfigurator<TState> UseMongoSnapshotStore<TState>(this EventSourcingConfigurator<TState>configurator, Action<MongoCollectionConfigurator<TState>> onConfig = null)
            where TState : class, IState
        {
            ConfigureConfigurator(configurator, "Snapshot", onConfig);
            configurator.Collection.AddSingleton<ISnapshotStore<TState>, MongoSnapshotStore<TState>>();
            return configurator;
        }

        public static EventSourcingConfigurator<TState> UseMongoLockStore<TState>(this EventSourcingConfigurator<TState> configurator, Action<MongoCollectionConfigurator<TState>> onConfig = null)
            where TState : class, IState
        {
            ConfigureConfigurator(configurator, "Lock", onConfig);
            configurator.Collection.AddSingleton<ILockStore<TState>, MongoLockStore<TState>>();
            return configurator;
        }

        public static EventSourcingConfigurator<TState> UseMongoViewStore<TState>(this EventSourcingConfigurator<TState> configurator, Action<MongoCollectionConfigurator<TState>> onConfig = null)
            where TState : class, IState
        {
            ConfigureConfigurator(configurator, "View", onConfig);
            configurator.Collection.AddSingleton<IViewStore<TState>, MongoViewStore<TState>>();
            return configurator;
        }

        private static void ConfigureConfigurator<TState>(EventSourcingConfigurator<TState> configurator, string name, Action<MongoCollectionConfigurator<TState>> onConfig = null) where TState : class, IState
        {
            configurator.Collection.AddSingleton(x =>
            {

                var mongoCollectionConfigurator = new MongoCollectionConfigurator<TState>();
                onConfig?.Invoke(mongoCollectionConfigurator);

                if (mongoCollectionConfigurator.Database == null)
                    mongoCollectionConfigurator.WithDatabase(typeof(TState).Name);
                if (mongoCollectionConfigurator.Collection == null)
                    mongoCollectionConfigurator.WithCollection(name);

                return mongoCollectionConfigurator;
            });
        }
    }
}

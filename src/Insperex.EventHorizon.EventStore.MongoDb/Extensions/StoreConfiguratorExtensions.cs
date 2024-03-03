using System;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.EventStore.Interfaces.Stores;
using Insperex.EventHorizon.EventStore.MongoDb.Models;
using Insperex.EventHorizon.EventStore.MongoDb.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Insperex.EventHorizon.EventStore.MongoDb.Extensions
{
    public static class StoreConfiguratorExtensions
    {
        public static StoreConfigurator<TState> UseMongoDbSnapshotStore<TState>(this StoreConfigurator<TState> configurator, Action<MongoConfig> onConfig = null)
            where TState : class, IState
        {
            configurator.Collection.AddSingleton<ISnapshotStore<TState>, MongoSnapshotStore<TState>>();
            configurator.Collection.AddSingleton<ILockStore<TState>, MongoLockStore<TState>>();
            return configurator;
        }

        public static StoreConfigurator<TState> UseMongoDbViewStore<TState>(this StoreConfigurator<TState> configurator, Action<MongoConfig> onConfig = null)
            where TState : class, IState
        {
            configurator.Collection.AddSingleton<IViewStore<TState>, MongoViewStore<TState>>();
            return configurator;
        }
    }
}

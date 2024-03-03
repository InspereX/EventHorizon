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
        public static EventHorizonConfigurator UseElasticSnapshotStore<TState>(this EventHorizonConfigurator configurator, Action<ElasticConfig> onConfig = null) where TState : IState
        {
            configurator.Collection.AddSingleton<ISnapshotStore<TState>, ElasticSnapshotStore<TState>>();
            configurator.Collection.AddSingleton<ILockStore<TState>, ElasticLockStore<TState>>();
            return configurator;
        }

        public static EventHorizonConfigurator UseElasticViewStore<TState>(this EventHorizonConfigurator configurator, Action<ElasticConfig> onConfig = null) where TState : IState
        {
            configurator.Collection.AddSingleton<IViewStore<TState>, ElasticViewStore<TState>>();
            return configurator;
        }
    }
}

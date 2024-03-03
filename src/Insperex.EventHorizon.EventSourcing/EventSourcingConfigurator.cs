using System;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.EventStore;
using Insperex.EventHorizon.EventStreaming;
using Microsoft.Extensions.DependencyInjection;

namespace Insperex.EventHorizon.EventSourcing
{
    public class EventSourcingConfigurator<TState>
        where TState : class, IState
    {
        public EventSourcingConfigurator(IServiceCollection collection)
        {

        }

        public EventSourcingConfigurator<TState> WithStoreConfig(Action<StoreConfigurator<TState>> onConfig)
        {
            return this;
        }

        public EventSourcingConfigurator<TState> WithStreamConfig(Action<StreamConfigurator> onConfig)
        {
            return this;
        }
    }
}

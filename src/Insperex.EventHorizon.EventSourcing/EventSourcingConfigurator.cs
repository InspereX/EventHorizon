using System;
using Insperex.EventHorizon.EventStore;
using Insperex.EventHorizon.EventStreaming;
using Microsoft.Extensions.DependencyInjection;

namespace Insperex.EventHorizon.EventSourcing
{
    public class EventSourcingConfigurator
    {
        public EventSourcingConfigurator(IServiceCollection collection)
        {

        }

        public EventSourcingConfigurator WithStoreConfig(Action<StoreConfigurator> onConfig)
        {
            return this;
        }

        public EventSourcingConfigurator WithStreamConfig(Action<StreamConfigurator> onConfig)
        {
            return this;
        }
    }
}

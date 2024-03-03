using System;
using Insperex.EventHorizon.Abstractions;
using Insperex.EventHorizon.EventStreaming;

namespace Insperex.EventHorizon.EventSourcing
{
    public static class EventHorizonConfiguratorExtensions
    {
        public static EventHorizonConfigurator AddEventSourcing(this EventHorizonConfigurator configurator, Action<StreamConfigurator> onConfig)
        {
            var streamConfigurator = new StreamConfigurator(configurator.Collection);
            onConfig.Invoke(streamConfigurator);
            return configurator;
        }
    }
}

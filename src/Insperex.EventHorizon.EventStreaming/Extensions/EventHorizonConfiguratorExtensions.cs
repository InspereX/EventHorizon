using System;
using Insperex.EventHorizon.Abstractions;

namespace Insperex.EventHorizon.EventStreaming.Extensions
{
    public static class EventHorizonConfiguratorExtensions
    {
        public static EventHorizonConfigurator AddEventStreaming(this EventHorizonConfigurator configurator, Action<StreamConfigurator> onConfig)
        {
            var streamConfigurator = new StreamConfigurator(configurator.Collection);
            onConfig.Invoke(streamConfigurator);
            return configurator;
        }
    }
}

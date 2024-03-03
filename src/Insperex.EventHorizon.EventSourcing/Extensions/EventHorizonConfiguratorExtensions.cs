using System;
using Insperex.EventHorizon.Abstractions;

namespace Insperex.EventHorizon.EventSourcing.Extensions
{
    public static class EventHorizonConfiguratorExtensions
    {
        public static EventHorizonConfigurator AddEventSourcing(this EventHorizonConfigurator configurator, Action<EventSourcingConfigurator> onConfig = null)
        {
            var streamConfigurator = new EventSourcingConfigurator(configurator.Collection);
            onConfig.Invoke(streamConfigurator);
            return configurator;
        }
    }
}

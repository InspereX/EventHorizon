using System;
using Insperex.EventHorizon.Abstractions;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.EventSourcing.Aggregates;
using Insperex.EventHorizon.EventSourcing.Senders;
using Insperex.EventHorizon.EventSourcing.Util;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Insperex.EventHorizon.EventSourcing.Extensions
{
    public static class EventHorizonConfiguratorExtensions
    {
        public static EventHorizonConfigurator AddAggregator<TState>(this EventHorizonConfigurator configurator, Action<EventSourcingConfigurator<TState>> onConfig = null)
            where TState : class, IState
        {
            var streamConfigurator = new EventSourcingConfigurator<TState>(configurator.Collection);
            onConfig?.Invoke(streamConfigurator);

            configurator.Collection.TryAddSingleton(typeof(EventSourcingClient<>));
            configurator.Collection.TryAddSingleton(typeof(AggregateBuilder<,>));
            configurator.Collection.TryAddSingleton<SenderBuilder>();
            configurator.Collection.TryAddSingleton<SenderSubscriptionTracker>();
            configurator.Collection.TryAddSingleton<ValidationUtil>();

            return configurator;
        }


    }
}

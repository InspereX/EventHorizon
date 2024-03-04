using System;
using Insperex.EventHorizon.Abstractions;
using Insperex.EventHorizon.EventStreaming.Admins;
using Insperex.EventHorizon.EventStreaming.Publishers;
using Insperex.EventHorizon.EventStreaming.Pulsar.Models;
using Insperex.EventHorizon.EventStreaming.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using Pulsar.Client.Api;

namespace Insperex.EventHorizon.EventStreaming.Pulsar.Extensions
{
    public static class EventHorizonConfiguratorExtensions
    {
        public static EventHorizonConfigurator AddPulsarClient(this EventHorizonConfigurator configurator, Action<PulsarConfig> onConfig)
        {
            configurator.Collection.Configure(onConfig);
            configurator.AddClientResolver<PulsarClientResolver, PulsarClient>();


            return configurator;
        }
    }
}

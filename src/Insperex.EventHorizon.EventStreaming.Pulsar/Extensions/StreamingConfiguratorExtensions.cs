using System;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.EventStreaming.Interfaces.Streaming;
using Microsoft.Extensions.DependencyInjection;

namespace Insperex.EventHorizon.EventStreaming.Pulsar.Extensions
{
    public static class StreamingConfiguratorExtensions
    {
        public static StreamConfigurator WithPulsarStream<TMessage, TPayload>(this StreamConfigurator configurator, Action<PulsarTopicConfigurator<TMessage, TPayload>> onConfig)
            where TMessage : ITopicMessage
            where TPayload : IPayload
        {
            // Add Admin and Factory
            configurator.Collection.AddSingleton(typeof(ITopicAdmin<>), typeof(PulsarTopicAdmin<>));
            configurator.Collection.AddSingleton(typeof(IStreamFactory<>), typeof(PulsarStreamFactory<>));
            return configurator;
        }
    }
}

using System;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Util;
using Insperex.EventHorizon.EventStreaming.Interfaces.Streaming;
using Microsoft.Extensions.DependencyInjection;

namespace Insperex.EventHorizon.EventStreaming.Pulsar.Extensions
{
    public static class StreamingConfiguratorExtensions
    {
        public static StreamConfigurator WithPulsarStream<TMessage, TPayload>(this StreamConfigurator configurator, Action<PulsarTopicConfigurator<TMessage, TPayload>> onConfig = null)
            where TMessage : ITopicMessage
            where TPayload : IPayload
        {
            configurator.Collection.AddSingleton(x =>
            {
                var c = new PulsarTopicConfigurator<TMessage, TPayload>(x.GetRequiredService<AttributeUtil>());
                onConfig?.Invoke(c);
                return c;
            });

            // Add Admin and Factory
            configurator.Collection.AddSingleton(typeof(ITopicAdmin<TMessage>), typeof(PulsarTopicAdmin<TMessage>));
            configurator.Collection.AddSingleton(typeof(IStreamFactory<TMessage>), typeof(PulsarStreamFactory<TMessage>));
            return configurator;
        }
    }
}

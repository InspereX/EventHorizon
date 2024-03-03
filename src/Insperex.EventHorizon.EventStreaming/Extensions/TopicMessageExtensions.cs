using System;
using System.Collections.Generic;
using System.Text.Json;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.EventStreaming.Admins;
using Insperex.EventHorizon.EventStreaming.Publishers;
using Insperex.EventHorizon.EventStreaming.Readers;
using Insperex.EventHorizon.EventStreaming.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Insperex.EventHorizon.EventStreaming.Extensions;

public static class TopicMessageExtensions
{
    public static object GetPayload<TMessage>(this TMessage message, Dictionary<string, Type> types)
        where TMessage : ITopicMessage =>
        JsonSerializer.Deserialize(message.Payload, types[message.Type]);
}

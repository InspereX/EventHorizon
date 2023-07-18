﻿using System;
using System.Linq;
using System.Text.Json;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Util;
using Insperex.EventHorizon.EventStreaming.Interfaces;

namespace Insperex.EventHorizon.EventStreaming.Extensions;

public static class TopicMessageExtensions
{
    public static object GetPayload<T>(this T message)
        where T : class, ITopicMessage =>
        JsonSerializer.Deserialize(message.Payload, AssemblyUtil.ActionDict[message.Type]);

    public static T Upgrade<T>(this T message)
        where T : class, ITopicMessage
    {
        var payload = message.GetPayload();
        var upgrade = payload.GetType()
            .GetInterfaces()
            .FirstOrDefault(x => x.Name == typeof(IUpgradeTo<>).Name)?.GetMethod("Upgrade");

        // If no upgrade return original message
        if (upgrade == null) return message;

        upgrade?.Invoke(payload, null);
        return Activator.CreateInstance(typeof(T), message.StreamId, payload) as T;
    }
}

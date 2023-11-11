﻿using System;
using System.Linq;
using System.Text.Json;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Util;

namespace Insperex.EventHorizon.Abstractions.Models;

public class MessageContext<T> where T : ITopicMessage
{
    private readonly StreamUtil _streamUtil;
    public T Data { get; set; }
    public TopicData TopicData { get; set; }

    public MessageContext(StreamUtil streamUtil)
    {
        _streamUtil = streamUtil;
    }

    public object GetPayload()
    {
        var action = _streamUtil.GetTypeFromTopic(Data.StreamId, Data.Type);
        return JsonSerializer.Deserialize(Data.Payload, action);
    }

    public T Upgrade()
    {
        var action = _streamUtil.GetTypeFromTopic(Data.StreamId, Data.Type);
        var payload = JsonSerializer.Deserialize(Data.Payload, action);
        var upgrade = action
            .GetInterfaces()
            .FirstOrDefault(x => x.Name == typeof(IUpgradeTo<>).Name)?.GetMethod("Upgrade");

        // If no upgrade return original message
        if (upgrade == null) return Data;

        upgrade?.Invoke(payload, null);
        return (T)Activator.CreateInstance(typeof(T), Data.StreamId, payload);
    }
}

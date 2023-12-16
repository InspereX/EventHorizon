using System;
using System.Linq;
using System.Text.Json;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Models;
using Insperex.EventHorizon.EventStreaming.Util;

namespace Insperex.EventHorizon.EventStreaming.Models;

public class MessageContext<T> where T : ITopicMessage
{
    private readonly Type _type;
    public T Data { get; set; }
    public TopicData TopicData { get; set; }

    public MessageContext(Type type, T data, TopicData topicData)
    {
        _type = type;
        Data = data;
        TopicData = topicData;
    }

    public object GetPayload()
    {
        return JsonSerializer.Deserialize(Data.Payload, _type);
    }

    public T Upgrade()
    {
        var payload = JsonSerializer.Deserialize(Data.Payload, _type);
        var upgrade = _type
            .GetInterfaces()
            .FirstOrDefault(x => x.Name == typeof(IUpgradeTo<>).Name)?.GetMethod("Upgrade");

        // If no upgrade return original message
        if (upgrade == null) return Data;

        upgrade?.Invoke(payload, null);
        return (T)Activator.CreateInstance(typeof(T), Data.StreamId, payload);
    }
}

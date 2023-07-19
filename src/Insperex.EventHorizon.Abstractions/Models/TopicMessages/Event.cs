using System;
using System.Text.Json;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Util;

namespace Insperex.EventHorizon.Abstractions.Models.TopicMessages;

public class Event : ITopicMessage
{
    public string Id { get; set; }
    public long SequenceId { get; set; }
    public string StreamId { get; set; }
    public string Type { get; set; }
    public string Payload { get; set; }

    public Event()
    {
        Id = Guid.NewGuid().ToString();
    }

    public Event(string streamId, object payload)
    {
        Id = Guid.NewGuid().ToString();
        StreamId = streamId;
        Payload = JsonSerializer.Serialize(payload);
        Type = payload.GetType().Name;
    }

    public Event(string streamId, long sequenceId, object payload) : this(streamId, payload)
    {
        SequenceId = sequenceId;
    }

    public object GetPayload() => JsonSerializer.Deserialize(Payload, AssemblyUtil.ActionDict[Type]);
}

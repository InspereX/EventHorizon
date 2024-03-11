using System;
using System.Text.Json;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;

namespace Insperex.EventHorizon.Abstractions.Models.TopicMessages;

public class Event : ITopicMessage, ICrudEntity
{
    public string Id { get; set; }
    public string StreamId { get; set; }
    public long SequenceId { get; set; }
    public string Type { get; set; }
    public string Payload { get; set; }
    public DateTime CreatedDate { get; set; }

    public Event()
    {
        Id = Guid.NewGuid().ToString();
        CreatedDate = DateTime.UtcNow;
    }

    public Event(string streamId, object payload) : this()
    {
        StreamId = streamId;
        Payload = JsonSerializer.Serialize(payload);
        Type = payload.GetType().Name;
    }

    public Event(string streamId, long sequenceId, object payload) : this(streamId, payload)
    {
        SequenceId = sequenceId;
    }
}

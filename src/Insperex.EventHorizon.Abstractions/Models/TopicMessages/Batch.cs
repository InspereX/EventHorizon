using System.Text.Json;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;

namespace Insperex.EventHorizon.Abstractions.Models.TopicMessages
{
    public class Batch<T> : ITopicMessage
        where T : class, ITopicMessage
    {
        public string Id { get; set; }
        public string StreamId { get; set; }
        public string Type { get; set; }
        public string Payload { get; set; }

        public Batch()
        {
        }

        public Batch(string streamId, T[] payload)
        {
            Id = streamId;
            StreamId = streamId;
            Payload = JsonSerializer.Serialize(payload);
            Type = payload.GetType().Name;
        }

        public T[] Unwrap() => JsonSerializer.Deserialize<T[]>(Payload);
    }

    public class BatchRequest : Batch<Request>
    {
        public string SenderId { get; set; }

        public BatchRequest() { }

        public BatchRequest(string streamId, Request[] payload) : base(streamId, payload) { }
    }

    public class BatchResponse : Batch<Response>
    {
        public string SenderId { get; set; }
        public BatchResponse() { }

        public BatchResponse(string streamId, string senderId, Response[] payload) : base(streamId, payload)
        {
            SenderId = senderId;
        }
    }

    public class BatchCommand : Batch<Command>
    {
        public BatchCommand() { }
        public BatchCommand(string streamId, Command[] payload) : base(streamId, payload) { }
    }

    public class BatchEvent : Batch<Event>
    {
        public BatchEvent() { }
        public BatchEvent(string streamId, Event[] payload) : base(streamId, payload) { }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;

namespace Insperex.EventHorizon.Abstractions.Models.TopicMessages
{
    public class Batch<T> : ITopicMessage
        where T : class, ITopicMessage
    {
        public string Id { get; set; }
        public string StreamId { get; set; }
        public Dictionary<string, T> Payload { get; set; }

        public Batch()
        {
        }

        public Batch(string streamId, T[] payload)
        {
            StreamId = streamId;
            Payload = payload.ToDictionary(x => x.Id);
        }
    }

    public class BatchRequest : Batch<Request>
    {
        public string SenderId { get; set; }

        public BatchRequest()
        {
            Id = Guid.NewGuid().ToString();
        }

        public BatchRequest(string streamId, Request[] payload) : base(streamId, payload)
        {
            Id = Guid.NewGuid().ToString();
        }
    }

    public class BatchResponse : Batch<Response>
    {
        public string SenderId { get; set; }

        public BatchResponse()
        {

        }

        public BatchResponse(string id, string streamId, string senderId, Response[] payload) : base(streamId, payload)
        {
            Id = id;
            SenderId = senderId;
        }
    }

    public class BatchCommand : Batch<Command>
    {
        public BatchCommand() { }

        public BatchCommand(string streamId, Command[] payload) : base(streamId, payload)
        {

        }
    }

    public class BatchEvent : Batch<Event>
    {
        public BatchEvent() { }
        public BatchEvent(string streamId, Event[] payload) : base(streamId, payload)
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}

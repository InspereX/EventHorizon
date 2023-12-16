using System;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.EventStreaming.Interfaces.Streaming;

namespace Insperex.EventHorizon.EventStreaming.InMemory;

public class InMemoryTopicResolver : ITopicResolver
{
    public string GetTopic<TM>(Type stateType, string topic) where TM : ITopicMessage => $"in-memory://{stateType.Name}/{topic}";
}

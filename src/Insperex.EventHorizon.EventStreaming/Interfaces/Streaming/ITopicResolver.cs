using System;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;

namespace Insperex.EventHorizon.EventStreaming.Interfaces.Streaming;

public interface ITopicResolver
{
    string GetTopic<TM>(Type stateType, string topicName) where TM : ITopicMessage;
}

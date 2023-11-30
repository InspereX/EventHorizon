using System;
using System.Linq;
using Insperex.EventHorizon.Abstractions.Attributes;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Util;
using Insperex.EventHorizon.EventStreaming.Interfaces.Streaming;
using Insperex.EventHorizon.EventStreaming.Models;

namespace Insperex.EventHorizon.EventStreaming.InMemory;

public class InMemoryTopicResolver : ITopicResolver
{
    private readonly AttributeUtil _attributeUtil;

    public InMemoryTopicResolver(AttributeUtil attributeUtil)
    {
        _attributeUtil = attributeUtil;
    }

    public string[] GetTopics<TM>(Type stateType, string topicName = null) where TM : ITopicMessage
    {
        var attributes = _attributeUtil.GetAll<StreamAttribute>(stateType);
        var topics = attributes
            .Select(x =>
            {
                var action = typeof(TM);
                var state = x.SourceType?.Name ?? stateType.Name;
                var topic = topicName == null ? x.GetTopic(action) : $"{x.GetTopic(action)}-{topicName}";
                return $"in-memory://{state}/{topic}";
            })
            .ToArray();

        return topics;
    }
}

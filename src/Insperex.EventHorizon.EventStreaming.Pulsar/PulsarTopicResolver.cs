using System;
using System.Linq;
using Insperex.EventHorizon.Abstractions.Attributes;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.Abstractions.Util;
using Insperex.EventHorizon.EventStreaming.Interfaces.Streaming;
using Insperex.EventHorizon.EventStreaming.Models;
using Insperex.EventHorizon.EventStreaming.Pulsar.Attributes;
using Insperex.EventHorizon.EventStreaming.Pulsar.Models;
using Insperex.EventHorizon.EventStreaming.Util;

namespace Insperex.EventHorizon.EventStreaming.Pulsar;

public class PulsarTopicResolver : ITopicResolver
{
    private readonly AttributeUtil _attributeUtil;
    private const string DefaultTenant = "public";
    private const string DefaultNamespace = "default";
    private const string TypeKey = "$type";
    private const string NamespaceKey = "$namespace";
    private const string ActionKey = "$action";

    public PulsarTopicResolver(AttributeUtil attributeUtil)
    {
        _attributeUtil = attributeUtil;
    }

    public string[] GetTopics<TM>(Type type, string topicName = null) where TM : ITopicMessage
    {
        var persistent = typeof(TM).Name == "BatchEvent"
            ? EventStreamingConstants.Persistent
            : EventStreamingConstants.NonPersistent;

        var pulsarAttr = _attributeUtil.GetOne<PulsarNamespaceAttribute>(type);
        var attributes = _attributeUtil.GetAll<StreamAttribute>(type);
        var topics = attributes
            .Select(x =>
            {
                var tenant = pulsarAttr?.Tenant ?? DefaultTenant;
                var @namespace = pulsarAttr?.Namespace ?? DefaultNamespace;
                var topic = topicName == null ? x.Topic : $"{x.Topic}-{topicName}";
                var path = $"{persistent}://{tenant}/{@namespace}/{topic}";

                return TopicUtil.TopicReplace(path, type, typeof(TM), type.Assembly);
            })
            .ToArray();

        return topics;
    }
}

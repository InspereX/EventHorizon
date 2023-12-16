using System;
using System.Linq;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Util;
using Insperex.EventHorizon.EventStreaming.Interfaces.Streaming;
using Insperex.EventHorizon.EventStreaming.Models;
using Insperex.EventHorizon.EventStreaming.Pulsar.Attributes;
using Insperex.EventHorizon.EventStreaming.Pulsar.Models;

namespace Insperex.EventHorizon.EventStreaming.Pulsar;

public class PulsarTopicResolver : ITopicResolver
{
    private readonly AttributeUtil _attributeUtil;

    public PulsarTopicResolver(AttributeUtil attributeUtil)
    {
        _attributeUtil = attributeUtil;
    }

    public string GetTopic<TM>(Type stateType, string topic) where TM : ITopicMessage
    {
        // Get Tenant and NameSpace
        var pulsarAttr = _attributeUtil.GetOne<PulsarNamespaceAttribute>(stateType);
        var tenant = pulsarAttr?.Tenant ?? PulsarTopicConstants.DefaultTenant;
        var @namespace = !PulsarTopicConstants.MessageTypes.Contains(typeof(TM))
            ? pulsarAttr?.Namespace ?? PulsarTopicConstants.DefaultNamespace
            : PulsarTopicConstants.MessageNamespace;

        return $"{EventStreamingConstants.Persistent}://{tenant}/{@namespace}/{topic}";
    }
}

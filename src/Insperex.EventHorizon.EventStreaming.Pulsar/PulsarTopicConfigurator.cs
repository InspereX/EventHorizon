using System;
using System.Linq;
using Insperex.EventHorizon.Abstractions.Attributes;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.Abstractions.Reflection;
using Insperex.EventHorizon.Abstractions.Util;
using Insperex.EventHorizon.EventStreaming.Models;
using Insperex.EventHorizon.EventStreaming.Pulsar.Attributes;
using Insperex.EventHorizon.EventStreaming.Pulsar.Models;
using Insperex.EventHorizon.EventStreaming.Pulsar.Utils;
using SharpPulsar.Admin.v2;

namespace Insperex.EventHorizon.EventStreaming.Pulsar
{
    public class PulsarTopicConfigurator<TMessage, TPayload> : ITopicConfigurator<TMessage, TPayload>
        where TMessage : ITopicMessage
        where TPayload : IPayload
    {
        internal Type PayloadType { get; set; }
        internal Type MessageType { get; set; }
        internal string Tenant { get; private set; }
        internal string Namespace { get; private set; }
        internal string Topic { get; private set; }
        internal TenantInfo TenantInfo { get; private set; }
        internal Policies NamespacePolicies { get; private set; } = new Policies();


        public PulsarTopicConfigurator(AttributeUtil attributeUtil)
        {
            PayloadType = typeof(TPayload);
            MessageType = typeof(TMessage);

            var defaultTenant = AssemblyUtil.AssemblyName;
            var defaultNamespace = $"{MessageType.Name}-{PayloadType.Name}";

            // Set Defaults
            var pulsarAttr = attributeUtil.GetOne<PulsarNamespaceAttribute>(PayloadType);
            var streamAttr = attributeUtil.GetAll<StreamAttribute>(PayloadType).First(x => x.SubType == null);
            var tenant = pulsarAttr?.Tenant ?? defaultTenant;
            var @namespace = pulsarAttr?.Namespace ?? defaultNamespace;
            var topic = streamAttr?.Topic ?? defaultNamespace;
            WithTopic($"persistent://{tenant}/{@namespace}/{topic}");

            if (typeof(TMessage) == typeof(Event))
            {
                NamespacePolicies.Retention_policies = new RetentionPolicies
                {
                    RetentionTimeInMinutes = pulsarAttr?.RetentionTimeInMinutes ?? -1,
                    RetentionSizeInMB = pulsarAttr?.RetentionSizeInMb ?? -1
                };
            }
            else
            {
                NamespacePolicies.Retention_policies = new RetentionPolicies
                {
                    RetentionTimeInMinutes = 10,
                    RetentionSizeInMB = -1
                };
            }
        }

        public PulsarTopicConfigurator<TMessage, TPayload> WithTenantInfo(Action<TenantInfo> onConfig = null)
        {
            // Apply Config
            if (onConfig == null) return this;
            TenantInfo ??= new TenantInfo();
            onConfig.Invoke(TenantInfo);

            return this;
        }

        public PulsarTopicConfigurator<TMessage, TPayload> WithNamespacePolicies(Action<Policies> onConfig = null)
        {
            // Apply Config
            if (onConfig == null) return this;
            NamespacePolicies ??= new Policies();
            onConfig.Invoke(NamespacePolicies);

            return this;
        }

        public PulsarTopicConfigurator<TMessage, TPayload> WithTopic(string name)
        {
            var parts = PulsarTopicParser.Parse(name);
            Tenant = parts.Tenant;
            Namespace = parts.Namespace;
            Topic = parts.Topic;
            return this;
        }

        public string GetTopic(string senderId = null)
        {
            var topic = senderId == null ? Topic : $"{Topic}-{senderId}";
            return $"{EventStreamingConstants.Persistent}://{Tenant}/{Namespace}/{topic}"
                .Replace(PulsarTopicConstants.TypeKey, typeof(TMessage).Name);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Insperex.EventHorizon.Abstractions.Attributes;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Models;

namespace Insperex.EventHorizon.Abstractions.Util
{
    public class StreamUtil
    {
        private readonly AttributeUtil _assemblyUtil;
        private readonly Dictionary<(string, string), Type> _actions = new();
        private readonly Dictionary<Type, string> _topics = new();

        public StreamUtil(AttributeUtil assemblyUtil)
        {
            _assemblyUtil = assemblyUtil;

            // Store Topic from States
            foreach (var state in AssemblyUtil.States)
            {
                var streamAttribute = assemblyUtil.GetOne<StreamAttribute>(state);
                if (streamAttribute == null) continue;

                // Skip Views
                if (streamAttribute.SourceType != null) continue;

                var topic = streamAttribute.GetTopic(state);
                _topics[state] = topic;

                // Add Actions
                foreach (var action in AssemblyUtil.StateToCommandsLookup[state])
                {
                    _actions[(topic, action.Name)] = action;
                    _topics[action] = topic;
                }
                foreach (var action in AssemblyUtil.StateToRequestsLookup[state])
                {
                    _actions[(topic, action.Name)] = action;
                    _topics[action] = topic;
                }
                foreach (var action in AssemblyUtil.StateToEventsLookup[state])
                {
                    _actions[(topic, action.Name)] = action;
                    _topics[action] = topic;
                }
            }

            // Store Topic from Actions
            foreach (var action in AssemblyUtil.Actions)
            {
                var streamAttribute = assemblyUtil.GetOne<StreamAttribute>(action);
                if (streamAttribute == null) continue;

                // Add Action
                var topic = streamAttribute.GetTopic(action);
                _actions[(topic, action.Name)] = action;
                _topics[action] = topic;
            }
        }

        public Type GetTypeFromTopic(string topic, string type) => _actions.GetValueOrDefault((topic, type));

        public string GetTopic(Type type) => _topics.GetValueOrDefault(type);

        public object GetPayload(string topic, ITopicMessage topicMessage)
        {
            var action = GetTypeFromTopic(topic, topicMessage.Type);
            return JsonSerializer.Deserialize(topicMessage.Payload, action);
        }

        public T Upgrade<T>(string topic, T topicMessage) where T : ITopicMessage
        {
            var action = GetTypeFromTopic(topic, topicMessage.Type);
            var payload = JsonSerializer.Deserialize(topicMessage.Payload, action);
            var upgrade = action
                .GetInterfaces()
                .FirstOrDefault(x => x.Name == typeof(IUpgradeTo<>).Name)?.GetMethod("Upgrade");

            // If no upgrade return original message
            if (upgrade == null) return topicMessage;

            upgrade?.Invoke(payload, null);
            return (T)Activator.CreateInstance(typeof(T), topicMessage.StreamId, payload);
        }
    }
}

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
        private readonly Dictionary<string, Type> _topicActions = new();

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

                // Load Actions
                var commands = AssemblyUtil.StateToCommandsLookup.GetValueOrDefault(state);
                var requests = AssemblyUtil.StateToRequestsLookup.GetValueOrDefault(state);
                var events = AssemblyUtil.StateToEventsLookup.GetValueOrDefault(state);

                // Append Actions
                var actions = new List<Type>();
                if (commands?.Any() == true) actions.AddRange(commands);
                if (requests?.Any() == true) actions.AddRange(requests);
                if (events?.Any() == true) actions.AddRange(events);

                // Leave if no actions
                if (actions?.Any() != true) continue;

                // Add Actions
                foreach (var action in actions)
                {
                    var topic = GetTopic(streamAttribute, state);
                    _topicActions[GetKey(topic, action.Name)] = action;
                }
            }

            // Store Topic from Actions
            foreach (var action in AssemblyUtil.Actions)
            {
                var streamAttribute = assemblyUtil.GetOne<StreamAttribute>(action);
                if (streamAttribute == null) continue;

                // Add Action
                var topic = GetTopic(streamAttribute, action);
                _topicActions[GetKey(topic, action.Name)] = action;
            }
        }

        public Type GetTypeFromTopic(string topic, string type) => _topicActions.GetValueOrDefault(GetKey(topic, type));

        public string GetTopic(StreamAttribute streamAttribute, Type type)
        {
            return streamAttribute?.Topic?.Replace(StreamingConstants.TypeKey, type.Name);
        }

        public string GetTopic(Type type)
        {
            var streamAttribute = _assemblyUtil.GetOne<StreamAttribute>(type);
            return GetTopic(streamAttribute, type);
        }

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

        private static string GetKey(string topic, string type) => $"{topic}-{type}";
    }
}

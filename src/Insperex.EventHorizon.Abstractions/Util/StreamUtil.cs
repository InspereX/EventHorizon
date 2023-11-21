using System;
using System.Collections.Generic;
using Insperex.EventHorizon.Abstractions.Attributes;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;

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
                _topics[state] = streamAttribute.GetTopic(state);

                // Add Actions
                foreach (var action in AssemblyUtil.StateToCommandsLookup[state])
                {
                    var topic = streamAttribute.GetTopic(typeof(Command));
                    _actions[(topic, action.Name)] = action;
                    _topics[action] = topic;
                }
                foreach (var action in AssemblyUtil.StateToRequestsLookup[state])
                {
                    var topic = streamAttribute.GetTopic(typeof(Request));
                    _actions[(topic, action.Name)] = action;
                    _topics[action] = topic;
                }
                foreach (var action in AssemblyUtil.StateToEventsLookup[state])
                {
                    var topic = streamAttribute.GetTopic(typeof(Event));
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
    }
}

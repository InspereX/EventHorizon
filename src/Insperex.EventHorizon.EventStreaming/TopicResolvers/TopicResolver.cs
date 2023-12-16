using System;
using System.Collections.Generic;
using System.Linq;
using Insperex.EventHorizon.Abstractions.Attributes;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Models;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.Abstractions.Util;
using Insperex.EventHorizon.EventStreaming.Interfaces.Streaming;
using Insperex.EventHorizon.EventStreaming.Models;

namespace Insperex.EventHorizon.EventStreaming.TopicResolvers
{
    public class TopicResolver
    {
        private readonly ITopicResolver _topicResolver;
        private readonly AttributeUtil _attributeUtil;
        private readonly Dictionary<(string, string), Type> _actions = new();

        public TopicResolver(
            ITopicResolver topicResolver,
            AttributeUtil attributeUtil)
        {
            _topicResolver = topicResolver;
            _attributeUtil = attributeUtil;

            // Store Topic from States
            foreach (var state in AssemblyUtil.States)
            {
                var streamAttribute = attributeUtil.GetOne<StreamAttribute>(state);
                if (streamAttribute == null) continue;

                // Skip Views
                if (streamAttribute.SourceType != null) continue;

                // Add Actions
                AddActions<Command>(AssemblyUtil.StateToCommandsLookup[state]);
                AddActions<Request>(AssemblyUtil.StateToRequestsLookup[state]);
                AddActions<Response>(AssemblyUtil.StateToResponsesLookup[state]);
                AddActions<Event>(AssemblyUtil.StateToEventsLookup[state]);
            }

            // Store Topic from Actions
            AddActions<Command>(AssemblyUtil.Commands);
            AddActions<Request>(AssemblyUtil.Requests);
            AddActions<Response>(AssemblyUtil.Responses);
            AddActions<Event>(AssemblyUtil.Events);
        }

        public string GetTopic<TM>(Type stateType, bool isView = false, string topicPostfix = null) where TM : ITopicMessage =>
            GetTopics<TM>(stateType, isView, topicPostfix).FirstOrDefault();

        public string[] GetTopics<TM>(Type stateType, bool includeViews = false, string topicPostfix = null) where TM : ITopicMessage
        {
            var attributes = _attributeUtil.GetAll<StreamAttribute>(stateType);
            var topics = attributes
                .Where(x => includeViews || x.SourceType == null)
                .Select(x =>
                {
                    var action = typeof(TM);
                    var state = x.SourceType ?? stateType;
                    var topic = topicPostfix == null ? x.Topic : $"{x.Topic}-{topicPostfix}";
                    var newTopic = _topicResolver.GetTopic<TM>(state, topic);
                    var formatted = newTopic.Replace(StreamingConstants.TypeKey, action.Name);
                    return formatted;
                })
                .ToArray();

            return topics;
        }

        public MessageContext<T> CreateMessageContext<T>(TopicData topicData, T message) where T : ITopicMessage
        {
            var type = _actions.GetValueOrDefault((topicData.Topic, message.Type));
            return new MessageContext<T>(type, message, topicData);
        }

        private void AddActions<T>(Type[] types) where T : ITopicMessage
        {
            foreach (var type in types)
            {
                var topic = GetTopic<T>(type, false);
                if (topic == null) continue;

                _actions[(topic, type.Name)] = type;
            }
        }
    }
}

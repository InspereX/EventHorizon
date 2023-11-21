using System;
using System.Reflection;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Models;

namespace Insperex.EventHorizon.Abstractions.Attributes
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class StreamAttribute : Attribute
    {
        public string Topic { get; set; }
        public Type SourceType { get; set; }
        public bool OwnsTopic { get; set; }

        public StreamAttribute(string topic)
        {
            Topic = topic;
            OwnsTopic = true;
        }

        public StreamAttribute(Type sourceType)
        {
            var attr = sourceType.GetCustomAttribute<StreamAttribute>();
            if (attr == null) throw new Exception($"{sourceType.Name} is missing StreamAttribute");
            SourceType = sourceType;
            Topic = attr.Topic;
        }

        public string GetTopic(Type action)
        {
            return Topic.Replace(StreamingConstants.TypeKey, action.Name);
        }
    }
}

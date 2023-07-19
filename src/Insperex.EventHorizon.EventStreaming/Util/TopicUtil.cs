using System;
using System.Reflection;

namespace Insperex.EventHorizon.EventStreaming.Util
{
    public static class TopicUtil
    {
        private const string TypeKey = "$type";
        private const string AssemblyKey = "$assembly";
        private const string ActionKey = "$action";

        public static string TopicReplace(string topic, Type type, Type action, Assembly assembly)
        {
            return topic
                .Replace(TypeKey, type.Name)
                .Replace(ActionKey,  action.Name)
                .Replace(AssemblyKey,  assembly.GetName().Name);
        }
    }
}

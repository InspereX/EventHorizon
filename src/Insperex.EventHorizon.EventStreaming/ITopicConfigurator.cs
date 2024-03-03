using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;

namespace Insperex.EventHorizon.EventStreaming
{
    public interface ITopicConfigurator<TMessage, TPayload>
        where TMessage : ITopicMessage
        where TPayload : IPayload
    {
        string GetTopic(string senderId = null);
    }
}

using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;

namespace Insperex.EventHorizon.EventStore.Interfaces.Stores
{
    public interface IEventStore<T> : ICrudStore<Event> where T : IState
    {

    }
}

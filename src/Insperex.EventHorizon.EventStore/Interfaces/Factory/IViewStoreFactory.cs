using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.EventStore.Interfaces.Stores;
using Insperex.EventHorizon.EventStore.Models;

namespace Insperex.EventHorizon.EventStore.Interfaces.Factory;

public interface IViewStoreFactory<T> where T : class, IState
{
    public ICrudStore<View<T>> GetViewStore();
}

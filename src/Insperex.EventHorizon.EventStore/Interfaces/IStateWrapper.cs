using System;

namespace Insperex.EventHorizon.EventStore.Interfaces;

public interface IStateWrapper<T> : ICrudEntity
{
    public long SequenceId { get; set; }
    public T State { get; set; }
}

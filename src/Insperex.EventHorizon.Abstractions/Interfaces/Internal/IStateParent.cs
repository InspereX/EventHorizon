using System;

namespace Insperex.EventHorizon.Abstractions.Interfaces.Internal;

public interface IStateParent<T> : ICrudEntity
{
    public long SequenceId { get; set; }
    public T State { get; set; }
    public DateTime UpdatedDate { get; set; }
}

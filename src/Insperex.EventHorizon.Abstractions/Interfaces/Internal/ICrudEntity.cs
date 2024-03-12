using System;

namespace Insperex.EventHorizon.Abstractions.Interfaces.Internal;

public interface ICrudEntity
{
    public string Id { get; set; }
    public DateTime CreatedDate { get; set; }
}

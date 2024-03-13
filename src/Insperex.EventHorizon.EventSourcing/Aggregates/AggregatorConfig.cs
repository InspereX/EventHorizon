using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Serialization.Compression;

namespace Insperex.EventHorizon.EventSourcing.Aggregates;

public class AggregatorConfig<T> where T : IState
{
    public bool IsValidationEnabled { get; set; }
    public CompressionType? StateCompression { get; set; }
    public CompressionType? EventCompression { get; set; }
}

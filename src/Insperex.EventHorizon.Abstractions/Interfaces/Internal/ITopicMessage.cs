namespace Insperex.EventHorizon.Abstractions.Interfaces.Internal;

public interface ITopicMessage
{
    public string Id { get; set; }
    public string StreamId { get; set; }
}

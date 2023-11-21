using System.Linq;
using Bogus;
using Insperex.EventHorizon.Abstractions.Interfaces.Actions;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.EventStreaming.Samples.Models;

namespace Insperex.EventHorizon.EventStreaming.Test.Fakers;

public static class EventStreamingFakers
{
    private static readonly Faker Faker = new();
    private static readonly string[] StreamIds =
        Enumerable.Range(1, 100)
            .Select(i => Faker.Random.Guid().ToString())
            .ToArray();

    public static readonly Faker<Feed1PriceChanged> Feed1PriceChangedFaker = new Faker<Feed1PriceChanged>()
        .CustomInstantiator(x => new Feed1PriceChanged(x.PickRandom(StreamIds), x.Random.Int(1, 100)));

    public static readonly Faker<Feed2PriceChanged> Feed2PriceChangedFaker = new Faker<Feed2PriceChanged>()
        .CustomInstantiator(x => new Feed2PriceChanged(x.PickRandom(StreamIds), x.Random.Int(1, 100)));

    private static int _sequenceId;
    public static Event[] WrapEvents(IEvent[] events)
    {
        return events.Select(x => new Event(Faker.PickRandom(StreamIds), ++_sequenceId, x)).ToArray();
    }
}

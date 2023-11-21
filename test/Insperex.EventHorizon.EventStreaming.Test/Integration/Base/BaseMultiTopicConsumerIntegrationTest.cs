using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.EventStreaming.Publishers;
using Insperex.EventHorizon.EventStreaming.Samples.Models;
using Insperex.EventHorizon.EventStreaming.Subscriptions.Backoff;
using Insperex.EventHorizon.EventStreaming.Test.Fakers;
using Insperex.EventHorizon.EventStreaming.Test.Shared;
using Insperex.EventHorizon.EventStreaming.Test.Util;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Insperex.EventHorizon.EventStreaming.Test.Integration.Base;

[Trait("Category", "Integration")]
public abstract class BaseMultiTopicConsumerIntegrationTest : IAsyncLifetime
{
    protected readonly ITestOutputHelper _outputHelper;
    protected readonly TimeSpan _timeout;
    private Stopwatch _stopwatch;
    protected readonly StreamingClient _streamingClient;
    private readonly ListStreamConsumer<Event> _handler;
    private Publisher<Event> _publisher1;
    private Publisher<Event> _publisher2;
    private readonly PartialNackListStreamConsumer _partialNackHandler;
    private Event[] _events1;
    private Event[] _events2;
    protected Event[] _allEvents;

    protected BaseMultiTopicConsumerIntegrationTest(ITestOutputHelper outputHelper, IServiceProvider provider)
    {
        var random = new Random((int)DateTime.UtcNow.Ticks);
        UniqueTestId = $"{random.Next()}";

        Provider = provider;
        _outputHelper = outputHelper;
        _streamingClient = provider.GetRequiredService<StreamingClient>();
        _timeout = TimeSpan.FromSeconds(20);
        _handler = new ListStreamConsumer<Event>();
        _partialNackHandler = new(_outputHelper, 0.03, 3, 2,
            100, false);
    }

    protected string UniqueTestId { get; init; }

    protected IServiceProvider Provider { get; init; }

    public async Task InitializeAsync()
    {
        // Publish Events
        _events1 = EventStreamingFakers.WrapEvents(EventStreamingFakers.Feed1PriceChangedFaker.Generate(500).ToArray());
        _events2 = EventStreamingFakers.WrapEvents(EventStreamingFakers.Feed2PriceChangedFaker.Generate(500).ToArray());
        _allEvents = _events1.Concat(_events2).ToArray();
        _publisher1 = _streamingClient.CreatePublisher<Event>().AddStream<Feed1PriceChanged>().Build();
        _publisher2 = _streamingClient.CreatePublisher<Event>().AddStream<Feed2PriceChanged>().Build();
        await _publisher1.PublishAsync(_events1);
        await _publisher2.PublishAsync(_events2);

        // Setup
        _stopwatch = Stopwatch.StartNew();
    }

    public virtual async Task DisposeAsync()
    {
        _outputHelper.WriteLine($"Test Ran in {_stopwatch.ElapsedMilliseconds}ms");
        await _streamingClient.GetAdmin<Event>().DeleteTopicAsync(typeof(Feed1PriceChanged));
        await _streamingClient.GetAdmin<Event>().DeleteTopicAsync(typeof(Feed2PriceChanged));
        await _publisher1.DisposeAsync();
        await _publisher2.DisposeAsync();
    }

    [Fact]
    public async Task SubscribeToMultipleTopics()
    {
        // Consume
        await using var subscription = await _streamingClient.CreateSubscription<Event>()
            .AddStream<Feed1PriceChanged>()
            .AddStream<Feed2PriceChanged>()
            .BatchSize(_allEvents.Length/10)
            .OnBatch(_handler.OnBatch)
            .Build()
            .StartAsync();

        // Assert
        await WaitUtil.WaitForTrue(() => _allEvents.Length <= _handler.List.Count, _timeout);
        AssertUtil.AssertEventsValid(_allEvents, _handler.List.ToArray());
    }

    [Fact]
    public async Task SubscribeToTopicsSeparately()
    {
        // Consume
        var builder = _streamingClient.CreateSubscription<Event>()
            .BatchSize(_allEvents.Length / 10);

        await using var subscription1 = await builder.AddStream<Feed1PriceChanged>().OnBatch(_handler.OnBatch).Build().StartAsync();
        await using var subscription2 = await builder.AddStream<Feed2PriceChanged>().OnBatch(_handler.OnBatch).Build().StartAsync();

        // Assert
        await WaitUtil.WaitForTrue(() => _allEvents.Length <= _handler.List.Count, _timeout);
        AssertUtil.AssertEventsValid(_allEvents, _handler.List.ToArray());
    }

    [Fact]
    public async Task TestSingleConsumerWithAdvancedFailures()
    {
        // Consume
        await using var subscription = await _streamingClient.CreateSubscription<Event>()
            .SubscriptionName($"Fails_{UniqueTestId}")
            .AddStream<Feed1PriceChanged>()
            .AddStream<Feed2PriceChanged>()
            .BatchSize(_allEvents.Length / 10)
            .GuaranteeMessageOrderOnFailure(true)
            .ExponentialBackoff(b => b
                .StartAt(TimeSpan.FromMilliseconds(10))
                .Max(TimeSpan.FromSeconds(15)))
            .OnBatch(_partialNackHandler.OnBatch) // Will nack at least some messages.
            .Build()
            .StartAsync();

        // Wait for List
        await WaitUtil.WaitForTrue(() => _allEvents.Length <= _partialNackHandler.List.Count, _timeout);

        _partialNackHandler.Report();
        Assert.True(_partialNackHandler.RedeliveredMessages == 0,
            $"There were {_partialNackHandler.RedeliveredMessages} redeliveries of previously-accepted messages. Should not have any!");

        // Assert
        // Expecting the advanced failure handling to preserve message ordering despite the nacks.
        AssertUtil.AssertEventsValid(_allEvents, _partialNackHandler.List.ToArray());
    }
}

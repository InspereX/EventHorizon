﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.EventStore.Interfaces;
using Insperex.EventHorizon.EventStreaming;
using Insperex.EventHorizon.EventStreaming.Subscriptions;
using Microsoft.Extensions.Hosting;

namespace Insperex.EventHorizon.EventSourcing.Aggregates;

public class AggregateConsumerHostedService<TParent, TAction, T> : IHostedService
    where TParent : class, IStateParent<T>, new()
    where T : class, IState
    where TAction : class, ITopicMessage, new()
{
    private readonly Aggregator<TParent, T> _aggregator;
    private readonly Subscription<TAction> _subscription;

    public AggregateConsumerHostedService(
        StreamingClient streamingClient,
        Aggregator<TParent, T> aggregator)
    {
        _aggregator = aggregator;

        var config = _aggregator.GetConfig();

        var builder = streamingClient.CreateSubscription<TAction>()
            .SubscriptionName($"Apply-{typeof(TAction).Name}-{typeof(T).Name}")
            .AddStream<T>()
            .OnBatch(async x =>
            {
                var messages = x.Messages.Select(m => m.Data).ToArray();
                var responses = await aggregator.HandleBatchAsync((dynamic)messages, x.CancellationToken);
                await aggregator.PublishResponseAsync(responses);
            });

        if(config.BatchSize != null)
            builder = builder.BatchSize(config.BatchSize.Value);

        _subscription = builder.Build();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Try to Refresh any Missing or Outdated Snapshots
        if (_aggregator.GetConfig().IsRebuildEnabled)
            await _aggregator.RebuildAllAsync(cancellationToken);

        // Start Command Subscription
        await _subscription.StartAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _subscription.StopAsync();
    }
}

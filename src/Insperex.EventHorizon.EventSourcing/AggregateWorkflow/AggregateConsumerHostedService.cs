using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.EventStore.Interfaces;
using Insperex.EventHorizon.EventStreaming;
using Insperex.EventHorizon.EventStreaming.Subscriptions;
using Microsoft.Extensions.Hosting;

namespace Insperex.EventHorizon.EventSourcing.AggregateWorkflow;

public class AggregateConsumerHostedService<TStateWrapper, TState, TMessage> : IHostedService
    where TStateWrapper : class, IStateWrapper<TState>, new()
    where TState : class, IState
    where TMessage : class, ITopicMessage, new()
{
    private readonly AggregateWorkflow<TStateWrapper, TState> _aggregateWorkflow;
    private readonly Subscription<TMessage> _subscription;

    public AggregateConsumerHostedService(
        StreamingClient streamingClient,
        AggregateWorkflow<TStateWrapper, TState> aggregateWorkflow,
        Func<SubscriptionBuilder<TMessage>, SubscriptionBuilder<TMessage>> onBuildSubscription = null)
    {
        _aggregateWorkflow = aggregateWorkflow;

        var builder = streamingClient.CreateSubscription<TMessage>()
            .SubscriptionName($"Apply-{typeof(TMessage).Name}-{typeof(TState).Name}")
            .AddStream<TState>()
            .OnBatch(OnBatch);

        if (onBuildSubscription != null) builder = onBuildSubscription(builder);

        // Config Subscription
        if (_aggregateWorkflow.Config.BatchSize != default)
            builder.BatchSize(_aggregateWorkflow.Config.BatchSize);

        _subscription = builder.Build();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _subscription.StartAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _subscription.StopAsync();
    }

    private async Task OnBatch(SubscriptionContext<TMessage> batch)
    {
        // Consume Messages
        var messages = batch.Messages.Select(m => m.Data).ToArray();
        var aggregateDict = await _aggregateWorkflow.ConsumeAsync(messages, batch.CancellationToken);
        await _aggregateWorkflow.TryPublishResponsesAsync(aggregateDict);

        // Get Failed StreamIds
        var failedIds = aggregateDict.Values
            .Where(x => x.Error != null)
            .Select(x => x.Id)
            .ToArray();

        // Get Failed Messages
        var failedMessages = batch.Messages
            .Where(x => failedIds.Contains(x.Data.StreamId))
            .ToArray();

        // Nack
        batch.Nack(failedMessages);
    }
}

﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Models;
using Insperex.EventHorizon.EventStreaming.Interfaces.Streaming;
using Insperex.EventHorizon.EventStreaming.Pulsar.Utils;
using Insperex.EventHorizon.EventStreaming.Subscriptions;
using Insperex.EventHorizon.EventStreaming.Tracing;
using Insperex.EventHorizon.EventStreaming.Util;
using Pulsar.Client.Api;
using Pulsar.Client.Common;
using Pulsar.Client.Otel;
using SubscriptionType = Pulsar.Client.Common.SubscriptionType;

namespace Insperex.EventHorizon.EventStreaming.Pulsar;

public class PulsarTopicConsumer<T> : ITopicConsumer<T> where T : ITopicMessage, new()
{
    private readonly PulsarClientResolver _clientResolver;
    private readonly SubscriptionConfig<T> _config;
    private readonly ITopicAdmin<T> _admin;
    private readonly OtelConsumerInterceptor.OTelConsumerInterceptor<T> _intercept;
    private IConsumer<T> _consumer;
    private readonly Dictionary<string, Message<T>> _unackedMessages = new();
    private readonly SemaphoreSlim _semaphoreSlim;

    public PulsarTopicConsumer(
        PulsarClientResolver clientResolver,
        SubscriptionConfig<T> config,
        ITopicAdmin<T> admin)
    {
        _clientResolver = clientResolver;
        _config = config;
        _admin = admin;
        _intercept = new OtelConsumerInterceptor.OTelConsumerInterceptor<T>(
            TraceConstants.ActivitySourceName, PulsarClient.Logger);
        _semaphoreSlim = new SemaphoreSlim(1, 1);
    }

    public async Task InitAsync()
    {
        _consumer = await GetConsumerAsync();
    }

    public async Task<MessageContext<T>[]> NextBatchAsync(CancellationToken ct)
    {
        try
        {
            // var consumer = await GetConsumerAsync();
            // var messages = await consumer.BatchReceiveAsync(ct);
            var messages = await GetNext(ct);
            if (!messages.Any())
            {
                await Task.Delay(_config.NoBatchDelay, ct);
                return null;
            }

            var contexts =  messages
                .Select((x,i) =>
                {
                    // Note: x.MessageId.TopicName is null, when single tropic
                    var topic = _config.Topics.Length == 1 ? _config.Topics.First() : x.MessageId.TopicName;

                    var id = Guid.NewGuid().ToString();
                    var context = new MessageContext<T>
                    {
                        Data = x.GetValue(),
                        TopicData = PulsarMessageMapper.MapTopicData(id, x, topic)
                    };

                    _unackedMessages[id] = x;

                    return context;
                })
                .ToArray();

            return contexts;
        }
        catch (AlreadyClosedException)
        {
            // Ignore AlreadyClosedException
            return null;
        }

    }

    public async Task<Message<T>[]> GetNext(CancellationToken ct)
    {
        var list = new List<Message<T>>();

        var consumer = await GetConsumerAsync();
        var messages = await consumer.BatchReceiveAsync(ct);
        return messages.ToArray();
        try
        {
            while (list.Count < _config.BatchSize && !ct.IsCancellationRequested)
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                var message = await consumer.ReceiveAsync(cts.Token);
                list.Add(message);
            }
        }
        catch (TaskCanceledException e)
        {
            // ignore
        }

        return list.ToArray();
    }

    public async Task AckAsync(params MessageContext<T>[] messages)
    {
        var consumer = await GetConsumerAsync();
        if (messages?.Any() != true) return;
        foreach (var message in messages)
        {
            var unackedMessage = _unackedMessages[message.TopicData.Id];
            await consumer.AcknowledgeAsync(unackedMessage.MessageId);
            _unackedMessages.Remove(message.TopicData.Id);
        }
    }

    public async Task NackAsync(params MessageContext<T>[] messages)
    {
        var consumer = await GetConsumerAsync();
        if (messages?.Any() != true) return;
        foreach (var message in messages)
        {
            var unackedMessage = _unackedMessages[message.TopicData.Id];
            await consumer.NegativeAcknowledge(unackedMessage.MessageId);
            _unackedMessages.Remove(message.TopicData.Id);
        }
    }

    private async Task<IConsumer<T>> GetConsumerAsync()
    {
        // Defensive
        if (_consumer != null) return _consumer;

        // Lock is for Parallel Requests for some Producer
        await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(3));

        // Second Release is if they got past first
        if (_consumer != null) return _consumer;

        // Ensure Topic Exists
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        foreach (var topic in _config.Topics)
            await _admin.RequireTopicAsync(topic, cts.Token);

        var client = await _clientResolver.GetPulsarClientAsync();
        var builder = client.NewConsumer(Schema.JSON<T>())
            .ConsumerName(NameUtil.AssemblyNameWithGuid)
            .SubscriptionType(GetSubscriptionType(_config.SubscriptionType))
            .SubscriptionName(_config.SubscriptionName)
            .Intercept(_intercept);

        if (_config.Topics != null)
            builder = _config.Topics.Length == 1
                ? builder.Topic(_config.Topics.First())
                : builder.Topics(_config.Topics);

        if (_config.IsBeginning != null)
            builder = builder.SubscriptionInitialPosition(
                _config.IsBeginning == true
                    ? SubscriptionInitialPosition.Earliest
                    : SubscriptionInitialPosition.Latest);

        if (_config.BatchSize != null)
            builder = builder.ReceiverQueueSize(_config.BatchSize.Value);

        var consumer = await builder.SubscribeAsync();

        if (_config.StartDateTime != null)
            await consumer.SeekAsync(_config.StartDateTime.Value.Ticks);

        // Return
        return _consumer = consumer;
    }

    private static SubscriptionType GetSubscriptionType(Abstractions.Models.SubscriptionType subscriptionType)
    {
        return subscriptionType switch
        {
            Abstractions.Models.SubscriptionType.Exclusive => SubscriptionType.Exclusive,
            Abstractions.Models.SubscriptionType.Shared => SubscriptionType.Shared,
            Abstractions.Models.SubscriptionType.Failover => SubscriptionType.Failover,
            Abstractions.Models.SubscriptionType.KeyShared => SubscriptionType.KeyShared,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public async ValueTask DisposeAsync()
    {
        if(_consumer != null) await _consumer.DisposeAsync();
        _consumer = null;
    }
}

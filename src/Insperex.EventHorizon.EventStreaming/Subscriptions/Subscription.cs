﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Models;
using Insperex.EventHorizon.EventStreaming.Extensions;
using Insperex.EventHorizon.EventStreaming.Interfaces.Streaming;
using Insperex.EventHorizon.EventStreaming.Tracing;
using Microsoft.Extensions.Logging;

namespace Insperex.EventHorizon.EventStreaming.Subscriptions;

public class Subscription<T> : IAsyncDisposable where T : class, ITopicMessage, new()
{
    private readonly SubscriptionConfig<T> _config;
    private readonly ILogger<Subscription<T>> _logger;
    private readonly ITopicConsumer<T> _consumer;
    private readonly ITopicAdmin<T> _admin;
    private readonly ConcurrentQueue<MessageContext<T>[]> _queue = new();
    private bool _disposed;
    private bool _running;
    private bool _stopped;
    private readonly int _queueLimit;

    public Subscription(IStreamFactory factory, SubscriptionConfig<T> config, ILogger<Subscription<T>> logger)
    {
        _config = config;
        _logger = logger;
        _admin = factory.CreateAdmin<T>();
        _consumer = factory.CreateConsumer(_config);
        _queueLimit = 5;
    }

    public async Task<Subscription<T>> StartAsync()
    {
        if (_running) return this;
        _running = true;
        _stopped = false;

        // Initialize
        await _consumer.InitAsync();

        _logger.LogInformation("Subscription - Started {@Config}", _config);

        // Start Loop
        if (_config.IsPreload)
        {
            Task.Run(EnqueueLoop);
            Task.Run(DequeueLoop);
        }
        else
        {
            Task.Run(BasicLoop);
        }

        return this;
    }

    public async Task<Subscription<T>> StopAsync()
    {
        if (!_running) return this;

        // Cancel
        _running = false;
        // while (!_stopped)
        //     await Task.Delay(TimeSpan.FromMilliseconds(250));

        // Cleanup
        _logger.LogInformation("Subscription - Stopped {@Config}", _config);

        return this;
    }

    public async Task DeleteTopicsAsync()
    {
        foreach (var topic in _config.Topics)
            await _admin.DeleteTopicAsync(topic, CancellationToken.None);
    }

    private async void BasicLoop()
    {
        while (_running)
        {
            try
            {
                var batch = await LoadEvents();
                if (batch?.Any() == true)
                    await OnEvents(batch);
                else
                    await Task.Delay(_config.NoBatchDelay);
            }
            catch (TaskCanceledException)
            {
                // Ignore Cancels
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscription - Unhandled Exception {Message} {Subscription}", ex.Message, _config.SubscriptionName);
            }
        }
        _stopped = true;
    }

    private async void EnqueueLoop()
    {
        while (_running)
        {
            try
            {
                if ( _queue.Count <= _queueLimit)
                {
                    var batch = await LoadEvents();
                    if (batch?.Any() == true)
                        _queue.Enqueue(batch);
                    else
                        await Task.Delay(_config.NoBatchDelay);
                }
                else
                    await Task.Delay(_config.NoBatchDelay);
            }
            catch (TaskCanceledException)
            {
                // Ignore Cancels
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscription - Unhandled Exception {Message} {Subscription}", ex.Message, _config.SubscriptionName);
            }
        }
        _stopped = true;
    }

    private async void DequeueLoop()
    {
        while (_running)
        {
            try
            {
                var any = _queue.TryDequeue(out var batch);
                if (any)
                    await OnEvents(batch);
                else
                    await Task.Delay(_config.NoBatchDelay);
            }
            catch (TaskCanceledException)
            {
                // Ignore Cancels
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscription - Unhandled Exception {Message} {Subscription}", ex.Message, _config.SubscriptionName);
            }
        }
        _stopped = true;
    }

    private async Task<MessageContext<T>[]> LoadEvents()
    {
        using var activity = TraceConstants.ActivitySource.StartActivity();
        try
        {
            var sw = Stopwatch.StartNew();
            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var batch = await _consumer.NextBatchAsync(cts.Token);
            activity?.SetTag(TraceConstants.Tags.Count, batch?.Length ?? 0);
            activity?.SetStatus(ActivityStatusCode.Ok);

            // Upgrade Actions
            if (batch?.Any() == true)
            {
                // foreach (var item in batch)
                //     item.Data = item.Data.Upgrade();

                if(_config.IsLoggingActivity)
                    _logger.LogInformation("Subscription - Loaded {Type}(s) {Count} in {Duration} {Subscription}",
                    typeof(T).Name, batch.Length, sw.ElapsedMilliseconds, _config.SubscriptionName);
            }

            return batch;
        }
        catch (TaskCanceledException)
        {
            // ignore
            return Array.Empty<MessageContext<T>>();
        }
        catch (Exception ex)
        {
            // ignore disposed
            if (_disposed) return Array.Empty<MessageContext<T>>();

            // log error
            _logger.LogError(ex, "Subscription - Failed to load events => {Error} {Subscription}", ex.Message, _config.SubscriptionName);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private async Task OnEvents(MessageContext<T>[] batch)
    {
        var sw = Stopwatch.StartNew();
        using var activity = TraceConstants.ActivitySource.StartActivity();
        var context = new SubscriptionContext<T> { Messages = batch };
        try
        {
            await _config.OnBatch(context);

            // Ack/Nack Lists
            await _consumer.AckAsync(context.AckList.ToArray());
            await _consumer.NackAsync(context.NackList.ToArray());

            // Auto-Ack
            var list = batch
                .Where(x => !context.NackList.Contains(x))
                .Where(x => !context.AckList.Contains(x))
                .ToArray();
            await _consumer.AckAsync(list.ToArray());

            // Logging
            var min = batch.Min(x => x.TopicData.CreatedDate);
            var max = batch.Max(x => x.TopicData.CreatedDate);
            if(_config.IsLoggingActivity)
                _logger.LogInformation("Subscription - Processed {Type}(s) {Count} in {Duration}, from {Start}-{End} {Subscription}",
                typeof(T).Name, batch.Length, sw.ElapsedMilliseconds, min, max, _config.SubscriptionName);
            activity?.SetTag(TraceConstants.Tags.Count, batch.Length);
            activity?.SetTag(TraceConstants.Tags.Start, min);
            activity?.SetTag(TraceConstants.Tags.End, max);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Subscription - Failed to process {Message} {Subscription}", ex.Message, _config.SubscriptionName);
            await _consumer.NackAsync(batch);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        await StopAsync();
    }
}

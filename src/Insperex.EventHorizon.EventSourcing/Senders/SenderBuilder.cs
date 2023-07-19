﻿using System;
using System.Net;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Actions;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.EventStreaming;
using Insperex.EventHorizon.EventStreaming.Interfaces.Streaming;
using Microsoft.Extensions.Logging;

namespace Insperex.EventHorizon.EventSourcing.Senders;

public class SenderBuilder
{
    private readonly SenderSubscriptionTracker _subscriptionTracker;
    private readonly StreamingClient _streamingClient;
    private readonly ILoggerFactory _loggerFactory;
    private Func<Request, HttpStatusCode, string, IResponse> _getErrorResult;
    private TimeSpan _timeout = TimeSpan.FromSeconds(120);
    private int _batchSize = 1000;

    public SenderBuilder(SenderSubscriptionTracker subscriptionTracker, StreamingClient streamingClient, ILoggerFactory loggerFactory)
    {
        _subscriptionTracker = subscriptionTracker;
        _streamingClient = streamingClient;
        _loggerFactory = loggerFactory;
    }

    public SenderBuilder Timeout(TimeSpan timeout)
    {
        _timeout = timeout;
        return this;
    }

    public SenderBuilder GetErrorResult(Func<Request, HttpStatusCode, string, IResponse> getErrorResult)
    {
        _getErrorResult = getErrorResult;
        return this;
    }

    public SenderBuilder BatchSize(int batchSize)
    {
        _batchSize = batchSize;
        return this;
    }

    public Sender Build()
    {
        var config = new SenderConfig
        {
            Timeout = _timeout,
            GetErrorResult = _getErrorResult,
            BatchSize = _batchSize
        };

        return new Sender(_subscriptionTracker, _streamingClient, config, _loggerFactory.CreateLogger<Sender>());
    }
}

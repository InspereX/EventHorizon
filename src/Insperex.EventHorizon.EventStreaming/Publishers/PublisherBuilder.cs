﻿using System;
using System.Linq;
using System.Threading;
using Insperex.EventHorizon.Abstractions.Exceptions;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.EventStreaming.Interfaces.Streaming;
using Insperex.EventHorizon.EventStreaming.TopicResolvers;
using Microsoft.Extensions.Logging;

namespace Insperex.EventHorizon.EventStreaming.Publishers;

public class PublisherBuilder<T> where T : class, ITopicMessage, new()
{
    private readonly TopicResolver _topicResolver;
    private readonly IStreamFactory _factory;
    private readonly ILoggerFactory _loggerFactory;
    private string _topic;
    private TimeSpan _sendTimeout = TimeSpan.FromMinutes(2);
    private bool _isGuaranteed;
    private int _batchSize = 100;
    private bool _isOrderGuaranteed = true;

    public PublisherBuilder(TopicResolver topicResolver, IStreamFactory factory, ILoggerFactory loggerFactory)
    {
        _topicResolver = topicResolver;
        _loggerFactory = loggerFactory;
        _factory = factory;
    }

    internal PublisherBuilder<T> AddTopic(string topicName = null)
    {
        if (_topic != null) throw new MultiTopicNotSupportedException<PublisherBuilder<T>>();
        _topic = topicName;
        return this;
    }

    public PublisherBuilder<T> AddStream<TS>(string topicName = null)
    {
        if (_topic != null) throw new MultiTopicNotSupportedException<PublisherBuilder<T>>();
        _topic = _topicResolver.GetTopics<T>(typeof(TS), false, topicName).FirstOrDefault();
        return this;
    }

    public PublisherBuilder<T> IsGuaranteed(bool isGuaranteed)
    {
        _isGuaranteed = isGuaranteed;
        return this;
    }

    public PublisherBuilder<T> IsOrderGuaranteed(bool isOrderGuaranteed)
    {
        _isOrderGuaranteed = isOrderGuaranteed;
        return this;
    }

    public PublisherBuilder<T> SendTimeout(TimeSpan sendTimeout)
    {
        _sendTimeout = sendTimeout;
        return this;
    }

    public PublisherBuilder<T> BatchSize(int batchSize)
    {
        _batchSize = batchSize;
        return this;
    }

    public Publisher<T> Build()
    {
        var config = new PublisherConfig
        {
            Topic = _topic,
            IsGuaranteed = _isGuaranteed,
            IsOrderGuaranteed = _isOrderGuaranteed,
            SendTimeout = _sendTimeout,
            BatchSize = _batchSize
        };
        var logger = _loggerFactory.CreateLogger<Publisher<T>>();

        // Create
        return new Publisher<T>(_factory, config, logger);
    }
}

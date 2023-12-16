using System;
using System.Linq;
using System.Threading;
using Insperex.EventHorizon.Abstractions.Exceptions;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.EventStreaming.Interfaces.Streaming;
using Insperex.EventHorizon.EventStreaming.TopicResolvers;
using Microsoft.Extensions.Logging;

namespace Insperex.EventHorizon.EventStreaming.Readers;

public class ReaderBuilder<T> where T : class, ITopicMessage, new()
{
    private readonly TopicResolver _topicResolver;
    private readonly IStreamFactory _streamFactory;
    private DateTime? _endDateTime;
    private bool _isBeginning = true;
    private DateTime? _startDateTime;
    private string[] _keys;
    private string _topic;

    public ReaderBuilder(TopicResolver topicResolver, IStreamFactory streamFactory)
    {
        _topicResolver = topicResolver;
        _streamFactory = streamFactory;
    }

    public ReaderBuilder<T> AddStream<TS>(string topicName = null)
    {
        if (_topic != null) throw new MultiTopicNotSupportedException<ReaderBuilder<T>>();
        _topic = _topicResolver.GetTopics<T>(typeof(TS), true, topicName).FirstOrDefault();
        return this;
    }

    public ReaderBuilder<T> Keys(params string[] keys)
    {
        _keys = keys;
        return this;
    }

    public ReaderBuilder<T> StartDateTime(DateTime? startDateTime)
    {
        _startDateTime = startDateTime;
        return this;
    }

    public ReaderBuilder<T> EndDateTime(DateTime? endDateTime)
    {
        _endDateTime = endDateTime;
        return this;
    }

    public ReaderBuilder<T> IsBeginning(bool isBeginning)
    {
        _isBeginning = isBeginning;
        return this;
    }

    public Reader<T> Build()
    {
        var config = new ReaderConfig
        {
            Topic = _topic,
            Keys = _keys,
            StartDateTime = _startDateTime,
            EndDateTime = _endDateTime,
            IsBeginning = _isBeginning
        };
        var consumer = _streamFactory.CreateReader<T>(config);

        return new Reader<T>(consumer);
    }
}

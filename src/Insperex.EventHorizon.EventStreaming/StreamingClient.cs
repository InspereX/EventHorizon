using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Util;
using Insperex.EventHorizon.EventStreaming.Admins;
using Insperex.EventHorizon.EventStreaming.Interfaces.Streaming;
using Insperex.EventHorizon.EventStreaming.Publishers;
using Insperex.EventHorizon.EventStreaming.Readers;
using Insperex.EventHorizon.EventStreaming.Subscriptions;
using Insperex.EventHorizon.EventStreaming.TopicResolvers;
using Microsoft.Extensions.Logging;

namespace Insperex.EventHorizon.EventStreaming;

public class StreamingClient
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IStreamFactory _streamFactory;
    private readonly TopicResolver _topicResolver;

    public StreamingClient(IStreamFactory streamFactory, TopicResolver topicResolver, ILoggerFactory loggerFactory)
    {
        _streamFactory = streamFactory;
        _topicResolver = topicResolver;
        _loggerFactory = loggerFactory;
    }

    public PublisherBuilder<T> CreatePublisher<T>() where T : class, ITopicMessage, new()
    {
        return new PublisherBuilder<T>(_topicResolver, _streamFactory, _loggerFactory);
    }

    public ReaderBuilder<T> CreateReader<T>() where T : class, ITopicMessage, new()
    {
        return new ReaderBuilder<T>(_topicResolver, _streamFactory);
    }

    public SubscriptionBuilder<T> CreateSubscription<T>() where T : class, ITopicMessage, new()
    {
        return new SubscriptionBuilder<T>(_topicResolver, _streamFactory, _loggerFactory);
    }

    public Admin<T> GetAdmin<T>() where T : class, ITopicMessage, new()
    {
        return new Admin<T>(_streamFactory.CreateAdmin<T>(), _topicResolver);
    }

    public TopicResolver GetTopicResolver()
    {
        return _topicResolver;
    }
}

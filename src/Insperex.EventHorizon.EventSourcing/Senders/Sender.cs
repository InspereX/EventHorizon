using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Actions;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.EventStreaming;
using Insperex.EventHorizon.EventStreaming.Publishers;
using Microsoft.Extensions.Logging;

namespace Insperex.EventHorizon.EventSourcing.Senders;

public class Sender
{
    private readonly SenderConfig _config;
    private readonly ILogger<Sender> _logger;
    private readonly SenderSubscriptionTracker _subscriptionTracker;
    private readonly StreamingClient _streamingClient;
    private readonly Dictionary<string, object> _publisherDict = new();

    public Sender(SenderSubscriptionTracker subscriptionTracker, StreamingClient streamingClient, SenderConfig config, ILogger<Sender> logger)
    {
        _subscriptionTracker = subscriptionTracker;
        _streamingClient = streamingClient;
        _config = config;
        _logger = logger;
    }

    public Task SendAsync<T>(string streamId, params ICommand<T>[] objs) where T : IState
    {
        var commands = objs.Select(x => new Command(streamId, x)).ToArray();
        return SendAsync<T>(new BatchCommand(streamId, commands));
    }

    public Task SendAsync<T>(params BatchCommand[] commands) where T : IState
    {
        return GetPublisher<BatchCommand, T>(null).PublishAsync(commands);
    }

    public async Task<TR> SendAndReceiveAsync<T, TR>(string streamId, IRequest<T, TR> obj)
        where T : IState
        where TR : IResponse<T>
    {
        var results = await SendAndReceiveAsync(streamId, new[] { obj });
        return results.First();
    }

    public async Task<TR[]> SendAndReceiveAsync<T, TR>(string streamId, IRequest<T, TR>[] objs)
        where T : IState
        where TR : IResponse<T>
    {
        var requests = objs.Select(x => new Request(streamId, x)).ToArray();
        var res = await SendAndReceiveAsync<T>(new BatchRequest(streamId, requests));
        return res.SelectMany(x => x.Payload).Select(x => JsonSerializer.Deserialize<TR>(x.Value.Payload)).ToArray();
    }

    public async Task<BatchResponse[]> SendAndReceiveAsync<T>(params BatchRequest[] batchRequests) where T : IState
    {
        // Ensure subscription is ready
        await _subscriptionTracker.TrackSubscription<T>();

        // Sent SenderId to respond to
        foreach (var request in batchRequests)
            request.SenderId = _subscriptionTracker.GetSenderId();

        // Send requests
        await GetPublisher<BatchRequest, T>(null).PublishAsync(batchRequests);

        // Wait for messages
        var responseDict = await _subscriptionTracker.GetResponses(batchRequests, _config.Timeout, _config.GetErrorResult);

        return responseDict.Values.ToArray();
    }

    private Publisher<TM> GetPublisher<TM, T>(string path) where TM : class, ITopicMessage, new()
    {
        var key = $"{typeof(TM).Name}-{path}";
        if (!_publisherDict.ContainsKey(key))
            _publisherDict[key] = _streamingClient.CreatePublisher<TM>()
                .BatchSize(_config.BatchSize)
                .AddStream<T>(path)
                .Build();

        return _publisherDict[key] as Publisher<TM>;
    }
}

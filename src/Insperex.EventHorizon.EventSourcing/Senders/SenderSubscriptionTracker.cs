using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Actions;
using Insperex.EventHorizon.Abstractions.Models;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.EventStreaming;
using Insperex.EventHorizon.EventStreaming.Extensions;
using Insperex.EventHorizon.EventStreaming.Subscriptions;
using Insperex.EventHorizon.EventStreaming.Util;
using Response = Insperex.EventHorizon.Abstractions.Models.TopicMessages.Response;

namespace Insperex.EventHorizon.EventSourcing.Senders;

public class SenderSubscriptionTracker : IAsyncDisposable
{
    private readonly StreamingClient _streamingClient;
    private readonly ConcurrentDictionary<Type, Subscription<BatchResponse>> _subscriptionDict = new ();
    private readonly ConcurrentDictionary<string, Response> _responseDict = new ();
    private readonly string _senderId;
    private bool _cleaning;

    public SenderSubscriptionTracker(StreamingClient streamingClient)
    {
        _streamingClient = streamingClient;
        _senderId = NameUtil.AssemblyNameWithGuid;

        // Used for when process is stopped mid way
        AppDomain.CurrentDomain.ProcessExit += OnExit;
    }

    public string GetSenderId() => _senderId;

    public async Task TrackSubscription<T>() where T : IState
    {
        var type = typeof(T);
        if(_subscriptionDict.ContainsKey(type))
            return;

        var subscription = _streamingClient.CreateSubscription<BatchResponse>()
            .SubscriptionType(SubscriptionType.Exclusive)
            .IsLoggingActivity(false)
            .OnBatch(async x =>
            {
                // Check Results
                var responses = x.Messages
                    .Select(m => m.Data)
                    .SelectMany(m => m.Payload.Values)
                    .ToArray();

                foreach (var response in responses)
                    _responseDict[response.Id] = response;
            })
            .BatchSize(100000)
            .AddStream<T>(_senderId)
            .IsBeginning(true)
            .Build();

        // warmup
        var publisher = _streamingClient.CreatePublisher<BatchResponse>()
            .AddStream<T>(_senderId)
            .IsLoggingActivity(false)
            .IsGuaranteed(true)
            .Build();

        // for (var i = 0; i < 10; i++)
        // {
        //     const string id = "Warmup";
        //     var req = new BatchResponse(id, id, _senderId, Array.Empty<Response>());
        //     await publisher.PublishAsync(req);
        // }

        _subscriptionDict[type] = subscription;

        await subscription.StartAsync();
    }


    // public async Task<BatchResponse[]> GetResponses(BatchRequest[] requests)
    // {
    //     var responses = new List<BatchResponse>();
    //     do
    //     {
    //
    //     } while()
    // }

    public Response[] GetResponses(Request[] requests, Func<Request, HttpStatusCode, string, IResponse> configGetErrorResult)
    {
        var responses = new List<Response>();
        foreach (var request in requests)
        {
            if (_responseDict.TryGetValue(request.Id, out var value))
            {
                // Add Response, Make Custom if needed
                if (value.Error != null)
                {
                    var custom = configGetErrorResult(request, (HttpStatusCode)value.StatusCode, value.Error);
                    value = new Response(value.Id, value.StreamId, custom, value.Error, value.StatusCode);
                }

                responses.Add(value);
            }
        }

        return responses.ToArray();
    }

    private async Task CleanupAsync()
    {
        _cleaning = true;
        foreach (var group in _subscriptionDict)
        {
            // Stop Subscription
            await group.Value.StopAsync();

            // Delete Topic
            await _streamingClient.GetAdmin<BatchResponse>().DeleteTopicAsync(group.Key, _senderId);
        }
        _subscriptionDict.Clear();
    }

    private void OnExit(object sender, EventArgs e)
    {
        if(!_cleaning)
            CleanupAsync().Wait();
    }

    public async ValueTask DisposeAsync()
    {
        if(!_cleaning)
            await CleanupAsync();
    }
}

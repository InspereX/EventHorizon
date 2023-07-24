using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Actions;
using Insperex.EventHorizon.Abstractions.Models;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.EventStreaming;
using Insperex.EventHorizon.EventStreaming.Extensions;
using Insperex.EventHorizon.EventStreaming.Subscriptions;
using Insperex.EventHorizon.EventStreaming.Util;
using Microsoft.Extensions.Logging;
using Response = Insperex.EventHorizon.Abstractions.Models.TopicMessages.Response;

namespace Insperex.EventHorizon.EventSourcing.Senders;

public class SenderSubscriptionTracker : IAsyncDisposable
{
    private readonly StreamingClient _streamingClient;
    private readonly ILogger<SenderSubscriptionTracker> _logger;
    private readonly ConcurrentDictionary<Type, Subscription<BatchResponse>> _subscriptionDict = new ();
    private readonly ConcurrentDictionary<string, BatchResponse> _batchResDict = new ();
    private readonly string _senderId;
    private bool _cleaning;

    public SenderSubscriptionTracker(StreamingClient streamingClient, ILogger<SenderSubscriptionTracker> logger)
    {
        _streamingClient = streamingClient;
        _logger = logger;
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
                foreach (var message in x.Messages)
                    _batchResDict[message.Data.Id] = message.Data;
            })
            .BatchSize(100000)
            .AddStream<T>(_senderId)
            .IsBeginning(true)
            .Build();

        // // warmup
        // var publisher = _streamingClient.CreatePublisher<BatchResponse>()
        //     .AddStream<T>(_senderId)
        //     .IsLoggingActivity(false)
        //     .IsGuaranteed(true)
        //     .Build();

        // for (var i = 0; i < 10; i++)
        // {
        //     const string id = "Warmup";
        //     var req = new BatchResponse(id, id, _senderId, Array.Empty<Response>());
        //     await publisher.PublishAsync(req);
        // }

        _subscriptionDict[type] = subscription;

        await subscription.StartAsync();
    }


    public async Task<Dictionary<string, BatchResponse>> GetResponses(BatchRequest[] batchRequests, TimeSpan timeout,
        Func<Request, HttpStatusCode, string, IResponse> configGetErrorResult)
    {
        var sw = Stopwatch.StartNew();

        var cts = new CancellationTokenSource((int)timeout.TotalMilliseconds);
        var batchRequestDict = batchRequests.ToDictionary(x => x.Id);
        var batchResDict = new Dictionary<string, BatchResponse>();

        while(!cts.IsCancellationRequested && batchRequests.Length != batchResDict.Count)
        {
            foreach (var batchRequest in batchRequests)
                if (_batchResDict.TryGetValue(batchRequest.Id, out var value))
                {
                    batchResDict.Add(batchRequest.Id, value);
                    _batchResDict.Remove(batchRequest.Id, out var deleted);

                }

            await Task.Delay(200);
        }

        // Add Details To Error Models
        foreach (var batchResponse in batchResDict.Values)
        {
            var reqDict = batchRequestDict[batchResponse.Id].Payload;
            var resDict = batchResponse.Payload;
            foreach (var key in batchResponse.Payload.Keys)
            {
                var res = resDict[key];
                if (res.Error == null) continue;

                var req = reqDict[key];
                var custom = configGetErrorResult(req, (HttpStatusCode)res.StatusCode, res.Error);
                var value = new Response(res.Id, res.StreamId, custom, res.Error, res.StatusCode);
                resDict[key] = value;
            }
        }

        // Add Timed Out Results
        foreach (var batchRequest in batchRequests)
        {
            if (batchResDict.ContainsKey(batchRequest.Id)) continue;

            const string error = "Request Timed Out";
            var responses = batchRequest.Payload.Values
                .Select(x => new Response(x.Id, x.StreamId,
                    configGetErrorResult?.Invoke(x, HttpStatusCode.RequestTimeout, error), error,
                    (int)HttpStatusCode.RequestTimeout))
                .ToArray();

            batchResDict[batchRequest.Id] = new BatchResponse(batchRequest.Id, batchRequest.StreamId,
                batchRequest.SenderId, responses.ToArray());
        }

        // Log Error
        var errors = batchResDict
            .SelectMany(x => x.Value.Payload.Values)
            .Where(x => x.Error != null)
            .GroupBy(x => x.Error)
            .ToArray();
        foreach (var group in errors)
            _logger.LogError("Sender - Response Error(s) {Count} => {Error}", group.Count(), group.Key);

        if(errors.Any() != true)
            _logger.LogInformation("Sender - Received All Responses {Count} in {Duration}", batchResDict.Count, sw.ElapsedMilliseconds);

        return batchResDict;
    }

    // public Response[] GetResponses(Request[] requests, Func<Request, HttpStatusCode, string, IResponse> configGetErrorResult)
    // {
    //     var responses = new List<Response>();
    //     foreach (var request in requests)
    //     {
    //         if (_responseDict.TryGetValue(request.Id, out var value))
    //         {
    //             // Add Response, Make Custom if needed
    //             if (value.Error != null)
    //             {
    //                 var custom = configGetErrorResult(request, (HttpStatusCode)value.StatusCode, value.Error);
    //                 value = new Response(value.Id, value.StreamId, custom, value.Error, value.StatusCode);
    //             }
    //
    //             responses.Add(value);
    //         }
    //     }
    //
    //     return responses.ToArray();
    // }

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

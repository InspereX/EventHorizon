﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Models;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.EventStore.Interfaces;
using Insperex.EventHorizon.EventStore.Interfaces.Stores;
using Insperex.EventHorizon.EventStreaming;
using Insperex.EventHorizon.EventStreaming.Publishers;
using Microsoft.Extensions.Logging;

namespace Insperex.EventHorizon.EventSourcing.Aggregates;

public class Aggregator<TParent, T>
    where TParent : class, IStateWrapper<T>, new()
    where T : class, IState
{
    private readonly ICrudStore<TParent> _crudStore;
    private readonly ILogger<Aggregator<TParent, T>> _logger;
    private readonly StreamingClient _streamingClient;
    private readonly Dictionary<string, object> _publisherDict = new();

    public Aggregator(
        ICrudStore<TParent> crudStore,
        StreamingClient streamingClient,
        ILogger<Aggregator<TParent, T>> logger)
    {
        _crudStore = crudStore;
        _streamingClient = streamingClient;
        _logger = logger;
    }

    #region Save

    public async Task SaveAllAsync(Dictionary<string, Aggregate<T>> aggregateDict)
    {
        // Save Snapshots, Events, and Publish Responses for Successful Saves
        await SaveSnapshotsAsync(aggregateDict);
        await PublishEventsAsync(aggregateDict);

        // Log Groups of failed snapshots
        var aggStatusLookup = aggregateDict.Values.ToLookup(x => x.Error);
        foreach (var group in aggStatusLookup)
        {
            if (group.Key == null) continue;
            var first = group.First();
            _logger.LogError("{State} {Count} had {Status} => {Error}",
                typeof(T).Name, group.Count(), first.StatusCode, first.Error);
        }
    }

    private async Task SaveSnapshotsAsync(Dictionary<string, Aggregate<T>> aggregateDict)
    {
        try
        {
            // Save Snapshots and then track failures
            var sw = Stopwatch.StartNew();
            var parents = aggregateDict.Values
                .Where(x => x.Error == null)
                .Where(x => x.IsDirty)
                .Select(x => new TParent
                {
                    Id = x.Id,
                    SequenceId = x.SequenceId,
                    State = x.State,
                    CreatedDate = x.CreatedDate,
                    UpdatedDate = x.UpdatedDate
                })
                .ToArray();

            if (parents.Any() != true)
                return;

            var results = await _crudStore.UpsertAsync(parents, CancellationToken.None);
            foreach (var id in results.FailedIds)
                aggregateDict[id].SetStatus(HttpStatusCode.InternalServerError, "Snapshot Failed to Save");
            foreach (var id in results.PassedIds)
            {
                aggregateDict[id].SetStatus(aggregateDict[id].SequenceId == 1?
                    HttpStatusCode.Created : HttpStatusCode.OK);
                aggregateDict[id].SequenceId++;
            }
            _logger.LogInformation("Saved {Count} {Type} Aggregate(s) in {Duration}",
                aggregateDict.Count, typeof(T).Name, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            foreach (var aggregate in aggregateDict.Values)
                aggregate.SetStatus(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    private async Task PublishEventsAsync(Dictionary<string, Aggregate<T>> aggregateDict)
    {
        var events = aggregateDict.Values
            .Where(x => x.Error == null)
            .SelectMany(x => x.Events)
            .ToArray();

        if (events.Any() != true)
            return;

        try
        {
            var publisher = GetPublisher<Event>(null);
            await publisher.PublishAsync(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish events");
            var streamIds = events.Select(x => x.StreamId).Distinct().ToArray();
            foreach (var streamId in streamIds)
                aggregateDict[streamId].SetStatus(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    internal async Task PublishResponseAsync(Response[] responses)
    {
        try
        {
            var responsesLookup = responses.ToLookup(x => x.SenderId);
            foreach (var group in responsesLookup)
            {
                var publisher = GetPublisher<Response>(group.Key);
                await publisher.PublishAsync(group.ToArray());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish results");
        }
    }

    private Publisher<TM> GetPublisher<TM>(string path) where TM : class, ITopicMessage, new()
    {
        var key = $"{typeof(TM).Name}-{path}";
        if (!_publisherDict.ContainsKey(key))
            _publisherDict[key] = _streamingClient.CreatePublisher<TM>().AddStream<T>(path).Build();

        return _publisherDict[key] as Publisher<TM>;
    }

    private static void ResetAll(Dictionary<string, Aggregate<T>> aggregateDict)
    {
        foreach (var aggregate in aggregateDict.Values)
        {
            aggregate.Events.Clear();
            aggregate.Responses.Clear();
            aggregate.SetStatus(HttpStatusCode.OK);
        }
    }

    #endregion

    #region load

    public async Task<Aggregate<T>> GetAggregateFromStateAsync(string streamId, CancellationToken ct)
    {
        var result = await GetAggregatesFromStatesAsync(new[] { streamId }, ct);
        return result.Values.FirstOrDefault();
    }

    public async Task<Dictionary<string, Aggregate<T>>> GetAggregatesFromStatesAsync(string[] streamIds, CancellationToken ct)
    {
        try
        {
            // Load Snapshots
            var sw = Stopwatch.StartNew();
            streamIds = streamIds.Distinct().ToArray();
            var snapshots = await _crudStore.GetAllAsync(streamIds, ct);
            var parentDict = snapshots.ToDictionary(x => x.Id);

            // Build Aggregate Dict
            var aggregateDict = streamIds
                .Select(x => parentDict.TryGetValue(x, out var value)? new Aggregate<T>(value) : new Aggregate<T>(x))
                .ToDictionary(x => x.Id);

            _logger.LogInformation("Loaded {Count} {Type} Aggregate(s) in {Duration}",
                aggregateDict.Count, typeof(T).Name, sw.ElapsedMilliseconds);

            return aggregateDict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load snapshots");
            return streamIds
                .Select(x =>
                {
                    var agg = new Aggregate<T>(x);
                    agg.SetStatus(HttpStatusCode.InternalServerError, ex.Message);
                    return agg;
                })
                .ToDictionary(x => x.Id);
        }
    }

    public async Task<Aggregate<T>> GetAggregateFromEventsAsync(string streamId, DateTime? endDateTime = null)
    {
        var results = await GetAggregatesFromEventsAsync(new[] { streamId }, endDateTime);
        return results[streamId];
    }

    public async Task<Dictionary<string, Aggregate<T>>> GetAggregatesFromEventsAsync(string[] streamIds,
        DateTime? endDateTime = null)
    {
        var events = await GetEventsAsync(streamIds, endDateTime);
        var eventLookup = events.ToLookup(x => x.Data.StreamId);
        return streamIds
            .Select(x =>
            {
                var e = eventLookup[x].ToArray();
                return e.Any() ? new Aggregate<T>(e.ToArray()) : new Aggregate<T>(x);
            })
            .ToDictionary(x => x.Id);
    }

    public Task<MessageContext<Event>[]> GetEventsAsync(string[] streamIds, DateTime? endDateTime = null)
    {
        var reader = _streamingClient.CreateReader<Event>().AddStream<T>().Keys(streamIds).EndDateTime(endDateTime).Build();
        return reader.GetNextAsync(10000);
    }

    #endregion
}

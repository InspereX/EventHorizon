﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Insperex.EventHorizon.EventStore.ElasticSearchLegacy.Attributes;
using Insperex.EventHorizon.EventStore.Interfaces;
using Insperex.EventHorizon.EventStore.Interfaces.Stores;
using Insperex.EventHorizon.EventStore.Models;
using Microsoft.Extensions.Logging;
using Nest;
using IResponse = Nest.IResponse;

namespace Insperex.EventHorizon.EventStore.ElasticSearchLegacy;

public class ElasticCrudStore<TE> : ICrudStore<TE>
    where TE : class, ICrudEntity
{
    private readonly ElasticIndexAttribute _elasticAttr;
    private readonly IElasticClient _client;
    private readonly ILogger<ElasticCrudStore<TE>> _logger;
    private readonly string _dbName;

    public ElasticCrudStore(ElasticIndexAttribute elasticAttr, IElasticClient client, string bucketId, ILogger<ElasticCrudStore<TE>> logger)
    {
        _elasticAttr = elasticAttr;
        _client = client;
        _logger = logger;
        _dbName = bucketId;
    }

    public async Task SetupAsync(CancellationToken ct)
    {
        var getReq = await _client.Indices.GetAsync(new GetIndexRequest(_dbName), ct);
        if (getReq.IsValid) return;

        var createReq = await _client.Indices.CreateAsync(_dbName, cfg =>
        {
            cfg.Map<TE>(map => map.AutoMap())
                .Settings(x =>
                {
                    if (_elasticAttr?.Shards > 0) x.NumberOfShards(_elasticAttr?.Shards);
                    if (_elasticAttr?.Replicas > 0) x.NumberOfReplicas(_elasticAttr?.Replicas);
                    if (_elasticAttr?.RefreshIntervalMs > 0) x.RefreshInterval(_elasticAttr?.RefreshIntervalMs);
                    if (_elasticAttr?.MaxResultWindow > 0) x.Setting("max_result_window", _elasticAttr?.MaxResultWindow);
                    return x;
                });
            return cfg;
        }, ct);

        ThrowErrors(createReq);
    }

    public async Task<TE[]> GetAllAsync(string[] ids, CancellationToken ct)
    {
        if (ids?.Any() != true)
            return Array.Empty<TE>();

        ids = ids.Distinct().ToArray();

        var res = await _client.MultiGetAsync(m => m
            .Index(_dbName)
            .GetMany<TE>(ids, (g, id) => g.Index(_dbName))
            .Refresh(_elasticAttr?.Refresh == Refresh.True)
        , ct);

        ThrowErrors(res);

        return res.Hits.Select(x => x.Source as TE).Where(x => x != null).ToArray();
    }

    public async Task<DateTime> GetLastUpdatedDateAsync(CancellationToken ct)
    {
        var res = await _client.SearchAsync<Snapshot<TE>>(x =>
                x.Index(_dbName)
                    .Size(1)
                    .Source(s =>
                        s.Includes(i =>
                            i.Fields(f => f.UpdatedDate)
                        )
                    )
                    .Query(q =>
                        q.Bool(b =>
                            b.Filter(f => f.MatchAll())
                        )
                    )
                    .Sort(s => s.Descending(f => f.UpdatedDate))
            , ct);

        ThrowErrors(res);

        return res.Documents.FirstOrDefault()?.UpdatedDate ?? DateTime.MinValue;
    }

    public async Task<DbResult> InsertAsync(TE[] objs, CancellationToken ct)
    {
        var res = await _client.BulkAsync(
            b => b.Index(_dbName)
                .CreateMany(objs)
                .Refresh(_elasticAttr?.Refresh), ct);

        var result = new DbResult { PassedIds = objs.Select(x => x.Id).ToArray() };
        if (res.Errors)
        {
            var failedIds = res.ItemsWithErrors.Select(x => x.Id).ToArray();
            result.FailedIds = objs.Where(x => failedIds.Contains(x.Id)).Select(x => x.Id).ToArray();
            result.PassedIds = objs.Where(x => !failedIds.Contains(x.Id)).Select(x => x.Id).ToArray();
        }

        return result;
    }

    public async Task<DbResult> UpsertAsync(TE[] objs, CancellationToken ct)
    {
        var res = await _client.BulkAsync(
            b => b.Index(_dbName)
                .IndexMany(objs)
                .Refresh(_elasticAttr?.Refresh), ct);

        var result = new DbResult { PassedIds = objs.Select(x => x.Id).ToArray(), FailedIds = Array.Empty<string>() };
        if (res.Errors)
        {
            var failedIds = res.ItemsWithErrors.Select(x => x.Id).ToArray();
            result.FailedIds = objs.Where(x => failedIds.Contains(x.Id)).Select(x => x.Id).ToArray();
            result.PassedIds = objs.Where(x => !failedIds.Contains(x.Id)).Select(x => x.Id).ToArray();
        }

        return result;
    }

    public async Task DeleteAsync(string[] ids, CancellationToken ct)
    {
        var objs = ids.Select(x => new { Id = x }).ToArray();
        var res = await _client.BulkAsync(
            b => b.Index(_dbName)
                .DeleteMany(objs)
                .Refresh(_elasticAttr?.Refresh), ct);

        ThrowErrors(res);
    }

    public Task DropDatabaseAsync(CancellationToken ct)
    {
        return _client.Indices.DeleteAsync(_dbName, ct: ct);
    }

    private void ThrowErrors(MultiGetResponse res)
    {
        if (res.IsValid) return;

        var failedHits = res.Hits.Where(x => x.Error != null).ToArray();
        if (failedHits.Any())
        {
            var first = failedHits.First();
            if (first.Error.Type == "index_not_found_exception")
                return;
            else
                throw new Exception(first.Error.Type);
        }

        ThrowErrors(res as IResponse);
    }

    private void ThrowErrors(IResponse res)
    {
        if (res.IsValid) return;

        // Low Level Errors
        if (res.OriginalException != null)
        {
            var max = Math.Min(2000, res.DebugInformation.Length);
            _logger.LogError(res.OriginalException, res.DebugInformation.Substring(0, max));
            throw res.OriginalException;
        }

        // Low Level Errors
        if (res.ServerError != null && res.ServerError.Error.Type != "index_already_exists_exception")
        {
            var ex = new ElasticsearchClientException(res.ServerError.ToString());
            _logger.LogError(ex, res.ServerError.ToString());
            throw ex;
        }

        throw new Exception("Unknown Elastic Exception");
    }
}

﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.EventStore.Interfaces;
using Insperex.EventHorizon.EventStore.Interfaces.Stores;
using Insperex.EventHorizon.EventStore.Models;
using Insperex.EventHorizon.EventStore.MongoDb.Attributes;
using Insperex.EventHorizon.EventStore.MongoDb.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Insperex.EventHorizon.EventStore.MongoDb.Stores;

public abstract class AbstractMongoCrudStore<T> : ICrudStore<T>
    where T : ICrudEntity
{
    private const string Id = "_id";
    private const string Name = "name";
    private const string Document = "Document";
    private const string CreatedDate1 = "CreatedDate_1";
    private const string Tilda1 = "`1";
    private const string ErrorPrefix = "_id: \"";
    private const string ErrorPostfix = "\" }";
    private readonly string _database;
    private readonly IMongoClient _client;
    private readonly MongoCollectionAttribute _mongoAttr;
    private readonly IMongoCollection<T> _collection;
    private readonly IMongoDatabase _db;

    public AbstractMongoCrudStore(IMongoClient client, MongoCollectionAttribute mongoAttr, string database)
    {
        _client = client;
        _mongoAttr = mongoAttr;
        _database = database;
        var type = typeof(T);
        _db = client.GetDatabase(database);
        var typeName = type.Name.Replace(Tilda1, string.Empty);
        _collection = _db.GetCollection<T>(typeName);
    }

    public virtual async Task MigrateAsync(CancellationToken ct)
    {
        if (_mongoAttr?.ReadConcernLevel != null) _collection.WithReadConcern(new ReadConcern(_mongoAttr.ReadConcernLevel));
        if (_mongoAttr?.ReadPreferenceMode != null) _collection.WithReadPreference(new ReadPreference(_mongoAttr.ReadPreferenceMode));
        if (_mongoAttr?.WriteConcernLevel != null)
        {
            switch (_mongoAttr.WriteConcernLevel)
            {
                case WriteConcernLevel.Acknowledged: _collection.WithWriteConcern(WriteConcern.Acknowledged); break;
                case WriteConcernLevel.Unacknowledged: _collection.WithWriteConcern(WriteConcern.Unacknowledged); break;
                case WriteConcernLevel.W1: _collection.WithWriteConcern(WriteConcern.W1); break;
                case WriteConcernLevel.W2: _collection.WithWriteConcern(WriteConcern.W2); break;
                case WriteConcernLevel.W3: _collection.WithWriteConcern(WriteConcern.W3); break;
                case WriteConcernLevel.Majority: _collection.WithWriteConcern(WriteConcern.WMajority); break;
            }
        }

        if (_mongoAttr?.TimeToLiveMs != null)
            await AddIndex(CreatedDate1, Builders<T>.IndexKeys.Ascending(x => x.CreatedDate), TimeSpan.FromMilliseconds(_mongoAttr.TimeToLiveMs));
    }

    public async Task<T[]> GetAllAsync(string[] ids, CancellationToken ct)
    {
        var objs = await _collection
            .Find(x => ids.Contains(x.Id))
            .ToListAsync(ct);

        return objs.ToArray();
    }

    public async Task<DbResult> InsertAllAsync(T[] objs, CancellationToken ct)
    {
        var result = new DbResult();
        try
        {
            await _collection.InsertManyAsync(objs, new InsertManyOptions(), ct);
            result.PassedIds = objs.Select(x => x.Id).ToArray();
            result.FailedIds = Array.Empty<string>();
        }
        catch (MongoBulkWriteException<T> ex)
        {
            var dupeKeyError = ex.WriteErrors.FirstOrDefault(x => x.Code == 11000);
            if (dupeKeyError == null) throw;

            // Note: First Id is not in UnprocessedRequests
            var firstId = dupeKeyError.Message.Split(ErrorPrefix)[1].Replace(ErrorPostfix, string.Empty);

            // Get FailedIds w/ FirstId
            var failedIds = ex.UnprocessedRequests
                .Select(x => x.ToBsonDocument()[Document][Id].AsString)
                .Concat(new[] { firstId })
                .Distinct()
                .ToArray();

            // Store passed and failed
            result.FailedIds = objs.Where(x => failedIds.Contains(x.Id)).Select(x => x.Id).Distinct().ToArray();
            result.PassedIds = objs.Where(x => !failedIds.Contains(x.Id)).Select(x => x.Id).Distinct().ToArray();
        }

        return result;
    }

    public async Task<DbResult> UpsertAllAsync(T[] objs, CancellationToken ct)
    {
        var result = new DbResult();
        try
        {
            var ops = new List<WriteModel<T>>();
            foreach (var obj in objs)
            {
                var filter = Builders<T>.Filter.Eq(Id, obj.Id);
                ops.Add(new ReplaceOneModel<T>(filter, obj) { IsUpsert = true });
            }

            await _collection.BulkWriteAsync(ops, cancellationToken: ct);
            result.PassedIds = objs.Select(x => x.Id).ToArray();
            result.FailedIds = Array.Empty<string>();
        }
        catch (MongoBulkWriteException<T> ex)
        {
            var dupeKeyError = ex.WriteErrors.FirstOrDefault(x => x.Code == 11000);
            if (dupeKeyError == null) throw;

            // Note: First Id is not in UnprocessedRequests
            var firstId = dupeKeyError.Message.Split(ErrorPrefix)[1].Replace(ErrorPostfix, string.Empty);

            // Get FailedIds w/ FirstId
            var failedIds = ex.UnprocessedRequests
                .Select(x => x.ToBsonDocument()[Document][Id].AsString)
                .Concat(new[] { firstId })
                .Distinct()
                .ToArray();

            // Store passed and failed
            result.FailedIds = objs.Where(x => failedIds.Contains(x.Id)).Select(x => x.Id).Distinct().ToArray();
            result.PassedIds = objs.Where(x => !failedIds.Contains(x.Id)).Select(x => x.Id).Distinct().ToArray();
        }

        return result;
    }

    public async Task DeleteAllAsync(string[] ids, CancellationToken ct)
    {
        await _collection.DeleteManyAsync(x => ids.Contains(x.Id), ct);
    }

    public Task DropDatabaseAsync(CancellationToken ct)
    {
        return _client.DropDatabaseAsync(_database, ct);
    }

    protected async Task AddIndex(string name, IndexKeysDefinition<T> definition, TimeSpan? timeSpan = null)
    {
        var opts = new CreateIndexOptions { Background = true, ExpireAfter = timeSpan };
        var names = (await _collection.Indexes.ListAsync()).ToList().Select(x => x[Name.ToLower(CultureInfo.InvariantCulture)]).ToArray();
        if (!names.Contains(name))
            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<T>(definition,opts));
    }

    protected async Task AddShard(string key)
    {
        var shardDbResult = _client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument
        {
            { "enableSharding",$"{_database}" }
        });
        var partition = new BsonDocument {
            {"shardCollection", $"{_database}.{_collection.CollectionNamespace.CollectionName}"},
            {"key", new BsonDocument {{key, "hashed"}}}
        };
        await _db.RunCommandAsync(new BsonDocumentCommand<BsonDocument>(partition));
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Formatters;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Util;
using Insperex.EventHorizon.EventStore.Interfaces.Stores;
using Insperex.EventHorizon.EventStore.Models;
using Insperex.EventHorizon.EventStore.MongoDb.Attributes;
using MongoDB.Driver;

namespace Insperex.EventHorizon.EventStore.MongoDb.Stores
{
    public class MongoSnapshotStore<T> : AbstractMongoCrudStore<Snapshot<T>>, ISnapshotStore<T> where T : IState
    {
        private static readonly Type Type = typeof(T);
        private const string StreamId1 = "StreamId_1";
        public MongoSnapshotStore(Formatter formatter, AttributeUtil attributeUtil, MongoClientResolver clientResolver)
            : base(clientResolver.GetClient(),
                attributeUtil.GetOne<MongoCollectionAttribute>(Type),
                formatter.GetDatabase<Snapshot<T>>(Type)) { }

        public override async Task MigrateAsync(CancellationToken ct)
        {
            await AddIndex(StreamId1, Builders<Snapshot<T>>.IndexKeys.Ascending(x => x.Id));
            await base.MigrateAsync(ct);
        }
    }
}

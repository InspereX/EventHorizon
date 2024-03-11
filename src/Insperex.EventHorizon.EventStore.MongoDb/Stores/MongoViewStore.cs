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
    public class MongoViewStore<T> : AbstractMongoCrudStore<View<T>>, IViewStore<T> where T : IState
    {
        private static readonly Type Type = typeof(T);
        private const string StreamId1 = "UpdatedDate_1";
        public MongoViewStore(Formatter formatter, AttributeUtil attributeUtil, MongoClientResolver clientResolver)
            : base(clientResolver.GetClient(),
                attributeUtil.GetOne<MongoCollectionAttribute>(Type),
                formatter.GetDatabase<View<T>>(Type)) { }

        public override async Task MigrateAsync(CancellationToken ct)
        {
            await AddIndex(StreamId1, Builders<View<T>>.IndexKeys.Ascending(x => x.Id));
            await base.MigrateAsync(ct);
        }
    }
}

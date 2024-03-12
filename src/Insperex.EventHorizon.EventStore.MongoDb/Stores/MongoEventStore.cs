using System;
using System.Threading;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Formatters;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.Abstractions.Util;
using Insperex.EventHorizon.EventStore.Interfaces.Stores;
using Insperex.EventHorizon.EventStore.Models;
using Insperex.EventHorizon.EventStore.MongoDb.Attributes;
using MongoDB.Driver;

namespace Insperex.EventHorizon.EventStore.MongoDb.Stores
{
    public class MongoEventStore<T> : AbstractMongoCrudStore<Event>, IEventStore<T> where T : IState
    {
        private static readonly Type Type = typeof(T);
        private const string StreamId1 = "StreamId_1";
        private const string CreatedDate = "CreatedDate_1";
        public MongoEventStore(Formatter formatter, AttributeUtil attributeUtil, MongoClientResolver clientResolver)
            : base(clientResolver.GetClient(),
                attributeUtil.GetOne<MongoCollectionAttribute>(Type),
                formatter.GetDatabase<View<T>>(Type)) { }

        public override async Task MigrateAsync(CancellationToken ct)
        {
            await AddIndex(StreamId1, Builders<Event>.IndexKeys.Ascending(x => x.StreamId));
            await AddIndex(CreatedDate, Builders<Event>.IndexKeys.Ascending(x => x.CreatedDate));
            await base.MigrateAsync(ct);
        }
    }
}

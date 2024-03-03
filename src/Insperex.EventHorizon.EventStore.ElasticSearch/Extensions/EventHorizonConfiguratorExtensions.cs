using System;
using Elastic.Clients.Elasticsearch;
using Insperex.EventHorizon.Abstractions;
using Insperex.EventHorizon.EventStore.ElasticSearch.Models;
using Insperex.EventHorizon.EventStore.Locks;
using Microsoft.Extensions.DependencyInjection;

namespace Insperex.EventHorizon.EventStore.ElasticSearch.Extensions;

public static class EventHorizonConfiguratorExtensions
{
    public static EventHorizonConfigurator AddElasticClient(this EventHorizonConfigurator configurator, Action<ElasticConfig> onConfig)
    {
        configurator.Collection.Configure(onConfig);
        configurator.Collection.AddSingleton(typeof(LockFactory<>));
        configurator.AddClientResolver<ElasticClientResolver, ElasticsearchClient>();
        return configurator;
    }
}

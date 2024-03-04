﻿using System;
using Insperex.EventHorizon.Abstractions;
using Insperex.EventHorizon.EventStore.Locks;
using Insperex.EventHorizon.EventStore.MongoDb.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Insperex.EventHorizon.EventStore.MongoDb.Extensions;

public static class EventHorizonConfiguratorExtensions
{
    static EventHorizonConfiguratorExtensions()
    {
        // Allow all to serialize
        BsonSerializer.RegisterSerializer(new ObjectSerializer(_ => true));
    }

    public static EventHorizonConfigurator AddMongoDbClient(this EventHorizonConfigurator configurator, Action<MongoConfig> onConfig)
    {
        configurator.Collection.Configure(onConfig);
        configurator.Collection.AddSingleton(typeof(LockFactory<>));
        configurator.AddClientResolver<MongoClientResolver, MongoClient>();
        return configurator;
    }
}

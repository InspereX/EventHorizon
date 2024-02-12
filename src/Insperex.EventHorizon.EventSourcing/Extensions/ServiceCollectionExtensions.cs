using System;
using Insperex.EventHorizon.Abstractions;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.EventSourcing.Aggregates;
using Insperex.EventHorizon.EventSourcing.AggregateWorkflow;
using Insperex.EventHorizon.EventSourcing.AggregateWorkflow.Services;
using Insperex.EventHorizon.EventSourcing.AggregateWorkflow.Workflows;
using Insperex.EventHorizon.EventSourcing.Senders;
using Insperex.EventHorizon.EventSourcing.Util;
using Insperex.EventHorizon.EventStore.Models;
using Insperex.EventHorizon.EventStreaming;
using Insperex.EventHorizon.EventStreaming.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Insperex.EventHorizon.EventSourcing.Extensions;

public static class ServiceCollectionExtensions
{
    public static EventHorizonConfigurator AddEventSourcing(this EventHorizonConfigurator configurator)
    {
        configurator.Collection.TryAddSingleton(typeof(EventSourcingClient));
        configurator.Collection.TryAddSingleton(typeof(AggregatorBuilder<,>));
        configurator.Collection.TryAddSingleton(typeof(AggregateWorkflowFactory<>));
        configurator.Collection.TryAddSingleton(typeof(AggregateWorkflowHostedService));
        configurator.Collection.TryAddSingleton<SenderBuilder>();
        configurator.Collection.TryAddSingleton<SenderSubscriptionTracker>();
        configurator.Collection.TryAddSingleton<ValidationUtil>();

        return configurator;
    }

    public static EventHorizonConfigurator AddWorkflow<T>(this EventHorizonConfigurator configurator,
        Func<AggregateWorkflowFactory<T>, IAggregateWorkflow> onBuild = null)
        where T : class, IState
    {
        configurator.AddEventSourcing();
        configurator.Collection.AddScoped(x =>
        {
            var builder = x.GetRequiredService<AggregateWorkflowFactory<T>>();
            return onBuild?.Invoke(builder);
        });

        return configurator;
    }
}

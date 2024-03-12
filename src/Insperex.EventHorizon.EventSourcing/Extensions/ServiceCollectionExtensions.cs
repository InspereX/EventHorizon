using System;
using Insperex.EventHorizon.Abstractions;
using Insperex.EventHorizon.Abstractions.Extensions;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.EventSourcing.Aggregates;
using Insperex.EventHorizon.EventSourcing.AggregateWorkflows;
using Insperex.EventHorizon.EventSourcing.AggregateWorkflows.Workflows;
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
        configurator.Collection.TryAddSingleton(typeof(EventSourcingClient<>));
        configurator.Collection.TryAddSingleton(typeof(AggregatorBuilder<,>));
        configurator.Collection.TryAddSingleton(typeof(SenderBuilder<>));
        configurator.Collection.TryAddSingleton(typeof(WorkflowService<,,>));
        configurator.Collection.TryAddSingleton<SenderSubscriptionTracker>();
        configurator.Collection.TryAddSingleton<ValidationUtil>();

        return configurator;
    }

    public static EventHorizonConfigurator HandleRequests<TState>(this EventHorizonConfigurator configurator,
        Action<WorkflowConfigurator<TState>> onConfig = null)
        where TState : class, IState
    {
        configurator.AddEventSourcing();
        configurator.Collection.AddSingleton(x =>
        {
            var streamingClient = x.GetRequiredService<StreamingClient>();
            var configurator = new WorkflowConfigurator<TState>();
            onConfig?.Invoke(configurator);
            var workflowService = new WorkflowService<Snapshot<TState>, TState, Request>(x, configurator.WorkflowMiddleware);
            return new HandleAndApplyEvents<Snapshot<TState>, TState, Request>(streamingClient, workflowService, configurator);
        });

        return configurator;
    }

    public static EventHorizonConfigurator HandleCommands<TState>(this EventHorizonConfigurator configurator,
        Action<WorkflowConfigurator<TState>> onConfig = null)
        where TState : class, IState
    {
        configurator.AddEventSourcing();
        configurator.Collection.AddSingleton(x =>
        {
            var streamingClient = x.GetRequiredService<StreamingClient>();
            var configurator = new WorkflowConfigurator<TState>();
            onConfig?.Invoke(configurator);
            var workflowService = new WorkflowService<Snapshot<TState>, TState, Command>(x, configurator.WorkflowMiddleware);
            return new HandleAndApplyEvents<Snapshot<TState>, TState, Command>(streamingClient, workflowService, configurator);
        });

        return configurator;
    }

    public static EventHorizonConfigurator HandleEvents<TState>(this EventHorizonConfigurator configurator,
        Action<WorkflowConfigurator<TState>> onConfig = null)
        where TState : class, IState
    {
        configurator.AddEventSourcing();
        configurator.Collection.AddSingleton(x =>
        {
            var streamingClient = x.GetRequiredService<StreamingClient>();
            var configurator = new WorkflowConfigurator<TState>();
            onConfig?.Invoke(configurator);
            var workflowService = new WorkflowService<Snapshot<TState>, TState, Event>(x, configurator.WorkflowMiddleware);
            return new HandleAndApplyEvents<Snapshot<TState>, TState, Event>(streamingClient, workflowService, configurator);
        });

        return configurator;
    }

    public static EventHorizonConfigurator ApplyEvents<TState>(this EventHorizonConfigurator configurator)
        where TState : class, IState
    {
        configurator.AddEventSourcing();
        configurator.Collection.AddSingleton(x =>
        {
            var streamingClient = x.GetRequiredService<StreamingClient>();
            var workflowService = new WorkflowService<View<TState>, TState, Event>(x, null);
            return new ApplyEventsWorkflow<View<TState>, TState>(streamingClient, workflowService, null);
        });

        return configurator;
    }
}

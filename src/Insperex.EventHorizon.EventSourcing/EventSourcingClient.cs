using System;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.EventSourcing.Aggregates;
using Insperex.EventHorizon.EventSourcing.AggregateWorkflow;
using Insperex.EventHorizon.EventSourcing.Senders;
using Insperex.EventHorizon.EventStore.Interfaces.Factory;
using Insperex.EventHorizon.EventStore.Interfaces.Stores;
using Insperex.EventHorizon.EventStore.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Insperex.EventHorizon.EventSourcing;

public class EventSourcingClient
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SenderBuilder _senderBuilder;

    public EventSourcingClient(
        SenderBuilder senderBuilder,
        IServiceProvider serviceProvider)
    {
        _senderBuilder = senderBuilder;
        _serviceProvider = serviceProvider;
    }

    public SenderBuilder CreateSender() => _senderBuilder;
    public AggregateWorkflowFactory<TState> AggregateWorkflow<TState>()
        where TState : class, IState, new()
        => _serviceProvider.GetRequiredService<AggregateWorkflowFactory<TState>>();
    public AggregatorBuilder<Snapshot<TState>, TState> Aggregator<TState>()
        where TState : class, IState, new()
        => _serviceProvider.GetRequiredService<AggregatorBuilder<Snapshot<TState>, TState>>();
    public ICrudStore<Snapshot<TState>> GetSnapshotStore<TState>()
        where TState : class, IState, new()
        => _serviceProvider.GetRequiredService<ISnapshotStoreFactory<TState>>().GetSnapshotStore();
    public ICrudStore<View<TState>> GetViewStore<TState>()
        where TState : class, IState, new() =>
        _serviceProvider.GetRequiredService<IViewStoreFactory<TState>>().GetViewStore();
}

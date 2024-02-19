using System;
using System.Threading;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.EventSourcing.Interfaces;
using Insperex.EventHorizon.EventSourcing.Util;
using Insperex.EventHorizon.EventStore.Interfaces;
using Insperex.EventHorizon.EventStore.Interfaces.Factory;
using Insperex.EventHorizon.EventStore.Interfaces.Stores;
using Insperex.EventHorizon.EventStore.Locks;
using Insperex.EventHorizon.EventStore.Models;
using Insperex.EventHorizon.EventStreaming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Insperex.EventHorizon.EventSourcing.Aggregates;

public class AggregatorBuilder<TWrapper, T>
    where TWrapper : class, IStateWrapper<T>, new()
    where T : class, IState
{
    private readonly ICrudStore<TWrapper> _crudStore;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ValidationUtil _validationUtil;
    private readonly StreamingClient _streamingClient;
    private readonly LockFactory<T> _lockFactory;
    private readonly ILogger<AggregatorBuilder<TWrapper, T>> _logger;

    public AggregatorBuilder(IServiceProvider serviceProvider)
    {
        _crudStore = typeof(TWrapper).Name == typeof(Snapshot<>).Name?
            (ICrudStore<TWrapper>)serviceProvider.GetRequiredService<ISnapshotStoreFactory<T>>().GetSnapshotStore() :
            (ICrudStore<TWrapper>)serviceProvider.GetRequiredService<IViewStoreFactory<T>>().GetViewStore();
        _lockFactory = serviceProvider.GetRequiredService<LockFactory<T>>();
        _validationUtil = serviceProvider.GetRequiredService<ValidationUtil>();
        _streamingClient = serviceProvider.GetRequiredService<StreamingClient>();
        _loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = _loggerFactory.CreateLogger<AggregatorBuilder<TWrapper, T>>();
    }

    public Aggregator<TWrapper, T> Build()
    {
        // Create Store
        var @lock = _lockFactory.CreateLock($"Migrate-{typeof(T).Name}", Environment.MachineName).WaitForLockAsync().GetAwaiter().GetResult();
        _logger.LogInformation("{Store} Store - Start {TParent} {T} Migration {Host}", _crudStore.GetType().Name, typeof(TWrapper).Name, typeof(T).Name, Environment.MachineName);
        _crudStore.SetupAsync(CancellationToken.None).GetAwaiter().GetResult();
        _logger.LogInformation("{Store} Store - Finished {TParent} {T} Migration {Host}", _crudStore.GetType().Name, typeof(TWrapper).Name, typeof(T).Name, Environment.MachineName);
        @lock.DisposeAsync().GetAwaiter().GetResult();

        // Validate Handlers if Enabled
        _validationUtil.Validate<TWrapper, T>();

        var logger = _loggerFactory.CreateLogger<Aggregator<TWrapper, T>>();
        return new Aggregator<TWrapper, T>(_crudStore, _streamingClient, logger);
    }
}

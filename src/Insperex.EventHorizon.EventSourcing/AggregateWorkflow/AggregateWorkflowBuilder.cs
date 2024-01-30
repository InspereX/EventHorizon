using System;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.EventSourcing.Aggregates;
using Insperex.EventHorizon.EventSourcing.Interfaces;
using Insperex.EventHorizon.EventStore.Interfaces;
using Insperex.EventHorizon.EventStore.Interfaces.Stores;
using Insperex.EventHorizon.EventStreaming.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Insperex.EventHorizon.EventSourcing.AggregateWorkflow
{
    public class AggregateWorkflowBuilder<TWrapper, T>
        where TWrapper : class, IStateWrapper<T>, new()
        where T : class, IState
    {
        private readonly IServiceProvider _serviceProvider;
        private IAggregateMiddleware<T> _middleware;
        private int _batchSize;

        public AggregateWorkflowBuilder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public AggregateWorkflowBuilder<TWrapper, T> BatchSize(int batchSize)
        {
            _batchSize = batchSize;
            return this;
        }

        public AggregateWorkflowBuilder<TWrapper, T> UseMiddleware<TMiddle>() where TMiddle : IAggregateMiddleware<T>
        {
            using var scope = _serviceProvider.CreateScope();
            _middleware = scope.ServiceProvider.GetRequiredService<TMiddle>();
            return this;
        }

        public AggregateWorkflow<TWrapper, T> Build()
        {
            var config = new AggregateWorkflowConfig<TWrapper, T>
            {
                BatchSize = _batchSize,
                Middleware = _middleware,
            };
            return new AggregateWorkflow<TWrapper, T>(_serviceProvider, config);
        }
    }
}

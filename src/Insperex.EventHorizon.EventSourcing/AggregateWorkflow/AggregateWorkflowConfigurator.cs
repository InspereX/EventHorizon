using System;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.EventSourcing.Interfaces;
using Insperex.EventHorizon.EventStreaming.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Insperex.EventHorizon.EventSourcing.AggregateWorkflow
{
    public class AggregateWorkflowConfigurator<TState, TMessage>
        where TState : class, IState
        where TMessage : class, ITopicMessage, new()
    {
        private readonly IServiceProvider _provider;
        internal IAggregateMiddleware<TState> Middleware;
        internal Action<SubscriptionBuilder<TMessage>> OnSubConfig;

        public AggregateWorkflowConfigurator(IServiceProvider provider)
        {
            _provider = provider;
        }

        public AggregateWorkflowConfigurator<TState, TMessage> UseMiddleware<TMiddleware>()
            where TMiddleware : IAggregateMiddleware<TState>
        {
            Middleware = _provider.GetRequiredService<TMiddleware>();
            return this;
        }

        public AggregateWorkflowConfigurator<TState, TMessage> Subscription(Action<SubscriptionBuilder<TMessage>> onSubConfig)
        {
            OnSubConfig = onSubConfig;
            return this;
        }
    }
}

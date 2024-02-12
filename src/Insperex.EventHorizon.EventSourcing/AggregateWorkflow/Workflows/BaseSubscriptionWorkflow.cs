using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.EventSourcing.Aggregates;
using Insperex.EventHorizon.EventStreaming;
using Insperex.EventHorizon.EventStreaming.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Insperex.EventHorizon.EventSourcing.AggregateWorkflow.Workflows
{
    public abstract class BaseSubscriptionWorkflow<TState, TMessage> : IAggregateWorkflow
        where TState : class, IState
        where TMessage : class, ITopicMessage, new()
    {
        protected Subscription<TMessage> Subscription;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Subscription.StartAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Subscription.StopAsync();
        }

        protected async Task OnBatch(SubscriptionContext<TMessage> batch)
        {
            // Consume Messages
            var messages = batch.Messages.Select(m => m.Data).ToArray();
            var aggregateDict = await HandleAsync(messages, batch.CancellationToken);

            // Get Failed StreamIds
            var failedIds = aggregateDict.Values
                .Where(x => x.Error != null)
                .Select(x => x.Id)
                .ToArray();

            // Get Failed Messages
            var failedMessages = batch.Messages
                .Where(x => failedIds.Contains(x.Data.StreamId))
                .ToArray();

            // Nack
            batch.Nack(failedMessages);
        }

        public abstract Task<Dictionary<string, Aggregate<TState>>> HandleAsync(TMessage[] messages, CancellationToken ct);
    }
}

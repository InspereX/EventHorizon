using System;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.EventSourcing.Aggregates;
using Insperex.EventHorizon.EventSourcing.AggregateWorkflow.Services;
using Insperex.EventHorizon.EventSourcing.AggregateWorkflow.Workflows;
using Insperex.EventHorizon.EventStore.Models;
using Insperex.EventHorizon.EventStreaming;
using Microsoft.Extensions.DependencyInjection;

namespace Insperex.EventHorizon.EventSourcing.AggregateWorkflow
{
    public class AggregateWorkflowFactory<TState> where TState : class, IState
    {
        private readonly StreamingClient _streamingClient;
        private readonly IServiceProvider _provider;

        public AggregateWorkflowFactory(StreamingClient streamingClient, IServiceProvider provider)
        {
            _streamingClient = streamingClient;
            _provider = provider;
        }

        public HandleAndApplyEvents<Snapshot<TState>, TState, Command> HandleCommandsApplyEvents(Action<AggregateWorkflowConfigurator<TState, Command>> onConfig = null) => HandleAndApply(onConfig);
        public HandleAndApplyEvents<Snapshot<TState>, TState, Request> HandleRequestsApplyEvents(Action<AggregateWorkflowConfigurator<TState, Request>> onConfig = null) => HandleAndApply(onConfig);
        public HandleAndApplyEvents<Snapshot<TState>, TState, Event> HandleEventsApplyEvents(Action<AggregateWorkflowConfigurator<TState, Event>> onConfig = null) => HandleAndApply(onConfig);

        public ApplyEvents<Snapshot<TState>, TState> ApplyEvents(Action<AggregateWorkflowConfigurator<TState, Event>> onConfig = null)
        {
            var config = new AggregateWorkflowConfigurator<TState, Event>(_provider);
            onConfig?.Invoke(config);

            var workflowService = new WorkflowService<Snapshot<TState>, TState, Event>(_provider, config);
            return new ApplyEvents<Snapshot<TState>, TState>(_streamingClient, workflowService, config);
        }

        public RebuildAllWorkflow<Snapshot<TState>, TState> RebuildAll(AggregateWorkflowConfigurator<TState, Event> onConfig = null)
        {
            var aggregator = _provider.GetRequiredService<AggregatorBuilder<Snapshot<TState>, TState>>().Build();
            return new(_streamingClient, aggregator, onConfig);
        }

        private HandleAndApplyEvents<Snapshot<TState>, TState, TMessage> HandleAndApply<TMessage>(Action<AggregateWorkflowConfigurator<TState, TMessage>> onConfig = null)
            where TMessage : class, ITopicMessage, new()
        {
            var config = new AggregateWorkflowConfigurator<TState, TMessage>(_provider);
            onConfig?.Invoke(config);

            var workflowService = new WorkflowService<Snapshot<TState>, TState, TMessage>(_provider, config);
            return new HandleAndApplyEvents<Snapshot<TState>, TState, TMessage>(_streamingClient, workflowService, config);
        }
    }
}

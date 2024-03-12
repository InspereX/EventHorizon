using System;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.EventSourcing.Aggregates;
using Insperex.EventHorizon.EventSourcing.AggregateWorkflows.Workflows;
using Insperex.EventHorizon.EventStore.Models;
using Insperex.EventHorizon.EventStreaming;
using Microsoft.Extensions.DependencyInjection;

namespace Insperex.EventHorizon.EventSourcing.AggregateWorkflows
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

        public HandleAndApplyEvents<Snapshot<TState>, TState, Command> HandleCommands(Action<WorkflowConfigurator<TState>> onConfig = null) => Handle<Command>(onConfig);
        public HandleAndApplyEvents<Snapshot<TState>, TState, Request> HandleRequests(Action<WorkflowConfigurator<TState>> onConfig = null) => Handle<Request>(onConfig);
        public HandleAndApplyEvents<Snapshot<TState>, TState, Event> HandleEvents(Action<WorkflowConfigurator<TState>> onConfig = null) => Handle<Event>(onConfig);

        public ApplyEventsWorkflow<Snapshot<TState>, TState> ApplyEvents(Action<WorkflowConfigurator<TState>> onConfig = null)
        {
            var config = new WorkflowConfigurator<TState>();
            onConfig?.Invoke(config);

            var workflowService = new WorkflowService<Snapshot<TState>, TState, Event>(_provider, config.WorkflowMiddleware);
            return new ApplyEventsWorkflow<Snapshot<TState>, TState>(_streamingClient, workflowService, config);
        }

        public RebuildAllWorkflow<Snapshot<TState>, TState> RebuildAll(WorkflowConfigurator<TState> onConfig = null)
        {
            var aggregator = _provider.GetRequiredService<AggregatorBuilder<Snapshot<TState>, TState>>().Build();
            return new(_streamingClient, aggregator, onConfig);
        }

        private HandleAndApplyEvents<Snapshot<TState>, TState, TMessage> Handle<TMessage>(Action<WorkflowConfigurator<TState>> onConfig = null)
            where TMessage : class, ITopicMessage, new()
        {
            var config = new WorkflowConfigurator<TState>();
            onConfig?.Invoke(config);

            var workflowService = new WorkflowService<Snapshot<TState>, TState, TMessage>(_provider, config.WorkflowMiddleware);
            return new HandleAndApplyEvents<Snapshot<TState>, TState, TMessage>(_streamingClient, workflowService, config);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Actions;
using Insperex.EventHorizon.Abstractions.Models;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.Abstractions.Util;
using Insperex.EventHorizon.EventSourcing.Util;
using Insperex.EventHorizon.EventStore.Interfaces;
using Insperex.EventHorizon.EventStore.Models;

namespace Insperex.EventHorizon.EventSourcing.Aggregates;

public class Aggregate<T>
    where T : class, IState
{
    internal readonly List<Event> Events = new();
    internal readonly List<Response> Responses = new();
    private readonly Type _type = typeof(T);
    private Dictionary<Type, object> AllStates { get; set; }
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
    public string Error { get; private set; }
    public bool IsDirty { get; private set; }
    public string Id { get; set; }
    public long SequenceId { get; set; }
    public T State { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }

    public Aggregate(string streamId)
    {
        Id = streamId;
        CreatedDate = UpdatedDate = DateTime.UtcNow;
        Setup();
    }

    public Aggregate(IStateParent<T> model)
    {
        Id = model.Id;
        SequenceId = model.SequenceId;
        State = model.State;
        CreatedDate = model.CreatedDate;
        UpdatedDate = model.UpdatedDate;
        Setup();
    }

    public Aggregate(string streamId, IEvent[] events)
    {
        // Create
        Setup();
        Id = streamId;
        CreatedDate = DateTime.UtcNow;

        // Defensive
        if (!events.Any()) return;

        // Apply Events
        foreach (var @event in events)
            Apply(@event);
    }

    public void Handle(ICommand payload)
    {
        foreach (var state in AllStates)
        {
            var context = new AggregateContext(Exists());
            var method = AggregateAssemblyUtil.StateToCommandHandlersDict.GetValueOrDefault(state.Key)?.GetValueOrDefault(payload.GetType());
            method?.Invoke(state.Value, parameters: new object[] { payload, context } );
            foreach (var item in context.Events)
                Apply(item);
        }
    }

    public void Handle(IRequest payload, string requestId, string senderId)
    {
        foreach (var state in AllStates)
        {
            var context = new AggregateContext(Exists());
            var method = AggregateAssemblyUtil.StateToRequestHandlersDict.GetValueOrDefault(state.Key)?.GetValueOrDefault(payload.GetType());
            var result = method?.Invoke(state.Value, parameters: new object[] { payload, context } );
            Responses.Add(new Response(requestId, senderId, Id, result, Error, (int)StatusCode));
            foreach(var item in context.Events)
                Apply(item);
        }
    }

    public void Apply(IEvent<T> @event, long? sequenceId = null)
    {
        Apply(@event, sequenceId);
    }

    public void Apply(IEvent payload, long? sequenceId = null)
    {
        foreach (var state in AllStates)
        {
            var method = AggregateAssemblyUtil.StateToEventHandlersDict.GetValueOrDefault(state.Key)?.GetValueOrDefault(payload.GetType());
            method?.Invoke(state.Value,
                parameters: sequenceId == null ? new object[] { payload } : new object[] { payload, sequenceId });
        }

        // If SequenceId passed, then update sequenceId
        if (sequenceId != null)
        {
            SequenceId = sequenceId.Value;
        }
        else
        {
            IsDirty = true;
            SequenceId = ++SequenceId;
            Events.Add(new Event(Id, SequenceId, payload));
        }
        UpdatedDate = DateTime.UtcNow;
    }

    public void SetStatus(HttpStatusCode statusCode, string error = null)
    {
        Error = error;
        StatusCode = statusCode;
        foreach (var response in Responses)
        {
            response.StatusCode = (int)statusCode;
            response.Error = error;
        }
    }

    private void Setup()
    {
        // Initialize Data
        State ??= Activator.CreateInstance<T>();
        State.Id = Id;
        var properties = AssemblyUtil.StatePropertiesWithSubStates[_type];
        AllStates = properties
            .ToDictionary(x => x.PropertyType, x =>
            {
                var state = Activator.CreateInstance(x.PropertyType);
                ((dynamic)state)!.Id = Id;
                x.SetValue(State, state);
                return state;
            });
        AllStates[_type] = State;
    }

    public bool Exists() => SequenceId > 0;

    public Snapshot<T> GetSnapshot() => new(Id, SequenceId, State, CreatedDate, UpdatedDate);
    public View<T> GetView() => new(Id, SequenceId, State, CreatedDate, UpdatedDate);
}

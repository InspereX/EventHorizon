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
    private readonly StreamUtil _streamUtil;
    internal readonly List<Event> Events = new();
    internal readonly List<Response> Responses = new();
    private readonly Type _type = typeof(T);
    private readonly string _topic;
    private Dictionary<Type, object> AllStates { get; set; }
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
    public string Error { get; private set; }
    public bool IsDirty { get; private set; }
    public string Id { get; set; }
    public long SequenceId { get; set; }
    public T State { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }

    public Aggregate(string streamId, StreamUtil streamUtil)
    {
        _streamUtil = streamUtil;
        _topic = _streamUtil.GetTopic(typeof(T));

        Id = streamId;
        CreatedDate = UpdatedDate = DateTime.UtcNow;
        Setup();
    }

    public Aggregate(IStateParent<T> model, StreamUtil streamUtil)
    {
        _streamUtil = streamUtil;
        _topic = _streamUtil.GetTopic(typeof(T));
        Id = model.Id;
        SequenceId = model.SequenceId;
        State = model.State;
        CreatedDate = model.CreatedDate;
        UpdatedDate = model.UpdatedDate;
        Setup();
    }

    public Aggregate(MessageContext<Event>[] events, StreamUtil streamUtil)
    {
        _streamUtil = streamUtil;
        _topic = _streamUtil.GetTopic(typeof(T));

        // Create
        Setup();
        Id = events.Select(x => x.Data.StreamId).FirstOrDefault();
        CreatedDate = DateTime.UtcNow;

        // Defensive
        if (!events.Any()) return;

        // Apply Events
        foreach (var @event in events)
            Apply(@event.Data, @event.TopicData?.Topic, false);
    }

    public void Handle(Command command, string topic = default)
    {
        topic ??= _topic;

        // Try Self
        var payload = _streamUtil.GetPayload(topic, command);
        foreach (var state in AllStates)
        {
            var context = new AggregateContext(Exists());
            var method = AggregateAssemblyUtil.StateToCommandHandlersDict.GetValueOrDefault(state.Key)?.GetValueOrDefault(payload.GetType());
            method?.Invoke(state.Value, parameters: new [] { payload, context } );
            foreach(var item in context.Events)
                Apply(new Event(Id, SequenceId, item));
        }
    }

    public void Handle(Request request, string topic = default)
    {
        topic ??= _topic;

        // Try Self
        var payload = _streamUtil.GetPayload(topic, request);
        foreach (var state in AllStates)
        {
            var context = new AggregateContext(Exists());
            var method = AggregateAssemblyUtil.StateToRequestHandlersDict.GetValueOrDefault(state.Key)?.GetValueOrDefault(payload.GetType());
            var result = method?.Invoke(state.Value, parameters: new [] { payload, context } );
            Responses.Add(new Response(request.Id, request.SenderId, Id, result, Error, (int)StatusCode));
            foreach(var item in context.Events)
                Apply(new Event(Id, SequenceId, item), topic);
        }
    }

    public void Apply(IEvent<T> @event, string topic = default)
    {
        Apply(new Event(Id, ++SequenceId, @event), topic);
    }

    public void Apply(Event @event, string topic = default, bool isFirstTime = true)
    {
        topic ??= _topic;

        // Try Self
        var payload = _streamUtil.GetPayload(topic, @event);
        foreach (var state in AllStates)
        {
            var method = AggregateAssemblyUtil.StateToEventHandlersDict.GetValueOrDefault(state.Key)?.GetValueOrDefault(payload.GetType());
            method?.Invoke(state.Value, parameters: new [] { payload } );
        }

        // Track Events only if first time applying
        if (isFirstTime)
        {
            @event.SequenceId = ++SequenceId;
            Events.Add(@event);
        }
        else
        {
            SequenceId = @event.SequenceId;
        }

        IsDirty = true;
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

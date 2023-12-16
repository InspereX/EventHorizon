using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json;
using Insperex.EventHorizon.Abstractions.Models;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.Abstractions.Util;
using Insperex.EventHorizon.EventSourcing.Aggregates;
using Insperex.EventHorizon.EventSourcing.Samples.Models.Actions;
using Insperex.EventHorizon.EventSourcing.Samples.Models.Snapshots;
using Insperex.EventHorizon.EventSourcing.Samples.Models.View;
using Insperex.EventHorizon.EventStore.Models;
using Insperex.EventHorizon.EventStreaming.InMemory;
using Insperex.EventHorizon.EventStreaming.Util;
using Xunit;

namespace Insperex.EventHorizon.EventSourcing.Test.Unit;

[Trait("Category", "Unit")]
public class AggregateUnitTests
{
    private readonly string _streamId;

    public AggregateUnitTests()
    {
        _streamId = "123";
    }

    [Fact]
    public void TestAggregateFromEvents()
    {
        var events = Enumerable.Range(0, 5).Select(x => new AccountCredited(100)).ToArray();
        var aggregate = new Aggregate<AccountState>(_streamId, events);

        Assert.Equal(_streamId, aggregate.Id);
        Assert.Equal(5, aggregate.SequenceId);
        Assert.Equal(events.Sum(x => x.Amount), aggregate.State.Amount);
        Assert.True(aggregate.Exists());
    }

    [Fact]
    public void TestAggregateFromSnapshot()
    {
        var state = new AccountState { Id = _streamId, Amount = 100 };
        var snapshotWrapper = new Snapshot<AccountState>(state.Id, 1, state, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
        var aggregate = new Aggregate<AccountState>(snapshotWrapper);

        Assert.Equal(snapshotWrapper.Id, aggregate.Id);
        Assert.Equal(snapshotWrapper.SequenceId, aggregate.SequenceId);
        Assert.Equal(snapshotWrapper.CreatedDate, aggregate.CreatedDate);
        Assert.Equal(snapshotWrapper.UpdatedDate, aggregate.UpdatedDate);
        Assert.Equal(state.Amount, aggregate.State.Amount);
        Assert.True(aggregate.Exists());
    }


    [Fact]
    public void TestAggregateFromOnlyStreamId()
    {
        var aggregate = new Aggregate<AccountState>(_streamId);

        Assert.Equal(_streamId, aggregate.Id);
        Assert.Equal(0, aggregate.SequenceId);
        Assert.NotEqual(default, aggregate.CreatedDate);
        Assert.Equal(aggregate.CreatedDate, aggregate.UpdatedDate);
        Assert.False(aggregate.Exists());
    }

    [Fact]
    public void TestApplyEventBasicView()
    {
        // Create Aggregate and Apply
        var @event = new AccountOpened(100);
        var agg = new Aggregate<AccountView>(_streamId);
        agg.Apply(@event);

        // Assert State and Agg
        Assert.Equal(_streamId, agg.Id);
        Assert.Equal(_streamId, agg.State.Id);
        Assert.Equal(1, agg.SequenceId);
        Assert.Equal(@event.Amount, agg.State.Amount);

        // Assert Event
        Assert.Single(agg.Events);
    }

    [Fact]
    public void TestApplyEventAdvancedView()
    {
        // Create Aggregate and Apply
        var @event = new AccountOpened(100);
        var agg = new Aggregate<SearchAccountView>(_streamId);
        agg.Apply(@event);

        // Assert State and Agg
        Assert.Equal(_streamId, agg.Id);
        Assert.Equal(_streamId, agg.State.Id);
        Assert.Equal(1, agg.SequenceId);
        Assert.Equal(@event.Amount, agg.State.Account.Amount);

        // Assert Event
        Assert.Single(agg.Events);
    }

    [Fact]
    public void TestHandleCommand()
    {
        // Create Aggregate and Apply
        var command = new ChangeUserName("Bob");
        var agg = new Aggregate<UserState>(_streamId);
        agg.Handle(command);

        // Assert State and Agg
        Assert.Equal(_streamId, agg.Id);
        Assert.Equal(_streamId, agg.State.Id);
        Assert.Equal(1, agg.SequenceId);
        Assert.Equal(command.Name, agg.State.Name);

        // Assert Event
        var @event = agg.Events.First();
        var actual = JsonSerializer.Deserialize<AccountOpened>(@event.Payload);
        Assert.Equal(_streamId, @event.StreamId);
        Assert.Equal(1, @event.SequenceId);
        Assert.Equal(actual.Amount, actual!.Amount);
    }

    [Fact]
    public void TestHandleRequestResponse()
    {
        // Create Aggregate and Apply
        var req = new OpenAccount(100);
        var agg = new Aggregate<AccountState>(_streamId);
        agg.Handle(req, "123", "123");

        // Assert State and Agg
        Assert.Equal(_streamId, agg.Id);
        Assert.Equal(_streamId, agg.State.Id);
        Assert.Equal(1, agg.SequenceId);
        Assert.Equal(req.Amount, agg.State.Amount);

        // Assert Event
        var @event = agg.Events.First();
        var actual = JsonSerializer.Deserialize<AccountOpened>(@event.Payload);
        Assert.Equal(_streamId, @event.StreamId);
        Assert.Equal(1, @event.SequenceId);
        Assert.Equal(actual.Amount, actual!.Amount);

        // Assert Results
        var result = JsonSerializer.Deserialize<AccountResponse>(agg.Responses.First().Payload);
        Assert.Equal(HttpStatusCode.OK, result!.StatusCode);
    }

    [Fact]
    public void TestHandleRequestResponseFailedResult()
    {
        // Create Aggregate and Apply
        var request = new Withdrawal(100);
        var agg = new Aggregate<AccountState>(_streamId);
        agg.Handle(request, "123", "123");

        // Assert State and Agg
        Assert.Equal(_streamId, agg.Id);
        Assert.Equal(_streamId, agg.State.Id);
        Assert.Equal(0, agg.SequenceId);
        Assert.Equal(0, agg.State.Amount);

        // Assert Event
        Assert.Empty(agg.Events);

        // Assert Results
        var result = agg.Responses.First();
        var actual = JsonSerializer.Deserialize<AccountResponse>(result.Payload);
        Assert.Equal(HttpStatusCode.InternalServerError, actual.StatusCode);
        Assert.Equal(AccountConstants.WithdrawalDenied, actual.Error);
    }

    [Fact]
    public void TestHandleRequestResponseAggregateRoot()
    {
        // Create Aggregate and Apply
        var request = new OpenAccount(100);
        var agg = new Aggregate<BankAccount>(_streamId);
        agg.Handle(request, "123", "123");

        // Assert State and Agg
        Assert.Equal(_streamId, agg.Id);
        Assert.Equal(1, agg.SequenceId);
        Assert.Equal(_streamId, agg.State.AccountState.Id);
        Assert.Equal(request.Amount, agg.State.AccountState.Amount);

        // Assert Event
        var @event = agg.Events.First();
        var actual = JsonSerializer.Deserialize<AccountOpened>(@event.Payload);
        Assert.Equal(_streamId, @event.StreamId);
        Assert.Equal(1, @event.SequenceId);
        Assert.Equal(actual.Amount, actual!.Amount);
    }
}

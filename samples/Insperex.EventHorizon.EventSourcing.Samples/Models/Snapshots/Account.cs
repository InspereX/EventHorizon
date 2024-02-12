﻿using System.Collections.Generic;
using System.Net;
using Insperex.EventHorizon.Abstractions.Attributes;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Actions;
using Insperex.EventHorizon.Abstractions.Interfaces.Handlers;
using Insperex.EventHorizon.Abstractions.Models;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.EventSourcing.Samples.Models.Actions;
using Insperex.EventHorizon.EventStore.MongoDb.Attributes;
using Insperex.EventHorizon.EventStore.MongoDb.Models;
using Insperex.EventHorizon.EventStreaming.Pulsar.Attributes;
using MongoDB.Driver;

namespace Insperex.EventHorizon.EventSourcing.Samples.Models.Snapshots;

[Stream("$type")]
[PulsarNamespace("test_bank", "account")]
[SnapshotStore("test_bank_snapshot_account")]
[MongoCollection(ReadPreferenceMode = ReadPreferenceMode.SecondaryPreferred,
    ReadConcernLevel = ReadConcernLevel.Majority,
    WriteConcernLevel = WriteConcernLevel.Majority)]
public class Account : IState,
    IHandleRequest<OpenAccount, AccountResponse>,
    IHandleRequest<Withdrawal, AccountResponse>,
    IHandleRequest<Deposit, AccountResponse>,
    IHandleEvent<AccountOpened>,
    IApplyEvent<AccountOpened>,
    IApplyEvent<AccountDebited>,
    IApplyEvent<AccountCredited>
{
    public string Id { get; set; }

    [StreamPartitionKey]
    public string BankAccount { get; set; }
    public int Amount { get; set; }

    #region Requests

    public AccountResponse Handle(OpenAccount request, AggregateContext context)
    {
        if(!context.Exists)
            context.AddEvent(new AccountOpened(request.Amount));

        return new AccountResponse();
    }

    public AccountResponse Handle(Withdrawal request, AggregateContext context)
    {
        if(Amount < request.Amount)
            return new AccountResponse(HttpStatusCode.InternalServerError, AccountConstants.WithdrawalDenied);

        if(request.Amount != 0 && Amount >= request.Amount)
            context.AddEvent(new AccountDebited(request.Amount));

        return new AccountResponse();
    }

    public AccountResponse Handle(Deposit request, AggregateContext context)
    {
        context.AddEvent(new AccountCredited(request.Amount));
        return new AccountResponse();
    }

    public void Handle(AccountOpened @event, AggregateContext context) => context.AddEvent(@event);

    #endregion

    #region Applys

    public void Apply(AccountDebited @event) => Amount -= @event.Amount;
    public void Apply(AccountCredited @event) => Amount += @event.Amount;
    public void Apply(AccountOpened @event) => Amount = @event.Amount;

    #endregion
}

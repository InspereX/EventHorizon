﻿using System.Collections.Generic;
using Insperex.EventHorizon.Abstractions.Attributes;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.EventSourcing.Interfaces;
using Insperex.EventHorizon.EventSourcing.Interfaces.State;

namespace Insperex.EventHorizon.EventSourcing.Samples.Models.Snapshots;

[SnapshotStore("test_snapshot_bank_account", nameof(Account))]
[EventStream("test_event_bank_account", nameof(Account))]
public class Account : IState, 
    IHandleRequest<OpenAccount, AccountResponse>, 
    IHandleRequest<Withdrawal, AccountResponse>, 
    IHandleRequest<Deposit, AccountResponse>,
    IApplyEvent<AccountOpened>,
    IApplyEvent<AccountDebited>,
    IApplyEvent<AccountCredited>
{
    public string Id { get; set; }
    public int Amount { get; set; }

    #region Requests

    public AccountResponse Handle(OpenAccount request, List<IEvent> events)
    {
        if(Amount == default)
            events.Add(new AccountOpened(request.Amount));
            
        return new AccountResponse();
    }

    public AccountResponse Handle(Withdrawal request, List<IEvent> events)
    {
        if(Amount < request.Amount)
            return new AccountResponse(AccountResponseStatus.WithdrawalDenied);
        
        if(request.Amount != 0 && Amount >= request.Amount)
            events.Add(new AccountDebited(request.Amount));

        return new AccountResponse();
    }

    public AccountResponse Handle(Deposit request, List<IEvent> events)
    {
        events.Add(new AccountCredited(request.Amount));
        return new AccountResponse();
    } 

    #endregion

    #region Applys
    
    public void Apply(AccountDebited payload) => Amount -= payload.Amount;
    public void Apply(AccountCredited payload) => Amount += payload.Amount;
    public void Apply(AccountOpened payload) => Amount = payload.Amount;
    
    #endregion
}

[EventStream("test_event_bank_account", nameof(Account))]
public interface IApplyAccountEvents :
    IApplyEvent<AccountOpened>,
    IApplyEvent<AccountDebited>,
    IApplyEvent<AccountCredited>
{
    
}

// Request
public record OpenAccount(int Amount) : IRequest<Account, AccountResponse>;
public record Withdrawal(int Amount) : IRequest<Account, AccountResponse>;
public record Deposit(int Amount) : IRequest<Account, AccountResponse>;

// Events
public record AccountOpened(int Amount) : IEvent<Account>;
public record AccountDebited(int Amount) : IEvent<Account>;
public record AccountCredited(int Amount) : IEvent<Account>;

// Response
public record AccountResponse(AccountResponseStatus Status = AccountResponseStatus.Success) : IResponse<Account>;

public enum AccountResponseStatus
{
    Success,
    WithdrawalDenied,
    // ----- Internal Errors Below ----
    CommandTimedOut,
    LoadSnapshotFailed,
    SaveSnapshotFailed,
    SaveEventsFailed,
}
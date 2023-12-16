using System.Net;
using Insperex.EventHorizon.Abstractions.Interfaces.Actions;
using Insperex.EventHorizon.EventSourcing.Samples.Models.Snapshots;

namespace Insperex.EventHorizon.EventSourcing.Samples.Models.Actions;

// Request
public record OpenAccount(int Amount) : IRequest<AccountState, AccountResponse>;
public record Withdrawal(int Amount) : IRequest<AccountState, AccountResponse>;
public record Deposit(int Amount) : IRequest<AccountState, AccountResponse>;

// Events
public record AccountOpened(int Amount) : IEvent<AccountState>;
public record AccountDebited(int Amount) : IEvent<AccountState>;
public record AccountCredited(int Amount) : IEvent<AccountState>;

// Response
public record AccountResponse(HttpStatusCode StatusCode = HttpStatusCode.OK, string Error = null) : IResponse<AccountState>;

public static class AccountConstants
{
    public const string WithdrawalDenied = "Insufficient Funds";
}

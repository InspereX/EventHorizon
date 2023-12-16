using Insperex.EventHorizon.Abstractions.Interfaces;

namespace Insperex.EventHorizon.EventSourcing.Samples.Models.Snapshots;

public class BankAccount : IState
{
    public string Id { get; set; }
    public UserState UserState { get; set; }
    public AccountState AccountState { get; set; }
}

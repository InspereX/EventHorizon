﻿using System;
using System.Collections.Generic;
using Insperex.EventHorizon.Abstractions.Attributes;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Actions;
using Insperex.EventHorizon.Abstractions.Interfaces.Handlers;
using Insperex.EventHorizon.Abstractions.Models;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.EventStreaming.Interfaces;
using Insperex.EventHorizon.EventStreaming.Pulsar.Attributes;

namespace Insperex.EventHorizon.EventSourcing.Samples.Models.Snapshots;

[Stream("$type")]
[PulsarNamespace("test_bank", "user")]
[SnapshotStore("test_snapshot_bank_user")]
public class UserState : IState,
    IHandleCommand<ChangeUserName>,
    IApplyEvent<UserNameChangedV2>
{
    public string Id { get; set; }
    public string Name { get; set; }

    public void Handle(ChangeUserName command, AggregateContext context)
    {
        if(Name != command.Name)
            context.AddEvent(new UserNameChangedV2(command.Name));
    }

    public void Apply(UserNameChangedV2 @event)
    {
        Name = @event.Name;
    }
}

// Commands
public record ChangeUserName([property: StreamPartitionKey]string Name) : ICommand<UserState>;

// Events
public record UserNameChangedV2(string Name) : IEvent<UserState>;

// Legacy Events
[Obsolete]
public record UserNameChanged(string Name) : IEvent<UserState>, IUpgradeTo<UserNameChangedV2>
{
    public UserNameChangedV2 Upgrade()
    {
        return new UserNameChangedV2(Name);
    }
}

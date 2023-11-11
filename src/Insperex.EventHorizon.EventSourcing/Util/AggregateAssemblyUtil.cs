using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Insperex.EventHorizon.Abstractions.Interfaces.Actions;
using Insperex.EventHorizon.Abstractions.Interfaces.Handlers;
using Insperex.EventHorizon.Abstractions.Util;

namespace Insperex.EventHorizon.EventSourcing.Util;

public static class AggregateAssemblyUtil
{
    public static readonly ImmutableDictionary<Type, Dictionary<Type, MethodInfo>> StateToCommandHandlersDict = GetStateActionHandlerDict(typeof(IHandleCommand<>), "Handle");
    public static readonly ImmutableDictionary<Type, Dictionary<Type, MethodInfo>> StateToRequestHandlersDict = GetStateActionHandlerDict(typeof(IHandleRequest<,>), "Handle");
    public static readonly ImmutableDictionary<Type, Dictionary<Type, MethodInfo>> StateToEventHandlersDict = GetStateActionHandlerDict(typeof(IApplyEvent<>), "Apply");

    private static ImmutableDictionary<Type, Dictionary<Type, MethodInfo>> GetStateActionHandlerDict(MemberInfo type, string methodName) => AssemblyUtil.States
        .ToImmutableDictionary(x => x, x => x.GetInterfaces()
            .Where(i => i.Name == type.Name).ToDictionary(d => d.GetGenericArguments()[0], d => d.GetMethod(methodName)));

}

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Actions;
using Microsoft.Extensions.DependencyModel;

namespace Insperex.EventHorizon.Abstractions.Util;

public static class AssemblyUtil
{
    private static readonly Assembly Assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
    public static readonly string AssemblyName = Assembly.GetName().Name;

    private static readonly Type[] Types = DependencyContext.Default?.CompileLibraries
        .SelectMany(x =>
        {
            try
            {
                return Assembly.Load(x.Name)?.GetTypes();
            }
            catch (Exception)
            {
                return Array.Empty<Type>();
            }
        })
        .Where(x => x != null)
        .ToArray();

    #region States

    public static Type[] States = Types.Where(x => typeof(IState).IsAssignableFrom(x)).ToArray();

    public static readonly ImmutableDictionary<Type, PropertyInfo[]> StatePropertiesWithSubStates = States.ToImmutableDictionary(x => x, GetStatePropertiesWithState);

    public static readonly ImmutableDictionary<Type, Type[]> StateSubStates = StatePropertiesWithSubStates
        .ToImmutableDictionary(x => x.Key, x => x.Value.Select(s => s.PropertyType).ToArray());

    private static PropertyInfo[] GetStatePropertiesWithState(Type type) => type.GetProperties()
        .Where(p => p.PropertyType.GetInterface(nameof(IState)) != null)
        .ToArray();

    #endregion

    #region Actions

    public static Type[] Actions = Types.Where(x => typeof(IAction).IsAssignableFrom(x)).ToArray();

    public static readonly Dictionary<Type, Type[]> StateToCommandsLookup = GetStatesToActionLookup(typeof(ICommand<>));
    public static readonly Dictionary<Type, Type[]> StateToRequestsLookup = GetStatesToActionLookup(typeof(IRequest<,>));
    public static readonly Dictionary<Type, Type[]> StateToEventsLookup = GetStatesToActionLookup(typeof(IEvent<>));

    private static Dictionary<Type, Type[]> GetStatesToActionLookup(Type type) => States
        .ToDictionary(x => x, x => Actions.Where(a => a.GetInterfaces().Any(i => i.IsGenericType
                && i.GetGenericTypeDefinition() == type
                && i.GetGenericArguments()[0].Name == x.Name))
            .ToArray());

    #endregion
}

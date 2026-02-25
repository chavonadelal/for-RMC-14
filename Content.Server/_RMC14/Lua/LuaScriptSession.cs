using System.Collections.Generic;
using System.Text;
using MoonSharp.Interpreter;

namespace Content.Server._RMC14.Lua;

/// <summary>
///     Persistent context for one Lua "owner" + "actor": script, output buffer, and event subscriptions by key.
/// </summary>
public sealed class LuaScriptSession
{
    public EntityUid Owner { get; }
    public EntityUid Actor { get; }
    public Script Script { get; }
    public StringBuilder Output { get; }

    /// <summary>
    ///     Event key -> callback (one per key for now).
    /// </summary>
    public Dictionary<string, DynValue> Subscriptions { get; } = new();

    public bool HasSubscriptions => Subscriptions.Count > 0;

    public LuaScriptSession(EntityUid owner, EntityUid actor, Script script, StringBuilder output)
    {
        Owner = owner;
        Actor = actor;
        Script = script;
        Output = output;
    }

    public void Subscribe(string eventKey, DynValue callback)
    {
        Subscriptions[eventKey] = callback;
    }

    public void ClearSubscriptions()
    {
        Subscriptions.Clear();
    }
}

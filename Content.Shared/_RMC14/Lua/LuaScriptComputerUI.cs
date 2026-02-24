using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Lua;

[Serializable, NetSerializable]
public enum LuaScriptUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class LuaScriptRunBuiMsg(string code) : BoundUserInterfaceMessage
{
    public readonly string Code = code;
}

[Serializable, NetSerializable]
public sealed class LuaScriptBuiState(string output, string? error, bool timedOut) : BoundUserInterfaceState
{
    public readonly string Output = output;
    public readonly string? Error = error;
    public readonly bool TimedOut = timedOut;
}

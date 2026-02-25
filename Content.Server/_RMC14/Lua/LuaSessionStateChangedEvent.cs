namespace Content.Server._RMC14.Lua;

/// <summary>
///     Raised when a Lua session's output or active state changes.
///     UI owners (e.g. LuaScriptComputerSystem) subscribe and call SetUiState for their entity.
/// </summary>
public sealed record LuaSessionStateChangedEvent(
    EntityUid Owner,
    EntityUid Actor,
    string Output,
    string? Error,
    bool TimedOut,
    bool IsActive
);

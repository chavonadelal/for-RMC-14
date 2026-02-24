namespace Content.Server._RMC14.Lua;

/// <summary>
///     Result of a single Lua script execution.
/// </summary>
public sealed record LuaRunResult(
    bool Success,
    string Output,
    string? Error,
    bool TimedOut
);

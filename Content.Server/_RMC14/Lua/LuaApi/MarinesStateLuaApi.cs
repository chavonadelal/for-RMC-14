using MoonSharp.Interpreter;

namespace Content.Server._RMC14.Lua.LuaApi;

/// <summary>
///     Lua API for subscribing to marine state events (death, crit).
///     Calls session.Subscribe(eventKey, callback) so the runner stays generic.
/// </summary>
public sealed class MarinesStateLuaApi
{
    private readonly LuaScriptSession _session;

    public MarinesStateLuaApi(LuaScriptSession session)
    {
        _session = session;
    }

    /// <summary>
    ///     Subscribe to marine death. Callback receives (marineName: string).
    /// </summary>
    public void OnMarineDie(DynValue callback)
    {
        if (callback?.Type != DataType.Function)
            return;
        _session.Subscribe("MarineDie", callback);
    }

    /// <summary>
    ///     Subscribe to marine going critical. Callback receives (marineName: string).
    /// </summary>
    public void OnMarineCrit(DynValue callback)
    {
        if (callback?.Type != DataType.Function)
            return;
        _session.Subscribe("MarineCrit", callback);
    }
}

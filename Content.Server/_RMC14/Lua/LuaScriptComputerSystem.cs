using Content.Shared._RMC14.Lua;
using Robust.Server.GameObjects;

namespace Content.Server._RMC14.Lua;

public sealed class LuaScriptComputerSystem : EntitySystem
{
    [Dependency] private readonly LuaScriptRunnerSystem _luaRunner = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LuaSessionStateChangedEvent>(OnSessionStateChanged);
        Subs.BuiEvents<LuaScriptComputerComponent>(LuaScriptUiKey.Key, subs =>
        {
            subs.Event<LuaScriptRunBuiMsg>(OnRun);
            subs.Event<LuaScriptStopBuiMsg>(OnStop);
        });
    }

    private void OnSessionStateChanged(LuaSessionStateChangedEvent ev)
    {
        _ui.SetUiState(ev.Owner, LuaScriptUiKey.Key, new LuaScriptBuiState(
            ev.Output,
            ev.Error,
            ev.TimedOut,
            ev.IsActive
        ));
    }

    private void OnRun(Entity<LuaScriptComputerComponent> ent, ref LuaScriptRunBuiMsg msg)
    {
        _luaRunner.RunWithSession(ent.Owner, msg.Actor, msg.Code);
    }

    private void OnStop(Entity<LuaScriptComputerComponent> ent, ref LuaScriptStopBuiMsg msg)
    {
        _luaRunner.StopSession(ent.Owner, msg.Actor);
    }
}

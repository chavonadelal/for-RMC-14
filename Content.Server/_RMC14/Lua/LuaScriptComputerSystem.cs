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
        Subs.BuiEvents<LuaScriptComputerComponent>(LuaScriptUiKey.Key, subs =>
        {
            subs.Event<LuaScriptRunBuiMsg>(OnRun);
        });
    }

    private void OnRun(Entity<LuaScriptComputerComponent> ent, ref LuaScriptRunBuiMsg msg)
    {
        var result = _luaRunner.Run(msg.Code);
        var output = result.Output;
        if (result.Success && !result.TimedOut)
            output += (string.IsNullOrEmpty(output) ? "" : "\n") + "[OK] Task completed";
        _ui.SetUiState(ent.Owner, LuaScriptUiKey.Key, new LuaScriptBuiState(
            output,
            result.Error,
            result.TimedOut
        ));
    }
}

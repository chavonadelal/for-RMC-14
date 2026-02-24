using System.Collections.Generic;
using System.Text;
using Content.Server._RMC14.Lua.LuaApi;
using Content.Shared._RMC14.ARES;
using Content.Shared._RMC14.Marines.Announce;
using MoonSharp.Interpreter;
using Robust.Shared.GameObjects;

namespace Content.Server._RMC14.Lua;

public sealed class LuaScriptRunnerSystem : EntitySystem
{
    [Dependency] private readonly ARESSystem _ares = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;

    private static bool _typeRegistered;

    public override void Initialize()
    {
        base.Initialize();
        if (!_typeRegistered)
        {
            UserData.RegisterType<CommunicationsLuaApi>();
            _typeRegistered = true;
        }
    }

    /// <summary>
    ///     Runs Lua code on the main thread with Preset_HardSandbox: string, table, math, bit32,
    ///     basic (type, tostring, etc.), table iterators (pairs, ipairs). No load/require, no io/os, no file access.
    ///     print() is overridden to write to the script output buffer.
    /// </summary>
    public LuaRunResult Run(string code)
    {
        var output = new StringBuilder();
        var script = new Script(CoreModules.Preset_HardSandbox);
        var api = new CommunicationsLuaApi(output, _ares, _marineAnnounce);
        script.Globals["Communications"] = UserData.Create(api);
        script.Globals["print"] = (ScriptExecutionContext ctx, CallbackArguments args) =>
        {
            var parts = new List<string>();
            for (var i = 0; i < args.Count; i++)
                parts.Add(args[i].ToPrintString());
            output.AppendLine(string.Join("\t", parts));
            return DynValue.Nil;
        };

        string? error = null;
        try
        {
            script.DoString(code ?? string.Empty);
        }
        catch (ScriptRuntimeException ex)
        {
            error = ex.DecoratedMessage;
        }
        catch (SyntaxErrorException ex)
        {
            error = ex.DecoratedMessage;
        }

        return new LuaRunResult(
            Success: error == null,
            Output: output.ToString(),
            Error: error,
            TimedOut: false
        );
    }
}

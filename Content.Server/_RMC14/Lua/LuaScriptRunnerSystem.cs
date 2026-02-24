using System.Text;
using System.Threading.Tasks;
using Content.Server._RMC14.Lua.LuaApi;
using Content.Server.Radio.EntitySystems;
using Content.Shared._RMC14.Marines.Announce;
using MoonSharp.Interpreter;
using Robust.Shared.GameObjects;

namespace Content.Server._RMC14.Lua;

public sealed class LuaScriptRunnerSystem : EntitySystem
{
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;

    private const double DefaultTimeoutSeconds = 10.0;
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
    ///     Runs Lua code with the communications API exposed as global "communications".
    ///     Context (source entity, custom timeout) can be added later.
    /// </summary>
    public LuaRunResult Run(string code)
    {
        var output = new StringBuilder();
        var script = new Script(CoreModules.None);
        var api = new CommunicationsLuaApi(output, _radio, _marineAnnounce);
        script.Globals["Communications"] = UserData.Create(api);

        var timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);
        string? error = null;
        var timedOut = false;

        var task = Task.Run(() =>
        {
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
        });

        if (!task.Wait(timeout))
        {
            timedOut = true;
            error = "Execution timed out.";
        }

        return new LuaRunResult(
            Success: error == null && !timedOut,
            Output: output.ToString(),
            Error: error,
            TimedOut: timedOut
        );
    }
}

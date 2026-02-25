using System.Collections.Generic;
using System.Text;
using Content.Server._RMC14.Lua.LuaApi;
using Content.Shared._RMC14.ARES;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared.Mobs;
using MoonSharp.Interpreter;
using Robust.Shared.GameObjects;

namespace Content.Server._RMC14.Lua;

public sealed class LuaScriptRunnerSystem : EntitySystem
{
    [Dependency] private readonly ARESSystem _ares = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;

    private static bool _typeRegistered;
    private readonly Dictionary<(EntityUid Owner, EntityUid Actor), LuaScriptSession> _sessions = new();

    public override void Initialize()
    {
        base.Initialize();
        if (!_typeRegistered)
        {
            UserData.RegisterType<CommunicationsLuaApi>();
            UserData.RegisterType<MarinesStateLuaApi>();
            _typeRegistered = true;
        }
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    /// <summary>
    ///     Runs Lua code in a session (create or reuse). Raises LuaSessionStateChangedEvent when done.
    /// </summary>
    public void RunWithSession(EntityUid owner, EntityUid actor, string code)
    {
        var session = GetOrCreateSession(owner, actor);
        session.ClearSubscriptions();

        string? error = null;
        try
        {
            session.Script.DoString(code ?? string.Empty);
        }
        catch (ScriptRuntimeException ex)
        {
            error = ex.DecoratedMessage;
        }
        catch (SyntaxErrorException ex)
        {
            error = ex.DecoratedMessage;
        }

        if (!string.IsNullOrEmpty(error))
            session.Output.AppendLine("[Error] " + error);
        else if (session.HasSubscriptions)
            session.Output.AppendLine("[OK] Subscriptions active. Press Stop to clear.");

        RaiseStateChanged(session, error, false);
    }

    /// <summary>
    ///     Stops the session and raises LuaSessionStateChangedEvent with IsActive: false.
    /// </summary>
    public void StopSession(EntityUid owner, EntityUid actor)
    {
        var key = (owner, actor);
        if (!_sessions.Remove(key, out var session))
        {
            RaiseLocalEvent(new LuaSessionStateChangedEvent(owner, actor, string.Empty, null, false, false));
            return;
        }
        RaiseLocalEvent(new LuaSessionStateChangedEvent(session.Owner, session.Actor, session.Output.ToString(), null, false, false));
    }

    /// <summary>
    ///     Invokes all session callbacks for the given event key (e.g. "MarineDie", "MarineCrit").
    /// </summary>
    public void InvokeSubscriptions(string eventKey, params DynValue[] args)
    {
        foreach (var session in _sessions.Values)
        {
            if (!session.Subscriptions.TryGetValue(eventKey, out var callback))
                continue;
            try
            {
                session.Script.Call(callback, args);
            }
            catch (ScriptRuntimeException ex)
            {
                session.Output.AppendLine($"[Event error] {ex.DecoratedMessage}");
            }
            catch (SyntaxErrorException ex)
            {
                session.Output.AppendLine($"[Event error] {ex.DecoratedMessage}");
            }
            RaiseStateChanged(session, null, false);
        }
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (!HasComp<MarineComponent>(ev.Target))
            return;
        var name = Name(ev.Target);
        var arg = DynValue.NewString(name);
        switch (ev.NewMobState)
        {
            case MobState.Dead:
                InvokeSubscriptions("MarineDie", arg);
                break;
            case MobState.Critical:
                InvokeSubscriptions("MarineCrit", arg);
                break;
        }
    }

    private LuaScriptSession GetOrCreateSession(EntityUid owner, EntityUid actor)
    {
        var key = (owner, actor);
        if (_sessions.TryGetValue(key, out var existing))
            return existing;

        var output = new StringBuilder();
        var script = new Script(CoreModules.Preset_HardSandbox);
        var session = new LuaScriptSession(owner, actor, script, output);
        _sessions[key] = session;
        SetupSessionGlobals(session);
        return session;
    }

    private void SetupSessionGlobals(LuaScriptSession session)
    {
        var script = session.Script;
        var output = session.Output;

        script.Globals["Communications"] = UserData.Create(new CommunicationsLuaApi(output, _ares, _marineAnnounce));
        script.Globals["MarinesState"] = UserData.Create(new MarinesStateLuaApi(session));
        script.Globals["print"] = (ScriptExecutionContext ctx, CallbackArguments args) =>
        {
            var parts = new List<string>();
            for (var i = 0; i < args.Count; i++)
                parts.Add(args[i].ToPrintString());
            output.AppendLine(string.Join("\t", parts));
            return DynValue.Nil;
        };
    }

    private void RaiseStateChanged(LuaScriptSession session, string? error, bool timedOut)
    {
        RaiseLocalEvent(new LuaSessionStateChangedEvent(
            session.Owner,
            session.Actor,
            session.Output.ToString(),
            error,
            timedOut,
            session.HasSubscriptions
        ));
    }

    /// <summary>
    ///     One-shot run without session (no subscriptions). Used when no session behavior is needed.
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

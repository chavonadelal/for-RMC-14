using Content.Shared._RMC14.Lua;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Lua;

[UsedImplicitly]
public sealed class LuaScriptBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private LuaScriptWindow? _window;
    private Action<BaseButton.ButtonEventArgs>? _runHandler;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<LuaScriptWindow>();
        _runHandler = _ => SendMessage(new LuaScriptRunBuiMsg(Rope.Collapse(_window!.CodeInput.TextRope)));
        _window.RunButton.OnPressed += _runHandler;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_window is not { IsOpen: true } || state is not LuaScriptBuiState s)
            return;

        var text = s.Output;
        if (!string.IsNullOrEmpty(s.Error))
            text = (string.IsNullOrEmpty(text) ? "" : text + "\n") + "[Error] " + s.Error;
        if (s.TimedOut)
            text = (string.IsNullOrEmpty(text) ? "" : text + "\n") + "[Timed out]";
        _window.OutputDisplay.TextRope = new Rope.Leaf(text);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _window != null && _runHandler != null)
            _window.RunButton.OnPressed -= _runHandler;
        base.Dispose(disposing);
    }
}

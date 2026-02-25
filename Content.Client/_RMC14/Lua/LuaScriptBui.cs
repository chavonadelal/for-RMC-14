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
    private Action<BaseButton.ButtonEventArgs>? _stopHandler;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<LuaScriptWindow>();
        _runHandler = _ => OnRunPressed();
        _stopHandler = _ => OnStopPressed();
        _window.RunButton.OnPressed += _runHandler;
    }

    private void OnRunPressed()
    {
        if (_window == null)
            return;
        SendMessage(new LuaScriptRunBuiMsg(Rope.Collapse(_window.CodeInput.TextRope)));
        _window.CodeInput.Editable = false;
        SwitchToStop();
    }

    private void OnStopPressed()
    {
        SendMessage(new LuaScriptStopBuiMsg());
    }

    private void SwitchToRun()
    {
        if (_window == null)
            return;
        _window.RunButton.Text = "Run";
        _window.RunButton.OnPressed -= _stopHandler;
        _window.RunButton.OnPressed += _runHandler;
        _window.CodeInput.Editable = true;
    }

    private void SwitchToStop()
    {
        if (_window == null)
            return;
        _window.RunButton.Text = "Stop";
        _window.RunButton.OnPressed -= _runHandler;
        _window.RunButton.OnPressed += _stopHandler;
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

        if (s.IsActive)
        {
            _window.CodeInput.Editable = false;
            SwitchToStop();
        }
        else
        {
            SwitchToRun();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _window != null)
        {
            _window.RunButton.OnPressed -= _runHandler;
            _window.RunButton.OnPressed -= _stopHandler;
        }
        base.Dispose(disposing);
    }
}

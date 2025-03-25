using Content.Shared._RMC14.Megaphone;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Megaphone;

public sealed class MegaphoneBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private MegaphoneWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<MegaphoneWindow>();
        _window.SpeakButton.OnPressed += _ => OnSpeakPressed();
    }

    private void OnSpeakPressed()
    {
        if (_window == null)
            return;

        var message = _window.InputField.Text.Trim();
        if (!string.IsNullOrEmpty(message))
        {
            SendPredictedMessage(new MegaphoneSpeakMessage(message));
        }
    }
}

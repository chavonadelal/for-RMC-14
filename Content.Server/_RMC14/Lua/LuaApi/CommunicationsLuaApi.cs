using System.Text;
using Content.Server.Radio.EntitySystems;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared.Radio;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Lua.LuaApi;

/// <summary>
///     Lua-facing API for communications: radio and marine announcements.
///     Exposed to scripts as UserData (e.g. global "communications").
/// </summary>
public sealed class CommunicationsLuaApi
{
    private readonly StringBuilder _output;
    private readonly RadioSystem _radio;
    private readonly SharedMarineAnnounceSystem _marineAnnounce;

    public CommunicationsLuaApi(
        StringBuilder output,
        RadioSystem radio,
        SharedMarineAnnounceSystem marineAnnounce)
    {
        _output = output;
        _radio = radio;
        _marineAnnounce = marineAnnounce;
    }

    /// <summary>
    ///     Appends a line to the script output buffer (visible to the caller).
    /// </summary>
    public void Print(string text)
    {
        _output.AppendLine(text ?? string.Empty);
    }

    /// <summary>
    ///     Sends a message to the given radio channel.
    ///     Source entity is a placeholder until execution context is added.
    /// </summary>
    public void SendRadio(string channelId, string message)
    {
        try
        {
            var channel = new ProtoId<RadioChannelPrototype>(channelId);
            _radio.SendRadioMessage(EntityUid.Invalid, message ?? string.Empty, channel, EntityUid.Invalid, escapeMarkup: false);
        }
        catch (Exception ex)
        {
            _output.AppendLine($"SendRadio error: {ex.Message}");
        }
    }

    /// <summary>
    ///     Makes an announcement to all marines.
    /// </summary>
    public void AnnounceToMarines(string message)
    {
        _marineAnnounce.AnnounceToMarines(message ?? string.Empty);
    }
}

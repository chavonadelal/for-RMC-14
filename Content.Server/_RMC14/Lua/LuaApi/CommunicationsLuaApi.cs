using System.Text;
using Content.Shared._RMC14.ARES;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared.Radio;
using Robust.Shared.Audio;
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
    private readonly ARESSystem _ares;
    private readonly SharedMarineAnnounceSystem _marineAnnounce;

    public CommunicationsLuaApi(
        StringBuilder output,
        ARESSystem ares,
        SharedMarineAnnounceSystem marineAnnounce)
    {
        _output = output;
        _ares = ares;
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
    ///     Sends a message to the given radio channel as ARES (same as other ARES radio announcements).
    ///     channelId is the radio channel prototype id (e.g. "Common", "Command").
    /// </summary>
    public void SendRadio(string channelId, string message)
    {
        try
        {
            var channel = new ProtoId<RadioChannelPrototype>(channelId);
            var ares = _ares.EnsureARES();
            _marineAnnounce.AnnounceRadio(ares.Owner, message ?? string.Empty, channel);
        }
        catch (Exception ex)
        {
            _output.AppendLine($"SendRadio error: {ex.Message}");
        }
    }

    /// <summary>
    ///     Makes an ARES announcement to all marines (formatted as from ARES, with ARES sound).
    ///     Use from the ARES Lua console so announcements appear as from the ship AI.
    /// </summary>
    public void Announce(string message)
    {
        var sound = new SoundPathSpecifier("/Audio/_RMC14/AI/announce.ogg");
        _marineAnnounce.AnnounceARES(null, message ?? string.Empty, sound);
    }
}

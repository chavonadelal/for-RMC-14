using Content.Shared._RMC14.Dialog;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Commendations;

[ByRefEvent]
[Serializable, NetSerializable]
public sealed record RMCAwardRecommendationSelectMarineEvent(
    NetEntity Actor,
    NetEntity? Marine,
    string? LastPlayerId = null);

[ByRefEvent]
[Serializable, NetSerializable]
public sealed record RMCAwardRecommendationReasonEvent(
    NetEntity Actor,
    NetEntity? Marine,
    string? LastPlayerId,
    string Message = ""
) : DialogInputEvent(Message);

using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Ghost;

[Serializable, NetSerializable]
public sealed class RMCGhostTargetsRequestEvent : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class RMCGhostTargetsResponseEvent(List<RMCGhostTargetSectionModel> sections) : EntityEventArgs
{
    public List<RMCGhostTargetSectionModel> Sections = sections;
}

[Serializable, NetSerializable]
public sealed class RMCGhostTargetWarpRequestEvent(NetEntity target) : EntityEventArgs
{
    public NetEntity Target = target;
}

[Serializable, NetSerializable]
public sealed class RMCGhostTargetMostFollowedRequestEvent : EntityEventArgs;

[Serializable, NetSerializable]
public enum RMCGhostTargetSection : byte
{
    Marines,
    Humans,
    Xenos,
    Survivors,
    Infected,
    ErtMembers,
    Synthetics,
    Spp,
    Clf,
    WeYa,
    Hyperdyne,
    Tse,
    Freelancers,
    Mercenaries,
    Contractors,
    Hunted,
    Dutch,
    Marshals,
    Responders,
    Predators,
    Escaped,
    Thunderdome,
    Vehicles,
    Animals,
    Dead,
    Ghosts,
    Misc,
    Npcs,
    Locations,
}

[Serializable, NetSerializable]
public enum RMCGhostTargetState : byte
{
    Alive,
    Critical,
    Dead,
    Ghost,
    Location,
    Unknown,
}

[DataRecord]
[Serializable, NetSerializable]
public readonly record struct RMCGhostTargetSectionModel(
    RMCGhostTargetSection Section,
    int DisplayOrder,
    string NameLocId,
    Color? HeaderColor,
    List<RMCGhostTargetGroupModel> Groups);

[DataRecord]
[Serializable, NetSerializable]
public readonly record struct RMCGhostTargetGroupModel(
    int DisplayOrder,
    string Name,
    Color? HeaderColor,
    bool ShowHeader,
    List<RMCGhostTargetButton> Buttons);

[DataRecord]
[Serializable, NetSerializable]
public readonly record struct RMCGhostTargetButton(
    NetEntity Entity,
    string Label,
    string SearchText,
    RMCGhostTargetState State,
    int? HealthPercent,
    int Orbiters,
    bool IsGround,
    bool IsShip,
    bool IsLocation,
    SpriteSpecifier.Rsi? Icon,
    SpriteSpecifier.Rsi? Background,
    Color? BackgroundColor);

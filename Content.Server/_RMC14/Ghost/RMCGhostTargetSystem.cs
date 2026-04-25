using System.Linq;
using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Roles.Jobs;
using Content.Server.Warps;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Ghost;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Mutiny;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Roles;
using Content.Shared._RMC14.Survivor;
using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Follower;
using Content.Shared.Follower.Components;
using Content.Shared.Ghost;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Content.Shared.Warps;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Ghost;

public sealed class RMCGhostTargetSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly FollowerSystem _follower = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private EntityQuery<GhostComponent> _ghostQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;

    private const string UnmcFaction = "UNMC";
    private const string SppFaction = "SPP";
    private const string ClfFaction = "CLF";
    private const string WeYaFaction = "WeYa";
    private const string RoyalMarinesFaction = "RoyalMarines";
    private const string TseFaction = "TSE";
    private const string CivilianFaction = "Civilian";
    private const string BureauFaction = "Bureau";

    private readonly record struct RMCGhostTargetEntry(
        RMCGhostTargetSection Section,
        RMCGhostTargetButton Button,
        string GroupName,
        int GroupOrder,
        Color? GroupColor,
        bool ShowGroupHeader,
        int SortPriority,
        string SortName);

    private sealed class RMCGhostTargetGroupBuilder
    {
        public readonly int DisplayOrder;
        public readonly string Name;
        public readonly Color? HeaderColor;
        public readonly bool ShowHeader;
        public readonly List<RMCGhostTargetEntry> Entries = new();

        public RMCGhostTargetGroupBuilder(RMCGhostTargetEntry entry)
        {
            DisplayOrder = entry.GroupOrder;
            Name = entry.GroupName;
            HeaderColor = entry.GroupColor;
            ShowHeader = entry.ShowGroupHeader;
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        _ghostQuery = GetEntityQuery<GhostComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        SubscribeNetworkEvent<RMCGhostTargetsRequestEvent>(OnTargetsRequest);
        SubscribeNetworkEvent<RMCGhostTargetWarpRequestEvent>(OnWarpRequest);
        SubscribeNetworkEvent<RMCGhostTargetMostFollowedRequestEvent>(OnMostFollowedRequest);
    }

    private void OnTargetsRequest(RMCGhostTargetsRequestEvent msg, EntitySessionEventArgs args)
    {
        if (!TryGetSenderGhost(args, out var ghost))
        {
            Log.Warning($"User {args.SenderSession.Name} requested RMC ghost targets without being a ghost.");
            return;
        }

        RaiseNetworkEvent(new RMCGhostTargetsResponseEvent(GetSections(ghost)), args.SenderSession.Channel);
    }

    private void OnWarpRequest(RMCGhostTargetWarpRequestEvent msg, EntitySessionEventArgs args)
    {
        if (!TryGetSenderGhost(args, out var ghost))
        {
            Log.Warning($"User {args.SenderSession.Name} tried to RMC ghost warp without being a ghost.");
            return;
        }

        if (!TryGetEntity(msg.Target, out var target) ||
            target is not { } targetUid ||
            !Exists(targetUid) ||
            !IsValidTarget(targetUid))
        {
            Log.Warning($"User {args.SenderSession.Name} tried to RMC ghost warp to an invalid target: {msg.Target}");
            return;
        }

        WarpTo(ghost, targetUid);
    }

    private void OnMostFollowedRequest(RMCGhostTargetMostFollowedRequestEvent msg, EntitySessionEventArgs args)
    {
        if (!TryGetSenderGhost(args, out var ghost))
        {
            Log.Warning($"User {args.SenderSession.Name} tried to RMC ghost warp to most followed without being a ghost.");
            return;
        }

        if (_follower.GetMostGhostFollowed() is not { } target ||
            !Exists(target) ||
            !IsValidTarget(target))
        {
            return;
        }

        WarpTo(ghost, target);
    }

    private bool TryGetSenderGhost(EntitySessionEventArgs args, out EntityUid ghost)
    {
        ghost = default;
        if (args.SenderSession.AttachedEntity is not { Valid: true } attached ||
            !_ghostQuery.HasComp(attached))
        {
            return false;
        }

        ghost = attached;
        return true;
    }

    private List<RMCGhostTargetSectionModel> GetSections(EntityUid except)
    {
        var sections = new Dictionary<RMCGhostTargetSection, List<RMCGhostTargetEntry>>();
        var seen = new HashSet<EntityUid> { except };

        var mobs = EntityQueryEnumerator<MobStateComponent, TransformComponent, MetaDataComponent>();
        while (mobs.MoveNext(out var uid, out var mobState, out var xform, out var meta))
        {
            if (!seen.Add(uid) || TerminatingOrDeleted(uid))
                continue;

            AddEntry(sections, BuildMobTarget(uid, mobState, xform, meta.EntityName));
        }

        var ghosts = EntityQueryEnumerator<GhostComponent, TransformComponent, MetaDataComponent>();
        while (ghosts.MoveNext(out var uid, out _, out var xform, out var meta))
        {
            if (!seen.Add(uid) || TerminatingOrDeleted(uid))
                continue;

            AddEntry(sections, BuildGhostTarget(uid, xform, meta.EntityName));
        }

        var dropships = EntityQueryEnumerator<DropshipComponent, TransformComponent, MetaDataComponent>();
        while (dropships.MoveNext(out var uid, out _, out var xform, out var meta))
        {
            if (!seen.Add(uid) || TerminatingOrDeleted(uid))
                continue;

            AddEntry(sections, BuildSimpleTarget(uid, xform, meta.EntityName, RMCGhostTargetSection.Vehicles, RMCGhostTargetState.Location, true));
        }

        var warps = EntityQueryEnumerator<WarpPointComponent, TransformComponent, MetaDataComponent>();
        while (warps.MoveNext(out var uid, out var warp, out var xform, out var meta))
        {
            if (!seen.Add(uid) || TerminatingOrDeleted(uid))
                continue;

            AddEntry(sections, BuildSimpleTarget(uid, xform, warp.Location ?? meta.EntityName, RMCGhostTargetSection.Locations, RMCGhostTargetState.Location, true));
        }

        return BuildSectionModels(sections);
    }

    private static void AddEntry(
        Dictionary<RMCGhostTargetSection, List<RMCGhostTargetEntry>> sections,
        RMCGhostTargetEntry entry)
    {
        if (!sections.TryGetValue(entry.Section, out var entries))
        {
            entries = new List<RMCGhostTargetEntry>();
            sections[entry.Section] = entries;
        }

        entries.Add(entry);
    }

    private static List<RMCGhostTargetSectionModel> BuildSectionModels(
        Dictionary<RMCGhostTargetSection, List<RMCGhostTargetEntry>> grouped)
    {
        var sections = new List<RMCGhostTargetSectionModel>(grouped.Count);
        foreach (var group in grouped)
        {
            sections.Add(new RMCGhostTargetSectionModel(
                group.Key,
                RMCGhostTargetSectionData.GetDisplayOrder(group.Key),
                RMCGhostTargetSectionData.GetNameLocId(group.Key),
                RMCGhostTargetSectionData.GetHeaderColor(group.Key),
                BuildGroupModels(group.Key, group.Value)));
        }

        sections.Sort(CompareSections);
        return sections;
    }

    private static List<RMCGhostTargetGroupModel> BuildGroupModels(
        RMCGhostTargetSection section,
        List<RMCGhostTargetEntry> entries)
    {
        var groups = new List<RMCGhostTargetGroupBuilder>();
        foreach (var entry in entries)
        {
            RMCGhostTargetGroupBuilder? builder = null;
            foreach (var existing in groups)
            {
                if (existing.Name == entry.GroupName &&
                    existing.ShowHeader == entry.ShowGroupHeader)
                {
                    builder = existing;
                    break;
                }
            }

            if (builder == null)
            {
                builder = new RMCGhostTargetGroupBuilder(entry);
                groups.Add(builder);
            }

            builder.Entries.Add(entry);
        }

        groups.Sort(CompareGroups);

        var models = new List<RMCGhostTargetGroupModel>(groups.Count);
        foreach (var group in groups)
        {
            group.Entries.Sort(CompareEntries);

            var buttons = new List<RMCGhostTargetButton>(group.Entries.Count);
            foreach (var entry in group.Entries)
            {
                buttons.Add(entry.Button);
            }

            models.Add(new RMCGhostTargetGroupModel(
                group.DisplayOrder,
                group.Name,
                group.HeaderColor ?? RMCGhostTargetSectionData.GetGroupColor(section, group.Name),
                group.ShowHeader,
                buttons));
        }

        return models;
    }

    private static int CompareSections(RMCGhostTargetSectionModel left, RMCGhostTargetSectionModel right)
    {
        return left.DisplayOrder.CompareTo(right.DisplayOrder);
    }

    private static int CompareGroups(RMCGhostTargetGroupBuilder left, RMCGhostTargetGroupBuilder right)
    {
        var order = left.DisplayOrder.CompareTo(right.DisplayOrder);
        if (order != 0)
            return order;

        return CompareText(left.Name, right.Name);
    }

    private static int CompareEntries(RMCGhostTargetEntry left, RMCGhostTargetEntry right)
    {
        var priority = left.SortPriority.CompareTo(right.SortPriority);
        if (priority != 0)
            return priority;

        var orbiters = right.Button.Orbiters.CompareTo(left.Button.Orbiters);
        if (orbiters != 0)
            return orbiters;

        return CompareText(left.SortName, right.SortName);
    }

    private static int CompareText(string? left, string? right)
    {
        return string.Compare(left ?? string.Empty, right ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private RMCGhostTargetEntry BuildGhostTarget(EntityUid uid, TransformComponent xform, string name)
    {
        var section = RMCGhostTargetSection.Ghosts;
        var area = GetAreaName(uid);
        var label = name;
        return new RMCGhostTargetEntry(
            section,
            new RMCGhostTargetButton(
                GetNetEntity(uid),
                label,
                BuildSearchText(label, name, area, section),
                RMCGhostTargetState.Ghost,
                null,
                GetOrbiters(uid),
                IsGround(xform),
                IsShip(xform),
                false,
                null,
                null,
                null),
            string.Empty,
            0,
            null,
            false,
            0,
            name);
    }

    private RMCGhostTargetEntry BuildSimpleTarget(
        EntityUid uid,
        TransformComponent xform,
        string name,
        RMCGhostTargetSection section,
        RMCGhostTargetState state,
        bool isLocation)
    {
        var (icon, background, backgroundColor) = GetIcons(uid);
        var area = GetAreaName(uid);
        var label = name;
        return new RMCGhostTargetEntry(
            section,
            new RMCGhostTargetButton(
                GetNetEntity(uid),
                label,
                BuildSearchText(label, name, area, section),
                state,
                null,
                GetOrbiters(uid),
                IsGround(xform),
                IsShip(xform),
                isLocation,
                icon,
                background,
                backgroundColor),
            string.Empty,
            0,
            null,
            false,
            0,
            name);
    }

    private RMCGhostTargetEntry BuildMobTarget(EntityUid uid, MobStateComponent mobState, TransformComponent xform, string name)
    {
        var role = GetRole(uid);
        var squad = GetSquad(uid);
        var hive = GetHive(uid);
        var state = GetState(uid, mobState);
        var section = GetSection(uid, mobState, role);
        var health = GetHealthPercent(uid, mobState);
        var (icon, background, backgroundColor) = GetIcons(uid);
        var area = GetAreaName(uid);
        var label = name;
        var (groupName, groupColor, showGroupHeader) = GetGroup(section, squad, hive, role, area);

        return new RMCGhostTargetEntry(
            section,
            new RMCGhostTargetButton(
                GetNetEntity(uid),
                label,
                BuildSearchText(label, name, role, squad, hive, area, section),
                state,
                health,
                GetOrbiters(uid),
                IsGround(xform),
                IsShip(xform),
                false,
                icon,
                background,
                backgroundColor),
            groupName,
            RMCGhostTargetSectionData.GetGroupDisplayOrder(section, groupName),
            groupColor,
            showGroupHeader,
            GetSortPriority(section, role),
            name);
    }

    private static string BuildSearchText(
        string label,
        string? name,
        string? role,
        string? squad,
        string? hive,
        string? area,
        RMCGhostTargetSection section)
    {
        var parts = new List<string>
        {
            label,
            RMCGhostTargetSectionData.GetNameLocId(section),
            section.ToString(),
        };

        AddTextPart(parts, name);
        AddTextPart(parts, role);
        AddTextPart(parts, squad);
        AddTextPart(parts, hive);
        AddTextPart(parts, area);
        return string.Join("\n", parts);
    }

    private static string BuildSearchText(
        string label,
        string? name,
        string? area,
        RMCGhostTargetSection section)
    {
        return BuildSearchText(label, name, null, null, null, area, section);
    }

    private static void AddTextPart(List<string> parts, string? part)
    {
        if (!string.IsNullOrWhiteSpace(part))
            parts.Add(part);
    }

    private static (string Name, Color? Color, bool ShowHeader) GetGroup(
        RMCGhostTargetSection section,
        string? squad,
        string? hive,
        string? role,
        string? area)
    {
        var group = section switch
        {
            RMCGhostTargetSection.Marines => GetMarineGroup(squad, role),
            RMCGhostTargetSection.Xenos => GetHiveGroup(hive, area),
            RMCGhostTargetSection.Infected => GetHiveGroup(hive, area),
            RMCGhostTargetSection.Spp => GetSppGroup(role),
            RMCGhostTargetSection.WeYa => GetWeYaGroup(role),
            RMCGhostTargetSection.Tse => GetTseGroup(role),
            RMCGhostTargetSection.Predators => GetPredatorGroup(role),
            _ => string.Empty,
        };

        if (string.IsNullOrEmpty(group))
            return (string.Empty, null, false);

        return (group, RMCGhostTargetSectionData.GetGroupColor(section, group), true);
    }

    private static string GetMarineGroup(string? squad, string? role)
    {
        var text = $"{squad} {role}".ToLowerInvariant();
        if (text.Contains("mutineer"))
            return "MUTINY";

        if (text.Contains("loyalist"))
            return "LOYALIST";

        if (text.Contains("non-combat"))
            return "NON-COMBAT";

        if (text.Contains("alpha"))
            return "Alpha";

        if (text.Contains("bravo"))
            return "Bravo";

        if (text.Contains("charlie"))
            return "Charlie";

        if (text.Contains("delta"))
            return "Delta";

        if (text.Contains("foxtrot"))
            return "Foxtrot";

        if (text.Contains("echo"))
            return "Echo";

        if (text.Contains("cbrn"))
            return "CBRN";

        if (text.Contains("forecon"))
            return "FORECON";

        if (text.Contains("sof"))
            return "SOF";

        if (text.Contains("provost"))
            return "Provost";

        if (text.Contains("army"))
            return "Army";

        return "Other";
    }

    private static string GetHiveGroup(string? hive, string? area)
    {
        if (area?.Contains("Thunderdome", StringComparison.OrdinalIgnoreCase) == true)
            return "Thunderdome";

        var text = hive?.ToLowerInvariant() ?? string.Empty;
        if (text.Contains("normal") || text.Contains("prime"))
            return "Prime";

        if (text.Contains("corrupted"))
            return "Corrupted";

        if (text.Contains("forsaken"))
            return "Forsaken";

        if (text.Contains("mutated"))
            return "Mutated";

        if (text.Contains("yautja"))
            return "Yautja";

        return "Other";
    }

    private static string GetSppGroup(string? role)
    {
        var text = role?.ToLowerInvariant() ?? string.Empty;
        if (text.Contains("akula"))
            return "Akula";

        if (text.Contains("bizon"))
            return "Bizon";

        if (text.Contains("chayka"))
            return "Chayka";

        if (text.Contains("delfin"))
            return "Delfin";

        if (text.Contains("uppkdo"))
            return "UPPKdo";

        return "Other";
    }

    private static string GetWeYaGroup(string? role)
    {
        var text = role?.ToLowerInvariant() ?? string.Empty;
        if (text.Contains("whiteout"))
            return "Whiteout";

        if (text.Contains("w-y commando"))
            return "W-Y Commandos";

        if (text.Contains("pmc"))
            return "PMCs";

        if (text.Contains("security") || text.Contains("bodyguard"))
            return "Security Forces";

        return "Corporate";
    }

    private static string GetTseGroup(string? role)
    {
        var text = role?.ToLowerInvariant() ?? string.Empty;
        if (text.Contains("iasf"))
            return "Imperial Armed Space Force";

        if (text.Contains("rmc"))
            return "Royal Marines Commando";

        return "Other";
    }

    private static string GetPredatorGroup(string? role)
    {
        var text = role?.ToLowerInvariant() ?? string.Empty;
        if (text.Contains("young blood"))
            return "Young Bloods";

        if (text.Contains("military caste"))
            return "Military Caste";

        return "Hunters";
    }

    private static int GetSortPriority(RMCGhostTargetSection section, string? role)
    {
        if (section != RMCGhostTargetSection.Marines)
            return 100;

        var text = role?.ToLowerInvariant() ?? string.Empty;
        if (text.Contains("squad leader"))
            return 10;

        if (text.Contains("fireteam leader"))
            return 20;

        if (text.Contains("weapons specialist"))
            return 30;

        if (text.Contains("smartgunner"))
            return 40;

        if (text.Contains("combat technician"))
            return 50;

        if (text.Contains("hospital corpsman") || text.Contains("medic"))
            return 60;

        if (text.Contains("rifleman"))
            return 70;

        return 100;
    }

    private RMCGhostTargetSection GetSection(EntityUid uid, MobStateComponent mobState, string? role)
    {
        if (_mobState.IsDead(uid, mobState))
            return RMCGhostTargetSection.Dead;

        if (HasComp<XenoComponent>(uid))
            return RMCGhostTargetSection.Xenos;

        if (HasComp<VictimInfectedComponent>(uid))
            return RMCGhostTargetSection.Infected;

        if (HasComp<SynthComponent>(uid))
            return RMCGhostTargetSection.Synthetics;

        if (HasComp<RMCSurvivorComponent>(uid))
            return RMCGhostTargetSection.Survivors;

        if (TryComp<NpcFactionMemberComponent>(uid, out var factions))
        {
            if (IsFaction(uid, factions, SppFaction))
                return RMCGhostTargetSection.Spp;

            if (IsFaction(uid, factions, ClfFaction))
                return RMCGhostTargetSection.Clf;

            if (IsFaction(uid, factions, WeYaFaction))
                return RMCGhostTargetSection.WeYa;

            if (IsFaction(uid, factions, RoyalMarinesFaction) || IsFaction(uid, factions, TseFaction))
                return RMCGhostTargetSection.Tse;

            if (IsFaction(uid, factions, BureauFaction))
                return RMCGhostTargetSection.Marshals;
        }

        var roleLower = role?.ToLowerInvariant();
        if (roleLower != null)
        {
            if (roleLower.Contains("responder"))
                return RMCGhostTargetSection.Responders;

            if (roleLower.Contains("ert"))
                return RMCGhostTargetSection.ErtMembers;

            if (roleLower.Contains("hyperdyne"))
                return RMCGhostTargetSection.Hyperdyne;

            if (roleLower.Contains("freelancer"))
                return RMCGhostTargetSection.Freelancers;

            if (roleLower.Contains("mercenary") || roleLower.Contains("pmc"))
                return RMCGhostTargetSection.Mercenaries;

            if (roleLower.Contains("contractor"))
                return RMCGhostTargetSection.Contractors;

            if (roleLower.Contains("predator") || roleLower.Contains("yautja"))
                return RMCGhostTargetSection.Predators;

            if (roleLower.Contains("hunted"))
                return RMCGhostTargetSection.Hunted;

            if (roleLower.Contains("dutch"))
                return RMCGhostTargetSection.Dutch;

            if (roleLower.Contains("marshal") || roleLower.Contains("bureau"))
                return RMCGhostTargetSection.Marshals;
        }

        if (TryComp<NpcFactionMemberComponent>(uid, out var laterFactions))
        {
            if (IsFaction(uid, laterFactions, UnmcFaction) || HasComp<MarineComponent>(uid))
                return RMCGhostTargetSection.Marines;

            if (IsFaction(uid, laterFactions, CivilianFaction))
                return RMCGhostTargetSection.Humans;
        }

        if (HasComp<MarineComponent>(uid))
            return RMCGhostTargetSection.Marines;

        if (!HasMind(uid))
            return RMCGhostTargetSection.Npcs;

        return RMCGhostTargetSection.Humans;
    }

    private string? GetRole(EntityUid uid)
    {
        if (TryComp<XenoComponent>(uid, out var xeno) &&
            _prototypes.TryIndex<JobPrototype>(xeno.Role, out var xenoJob))
        {
            return xenoJob.LocalizedName;
        }

        if (TryComp<OriginalRoleComponent>(uid, out var originalRole) &&
            originalRole.Job is { } jobId &&
            _prototypes.TryIndex<JobPrototype>(jobId, out var job))
        {
            return job.LocalizedName;
        }

        if (TryComp<MindContainerComponent>(uid, out var mind) &&
            mind.Mind is { } mindId)
        {
            return _jobs.MindTryGetJobName(mindId);
        }

        return null;
    }

    private string? GetSquad(EntityUid uid)
    {
        if (!TryComp<SquadMemberComponent>(uid, out var member) ||
            member.Squad is not { } squad ||
            !Exists(squad))
        {
            return null;
        }

        var name = MetaData(squad).EntityName;
        if (HasComp<MutineerComponent>(uid))
            return Loc.GetString("rmc-ghost-target-squad-mutineer", ("squad", name));

        return name;
    }

    private string? GetHive(EntityUid uid)
    {
        EntityUid? hiveUid = null;
        if (TryComp<HiveMemberComponent>(uid, out var hiveMember))
            hiveUid = hiveMember.Hive;
        else if (TryComp<VictimInfectedComponent>(uid, out var infected))
            hiveUid = infected.Hive;

        if (hiveUid is not { } hive ||
            !Exists(hive))
        {
            return null;
        }

        return MetaData(hive).EntityName;
    }

    private RMCGhostTargetState GetState(EntityUid uid, MobStateComponent mobState)
    {
        if (_mobState.IsDead(uid, mobState))
            return RMCGhostTargetState.Dead;

        if (_mobState.IsCritical(uid, mobState))
            return RMCGhostTargetState.Critical;

        if (_mobState.IsAlive(uid, mobState))
            return RMCGhostTargetState.Alive;

        return RMCGhostTargetState.Unknown;
    }

    private int? GetHealthPercent(EntityUid uid, MobStateComponent mobState)
    {
        if (!_mobState.IsAlive(uid, mobState) && !_mobState.IsCritical(uid, mobState))
            return null;

        if (!TryComp<DamageableComponent>(uid, out var damageable) ||
            !_mobThreshold.TryGetDeadThreshold(uid, out var deadThreshold) ||
            deadThreshold is not { } threshold ||
            threshold <= FixedPoint2.Zero)
        {
            return null;
        }

        var health = 100 - MathF.Round(damageable.TotalDamage.Float() / threshold.Float() * 100);
        return Math.Clamp((int) health, 0, 100);
    }

    private (SpriteSpecifier.Rsi? Icon, SpriteSpecifier.Rsi? Background, Color? BackgroundColor) GetIcons(EntityUid uid)
    {
        SpriteSpecifier.Rsi? icon = null;
        SpriteSpecifier.Rsi? background = null;
        Color? backgroundColor = null;

        if (TryComp<TacticalMapIconComponent>(uid, out var tactical))
        {
            icon = tactical.Icon;
            background = tactical.Background;
        }

        if (TryComp<MarineComponent>(uid, out var marine))
            icon ??= ToRsi(marine.Icon);

        if (TryComp<SquadMemberComponent>(uid, out var member))
        {
            background ??= ToRsi(member.Background);
            backgroundColor ??= member.AccessibleBackgroundColor ?? member.BackgroundColor;
        }

        if (TryComp<OriginalRoleComponent>(uid, out var originalRole) &&
            originalRole.Job is { } jobId &&
            _prototypes.TryIndex<JobPrototype>(jobId, out var job) &&
            _prototypes.TryIndex<JobIconPrototype>(job.Icon, out var jobIcon))
        {
            icon ??= ToRsi(jobIcon.Icon);
        }

        if (TryComp<XenoComponent>(uid, out var xeno) &&
            _prototypes.TryIndex<JobPrototype>(xeno.Role, out var xenoJob) &&
            _prototypes.TryIndex<JobIconPrototype>(xenoJob.Icon, out var xenoIcon))
        {
            icon ??= ToRsi(xenoIcon.Icon);
        }

        return (icon, background, backgroundColor);
    }

    private static SpriteSpecifier.Rsi? ToRsi(SpriteSpecifier? specifier)
    {
        return specifier as SpriteSpecifier.Rsi;
    }

    private string? GetAreaName(EntityUid uid)
    {
        return Transform(uid).MapUid != null
            ? _area.GetAreaName(uid)
            : null;
    }

    private bool IsFaction(EntityUid uid, NpcFactionMemberComponent component, string faction)
    {
        return _npcFaction.IsMember((uid, component), faction);
    }

    private int GetOrbiters(EntityUid uid)
    {
        return TryComp<FollowedComponent>(uid, out var followed)
            ? followed.Following.Count(follower => _ghostQuery.HasComp(follower))
            : 0;
    }

    private bool HasMind(EntityUid uid)
    {
        return TryComp<MindContainerComponent>(uid, out var mind) && mind.HasMind;
    }

    private bool IsGround(TransformComponent xform)
    {
        return !IsShip(xform);
    }

    private bool IsShip(TransformComponent xform)
    {
        return xform.MapUid is { } map && HasComp<AlmayerComponent>(map);
    }

    private bool IsValidTarget(EntityUid target)
    {
        return HasComp<MobStateComponent>(target) ||
               HasComp<GhostComponent>(target) ||
               HasComp<DropshipComponent>(target) ||
               HasComp<WarpPointComponent>(target);
    }

    private void WarpTo(EntityUid ghost, EntityUid target)
    {
        _adminLog.Add(LogType.GhostWarp, $"{ToPrettyString(ghost)} RMC ghost warped to {ToPrettyString(target)}");

        if (HasComp<MobStateComponent>(target) ||
            HasComp<GhostComponent>(target) ||
            HasComp<DropshipComponent>(target))
        {
            _follower.StartFollowingEntity(ghost, target);
            return;
        }

        var xform = Transform(ghost);
        _transform.SetCoordinates(ghost, xform, Transform(target).Coordinates);
        _transform.AttachToGridOrMap(ghost, xform);

        if (_physicsQuery.TryComp(ghost, out var physics))
            _physics.SetLinearVelocity(ghost, Vector2.Zero, body: physics);
    }
}

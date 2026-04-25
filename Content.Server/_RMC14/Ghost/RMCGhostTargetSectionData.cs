using Content.Shared._RMC14.Ghost;
using Robust.Shared.Maths;

namespace Content.Server._RMC14.Ghost;

public static class RMCGhostTargetSectionData
{
    public static int GetDisplayOrder(RMCGhostTargetSection section)
    {
        return section switch
        {
            RMCGhostTargetSection.Marines => 10,
            RMCGhostTargetSection.Xenos => 20,
            RMCGhostTargetSection.Infected => 30,
            RMCGhostTargetSection.Survivors => 40,
            RMCGhostTargetSection.Synthetics => 50,
            RMCGhostTargetSection.Responders => 60,
            RMCGhostTargetSection.ErtMembers => 70,
            RMCGhostTargetSection.Vehicles => 80,
            RMCGhostTargetSection.Dead => 90,
            RMCGhostTargetSection.Ghosts => 100,
            RMCGhostTargetSection.Locations => 110,
            RMCGhostTargetSection.Spp => 200,
            RMCGhostTargetSection.Clf => 210,
            RMCGhostTargetSection.WeYa => 220,
            RMCGhostTargetSection.Hyperdyne => 230,
            RMCGhostTargetSection.Tse => 240,
            RMCGhostTargetSection.Freelancers => 250,
            RMCGhostTargetSection.Mercenaries => 260,
            RMCGhostTargetSection.Contractors => 270,
            RMCGhostTargetSection.Predators => 280,
            RMCGhostTargetSection.Hunted => 290,
            RMCGhostTargetSection.Dutch => 300,
            RMCGhostTargetSection.Marshals => 310,
            RMCGhostTargetSection.Escaped => 320,
            RMCGhostTargetSection.Thunderdome => 330,
            RMCGhostTargetSection.Humans => 400,
            RMCGhostTargetSection.Animals => 500,
            RMCGhostTargetSection.Npcs => 600,
            RMCGhostTargetSection.Misc => 700,
            _ => 700,
        };
    }

    public static string GetNameLocId(RMCGhostTargetSection section)
    {
        return section switch
        {
            RMCGhostTargetSection.Marines => "rmc-ghost-target-section-marines",
            RMCGhostTargetSection.Humans => "rmc-ghost-target-section-humans",
            RMCGhostTargetSection.Xenos => "rmc-ghost-target-section-xenos",
            RMCGhostTargetSection.Survivors => "rmc-ghost-target-section-survivors",
            RMCGhostTargetSection.Infected => "rmc-ghost-target-section-infected",
            RMCGhostTargetSection.ErtMembers => "rmc-ghost-target-section-ert-members",
            RMCGhostTargetSection.Synthetics => "rmc-ghost-target-section-synthetics",
            RMCGhostTargetSection.Spp => "rmc-ghost-target-section-spp",
            RMCGhostTargetSection.Clf => "rmc-ghost-target-section-clf",
            RMCGhostTargetSection.WeYa => "rmc-ghost-target-section-weya",
            RMCGhostTargetSection.Hyperdyne => "rmc-ghost-target-section-hyperdyne",
            RMCGhostTargetSection.Tse => "rmc-ghost-target-section-tse",
            RMCGhostTargetSection.Freelancers => "rmc-ghost-target-section-freelancers",
            RMCGhostTargetSection.Mercenaries => "rmc-ghost-target-section-mercenaries",
            RMCGhostTargetSection.Contractors => "rmc-ghost-target-section-contractors",
            RMCGhostTargetSection.Hunted => "rmc-ghost-target-section-hunted",
            RMCGhostTargetSection.Dutch => "rmc-ghost-target-section-dutch",
            RMCGhostTargetSection.Marshals => "rmc-ghost-target-section-marshals",
            RMCGhostTargetSection.Responders => "rmc-ghost-target-section-responders",
            RMCGhostTargetSection.Predators => "rmc-ghost-target-section-predators",
            RMCGhostTargetSection.Escaped => "rmc-ghost-target-section-escaped",
            RMCGhostTargetSection.Thunderdome => "rmc-ghost-target-section-thunderdome",
            RMCGhostTargetSection.Vehicles => "rmc-ghost-target-section-vehicles",
            RMCGhostTargetSection.Animals => "rmc-ghost-target-section-animals",
            RMCGhostTargetSection.Dead => "rmc-ghost-target-section-dead",
            RMCGhostTargetSection.Ghosts => "rmc-ghost-target-section-ghosts",
            RMCGhostTargetSection.Misc => "rmc-ghost-target-section-misc",
            RMCGhostTargetSection.Npcs => "rmc-ghost-target-section-npcs",
            RMCGhostTargetSection.Locations => "rmc-ghost-target-section-locations",
            _ => "rmc-ghost-target-section-misc",
        };
    }

    public static Color? GetHeaderColor(RMCGhostTargetSection section)
    {
        return section switch
        {
            RMCGhostTargetSection.Marines => Color.FromHex("#2878b8"),
            RMCGhostTargetSection.Humans => Color.FromHex("#1f8a8a"),
            RMCGhostTargetSection.Xenos => Color.FromHex("#4c315c"),
            RMCGhostTargetSection.Survivors => Color.FromHex("#4b9220"),
            RMCGhostTargetSection.Infected => Color.FromHex("#c42026"),
            RMCGhostTargetSection.ErtMembers => Color.FromHex("#b5892d"),
            RMCGhostTargetSection.Synthetics => Color.FromHex("#9b9b9b"),
            RMCGhostTargetSection.Spp => Color.FromHex("#4b9220"),
            RMCGhostTargetSection.Clf => Color.FromHex("#1f8a8a"),
            RMCGhostTargetSection.WeYa => Color.FromHex("#d8d8d8"),
            RMCGhostTargetSection.Hyperdyne => Color.FromHex("#c56f1f"),
            RMCGhostTargetSection.Tse => Color.FromHex("#c42026"),
            RMCGhostTargetSection.Freelancers => Color.FromHex("#c56f1f"),
            RMCGhostTargetSection.Mercenaries => Color.FromHex("#777777"),
            RMCGhostTargetSection.Contractors => Color.FromHex("#9b9b9b"),
            RMCGhostTargetSection.Hunted => Color.FromHex("#c42026"),
            RMCGhostTargetSection.Dutch => Color.FromHex("#4b9220"),
            RMCGhostTargetSection.Marshals => Color.FromHex("#255491"),
            RMCGhostTargetSection.Responders => Color.FromHex("#a83aa8"),
            RMCGhostTargetSection.Predators => Color.FromHex("#4b9220"),
            RMCGhostTargetSection.Escaped => Color.FromHex("#708c2b"),
            RMCGhostTargetSection.Thunderdome => Color.FromHex("#c56f1f"),
            _ => null,
        };
    }

    public static int GetGroupDisplayOrder(RMCGhostTargetSection section, string? group)
    {
        return NormalizeGroup(group) switch
        {
            "mutiny" => 1,
            "loyalist" => 2,
            "non-combat" => 3,
            "alpha" => 10,
            "prime" => 10,
            "pmcs" => 10,
            "hunters" => 10,
            "akula" => 10,
            "bravo" => 20,
            "corrupted" => 20,
            "security forces" => 20,
            "young bloods" => 20,
            "bizon" => 20,
            "charlie" => 30,
            "forsaken" => 30,
            "corporate" => 30,
            "military caste" => 30,
            "chayka" => 30,
            "delta" => 40,
            "mutated" => 40,
            "w-y commandos" => 40,
            "delfin" => 40,
            "foxtrot" => 50,
            "thunderdome" => 50,
            "whiteout" => 50,
            "uppkdo" => 50,
            "echo" => 60,
            "yautja" => 60,
            "cbrn" => 70,
            "forecon" => 80,
            "sof" => 90,
            "provost" => 110,
            "army" => 120,
            "other" => 100,
            _ => 100,
        };
    }

    public static Color GetGroupColor(RMCGhostTargetSection section, string? group)
    {
        var normalized = NormalizeGroup(group);
        return normalized switch
        {
            "alpha" or "mutiny" or "sof" or "provost" or "red" or "infected" or "commando" or "military caste" or "whiteout" => Color.FromHex("#c42026"),
            "bravo" or "yellow" or "bizon" => Color.FromHex("#d9c300"),
            "charlie" or "purple" or "prime" or "xeno" => Color.FromHex("#9531b6"),
            "delta" or "blue" or "loyalist" or "delfin" => Color.FromHex("#2878b8"),
            "foxtrot" or "young bloods" or "brown" => Color.FromHex("#8a5a22"),
            "echo" or "teal" => Color.FromHex("#1f8a8a"),
            "non-combat" or "green" or "corrupted" or "forecon" or "hunters" or "army" => Color.FromHex("#4b9220"),
            "cbrn" or "dark-blue" or "akula" => Color.FromHex("#255491"),
            "mutated" or "pink" => Color.FromHex("#a83aa8"),
            "security forces" or "orange" => Color.FromHex("#c56f1f"),
            "pmcs" or "w-y commandos" or "white" => Color.FromHex("#d8d8d8"),
            "other" or "grey" or "light-grey" => Color.FromHex("#777777"),
            _ => GetHeaderColor(section) ?? Color.FromHex("#777777"),
        };
    }

    private static string NormalizeGroup(string? group)
    {
        return group?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}

using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// Where a scenario places one monster on an encounter's battlefield.
///
/// The monster's own stats -- hit points, armor class, weapons, and the rest
/// -- live once in the ruleset (Core.Definitions.MonsterDefinition) and are
/// referenced here by ID, the same way a CombatantWeaponDefinition used to
/// reference a ruleset WeaponDefinition. This type carries only what is
/// specific to this placement in this encounter.
internal sealed record EncounterCombatantDefinition
{
    internal required string CombatantId { get; init; }

    internal required string MonsterId { get; init; }

    internal required string SideId { get; init; }

    internal required GridPosition StartingPosition { get; init; }
}

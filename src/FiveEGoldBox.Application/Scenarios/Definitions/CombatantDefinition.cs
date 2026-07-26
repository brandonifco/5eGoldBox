using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// A scenario-authored combatant — the opposition, rather than anyone the
/// campaign supplied.
internal sealed record CombatantDefinition
{
    internal required string CombatantId { get; init; }

    internal required string DisplayName { get; init; }

    internal required string SideId { get; init; }

    internal required int MaximumHitPoints { get; init; }

    internal required int ArmorClass { get; init; }

    internal required int MovementSpeedFeet { get; init; }

    internal required GridPosition StartingPosition { get; init; }

    internal required CombatantZeroHitPointPolicy ZeroHitPointPolicy { get; init; }

    /// Every ability's modifier. All six are required, because saving throws
    /// need all six and a missing one would only surface when something asked
    /// this combatant to make that save.
    internal required IReadOnlyList<CombatantAbilityModifier> AbilityModifiers { get; init; }

    internal required int ProficiencyBonus { get; init; }

    /// Whether this combatant is proficient with the weapons it carries.
    /// Stat blocks are proficient with their own gear as a rule, so this
    /// defaults accordingly.
    internal bool IsProficientWithWeapons { get; init; } = true;

    internal required IReadOnlyList<CombatantWeaponDefinition> Weapons { get; init; }
}

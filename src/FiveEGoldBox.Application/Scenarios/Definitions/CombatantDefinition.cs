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

    internal required IReadOnlyList<CombatantWeaponDefinition> Weapons { get; init; }
}

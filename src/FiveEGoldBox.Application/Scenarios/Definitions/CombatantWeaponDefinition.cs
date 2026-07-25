namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// A weapon reference plus the ammunition the scenario grants with it. The
/// weapon's own mechanics live in the ruleset, not here.
internal sealed record CombatantWeaponDefinition
{
    internal required string WeaponId { get; init; }

    /// Null for weapons that do not consume ammunition.
    internal string? AmmunitionItemId { get; init; }

    internal int? AmmunitionQuantity { get; init; }
}

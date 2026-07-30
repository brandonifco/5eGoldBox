namespace FiveEGoldBox.Core.Definitions;

/// A weapon reference plus the ammunition the monster carries with it. The
/// weapon's own mechanics live elsewhere in the ruleset, not here.
public sealed record MonsterWeaponDefinition
{
    public required string WeaponId { get; init; }

    /// Null for weapons that do not consume ammunition.
    public string? AmmunitionItemId { get; init; }

    public int? AmmunitionQuantity { get; init; }
}

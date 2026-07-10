using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Characters;

public sealed record WeaponAttack
{
    public required string WeaponId { get; init; }

    public required string WeaponName { get; init; }

    public required WeaponCategory Category { get; init; }

    public required WeaponAttackKind AttackKind { get; init; }

    public required Ability AttackAbility { get; init; }

    public required int AbilityModifier { get; init; }

    public required bool IsProficient { get; init; }

    public required int ProficiencyBonus { get; init; }

    public required int AttackBonus { get; init; }

    public required DamageDice Damage { get; init; }

    public DamageDice? VersatileDamage { get; init; }

    public required string DamageType { get; init; }

    public required int DamageBonus { get; init; }

    public IReadOnlyList<string> Properties { get; init; }
        = Array.Empty<string>();

    public int? ReachFeet { get; init; }

    public int? NormalRangeFeet { get; init; }

    public int? LongRangeFeet { get; init; }

    public string? AmmunitionItemId { get; init; }

    public int? AmmunitionQuantityAvailable { get; init; }
}
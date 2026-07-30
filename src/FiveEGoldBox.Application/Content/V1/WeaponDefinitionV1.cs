namespace FiveEGoldBox.Application.Content.V1;

internal sealed record WeaponDefinitionV1
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required WeaponCategoryV1 Category { get; init; }

    public required WeaponAttackKindV1 AttackKind { get; init; }

    public required DamageDiceV1 Damage { get; init; }

    public DamageDiceV1? VersatileDamage { get; init; }

    public required string DamageType { get; init; }

    public IReadOnlyList<string> Properties { get; init; }
        = Array.Empty<string>();

    public int? ReachFeet { get; init; }

    public int? NormalRangeFeet { get; init; }

    public int? LongRangeFeet { get; init; }

    public string? AmmunitionItemId { get; init; }

    public decimal WeightPounds { get; init; }

    public int? CostInCopperPieces { get; init; }
}

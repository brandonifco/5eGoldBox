using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Definitions;

public sealed record WeaponDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required WeaponCategory Category { get; init; }

    public required WeaponAttackKind AttackKind { get; init; }

    public required DamageDice Damage { get; init; }

    public required string DamageType { get; init; }

    public IReadOnlyList<string> Properties { get; init; }
        = Array.Empty<string>();

    public int? NormalRangeFeet { get; init; }

    public int? LongRangeFeet { get; init; }

    public decimal WeightPounds { get; init; }
    
    public int? CostInCopperPieces { get; init; }

}
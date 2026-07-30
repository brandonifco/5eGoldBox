namespace FiveEGoldBox.Application.Content.V1;

internal sealed record MonsterDefinitionV1
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required int MaximumHitPoints { get; init; }

    public required int ArmorClass { get; init; }

    public required int MovementSpeedFeet { get; init; }

    public required CombatantZeroHitPointPolicyV1 ZeroHitPointPolicy { get; init; }

    public required IReadOnlyList<MonsterAbilityModifierV1> AbilityModifiers { get; init; }

    public required int ProficiencyBonus { get; init; }

    public bool IsProficientWithWeapons { get; init; } = true;

    public required IReadOnlyList<MonsterWeaponDefinitionV1> Weapons { get; init; }
}

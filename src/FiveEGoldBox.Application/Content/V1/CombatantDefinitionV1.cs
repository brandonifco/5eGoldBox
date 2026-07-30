namespace FiveEGoldBox.Application.Content.V1;

internal sealed record CombatantDefinitionV1
{
    public required string CombatantId { get; init; }

    public required string DisplayName { get; init; }

    public required string SideId { get; init; }

    public required int MaximumHitPoints { get; init; }

    public required int ArmorClass { get; init; }

    public required int MovementSpeedFeet { get; init; }

    public required GridPositionV1 StartingPosition { get; init; }

    public required CombatantZeroHitPointPolicyV1 ZeroHitPointPolicy { get; init; }

    public required IReadOnlyList<CombatantAbilityModifierV1> AbilityModifiers { get; init; }

    public required int ProficiencyBonus { get; init; }

    public bool IsProficientWithWeapons { get; init; } = true;

    public required IReadOnlyList<CombatantWeaponDefinitionV1> Weapons { get; init; }
}

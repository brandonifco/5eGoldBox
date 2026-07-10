using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Characters;

public sealed record CharacterSnapshot
{
    public required string Name { get; init; }

    public required int Level { get; init; }

    public string? RaceId { get; init; }

    public string? RaceName { get; init; }

    public required CharacterSize Size { get; init; }

    public string? SubraceId { get; init; }

    public string? SubraceName { get; init; }

    public string? ClassId { get; init; }

    public string? ClassName { get; init; }

    public string? BackgroundId { get; init; }

    public string? BackgroundName { get; init; }

    public string? BackgroundFeatureId { get; init; }
    public DieType? HitDie { get; init; }

    public int? HitDiceCount { get; init; }

    public int? MaxHitPoints { get; init; }

    public int? SpeedFeet { get; init; }

    public IReadOnlyList<CharacterMovementSpeed> MovementSpeeds { get; init; }
        = Array.Empty<CharacterMovementSpeed>();

    public IReadOnlyList<CharacterSense> Senses { get; init; }
        = Array.Empty<CharacterSense>();

    public required int CarryingCapacityPounds { get; init; }

    public required int PushDragLiftPounds { get; init; }

    public required decimal EquippedWeightPounds { get; init; }

    public required decimal InventoryWeightPounds { get; init; }

    public required decimal CurrencyWeightPounds { get; init; }

    public required decimal TotalCarriedWeightPounds { get; init; }

    public required bool IsOverCarryingCapacity { get; init; }

    public IReadOnlyList<InventoryItemSnapshot> InventoryItems { get; init; }
        = Array.Empty<InventoryItemSnapshot>();

    public CurrencyAmount Currency { get; init; } = new();

    public required int CurrencyValueInCopperPieces { get; init; }

    public required decimal CurrencyValueInGoldPieces { get; init; }

    public string? EquippedArmorId { get; init; }

    public string? EquippedArmorName { get; init; }

    public string? EquippedShieldId { get; init; }

    public string? EquippedShieldName { get; init; }

    public int? ArmorClass { get; init; }

    public required int InitiativeBonus { get; init; }

    public required int PassivePerception { get; init; }

    public bool HasStealthDisadvantage { get; init; }

    public IReadOnlyList<string> EquippedWeaponIds { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> EquippedWeaponNames { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<WeaponAttack> WeaponAttacks { get; init; }
        = Array.Empty<WeaponAttack>();

    public required int ProficiencyBonus { get; init; }

    public required IReadOnlyDictionary<Ability, int> AbilityScores { get; init; }

    public required IReadOnlyDictionary<Ability, int> AbilityModifiers { get; init; }

    public IReadOnlyList<SavingThrowBonus> SavingThrowBonuses { get; init; }
        = Array.Empty<SavingThrowBonus>();

    public IReadOnlyList<Ability> SavingThrowProficiencies { get; init; }
        = Array.Empty<Ability>();

    public IReadOnlyList<string> ArmorProficiencies { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> WeaponProficiencies { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> ToolProficiencies { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> SkillProficiencies { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<SkillBonus> SkillBonuses { get; init; }
        = Array.Empty<SkillBonus>();

    public IReadOnlyList<string> Languages { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> Traits { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> ClassFeatures { get; init; }
        = Array.Empty<string>();
}
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverInventoryCarryingCapacityValidationTests
{
    [Fact]
    public void Validate_WithInventoryWeightBelowCarryingCapacity_DoesNotReturnCarryingCapacityWarning()
    {
        RulesetDefinition ruleset = CreateInventoryRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.test",
            InventoryItems =
            [
                new InventoryItemDraft
                {
                    ItemId = "equipment.light_bundle",
                    Quantity = 1
                }
            ]
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code == "character.carrying_capacity.exceeded");
    }

    [Fact]
    public void Validate_WithInventoryWeightEqualToCarryingCapacity_DoesNotReturnCarryingCapacityWarning()
    {
        RulesetDefinition ruleset = CreateInventoryRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.test",
            InventoryItems =
            [
                new InventoryItemDraft
                {
                    ItemId = "equipment.exact_bundle",
                    Quantity = 1
                }
            ]
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code == "character.carrying_capacity.exceeded");
    }

    [Fact]
    public void Validate_WithInventoryWeightAboveCarryingCapacity_ReturnsWarningButRemainsValid()
    {
        RulesetDefinition ruleset = CreateInventoryRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.test",
            InventoryItems =
            [
                new InventoryItemDraft
                {
                    ItemId = "equipment.exact_bundle",
                    Quantity = 1
                },
                new InventoryItemDraft
                {
                    ItemId = "equipment.small_bundle",
                    Quantity = 1
                }
            ]
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.True(result.IsValid);

        ValidationIssue issue = Assert.Single(
            result.Issues,
            issue => issue.Code == "character.carrying_capacity.exceeded");

        Assert.Equal(ValidationSeverity.Warning, issue.Severity);
    }

    [Fact]
    public void Validate_WithEquippedWeightAndInventoryWeightAboveCarryingCapacity_ReturnsWarningButRemainsValid()
    {
        RulesetDefinition ruleset = CreateEquipmentAndInventoryRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.test",
            ClassId = "class.test",
            EquippedArmorId = "armor.heavy_load",
            InventoryItems =
            [
                new InventoryItemDraft
                {
                    ItemId = "equipment.small_bundle",
                    Quantity = 2
                }
            ]
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.True(result.IsValid);

        ValidationIssue issue = Assert.Single(
            result.Issues,
            issue => issue.Code == "character.carrying_capacity.exceeded");

        Assert.Equal(ValidationSeverity.Warning, issue.Severity);
    }

    [Fact]
    public void Validate_WithRacialStrengthIncrease_UsesFinalStrengthForInventoryWeightWarning()
    {
        RulesetDefinition ruleset = CreateStrongRaceInventoryRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.strong",
            InventoryItems =
            [
                new InventoryItemDraft
                {
                    ItemId = "equipment.exact_bundle",
                    Quantity = 1
                }
            ]
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code == "character.carrying_capacity.exceeded");
    }

    private static CharacterDraft CreateDraftWithStrength(int strength)
    {
        return new CharacterDraft
        {
            Name = "Inventory Carrying Character",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.Manual,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = strength,
                [Ability.Dexterity] = 10,
                [Ability.Constitution] = 10,
                [Ability.Intelligence] = 10,
                [Ability.Wisdom] = 10,
                [Ability.Charisma] = 10
            }
        };
    }

    private static RulesetDefinition CreateInventoryRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.inventory_carrying_capacity_validation",
            Name = "Inventory Carrying Capacity Validation Ruleset",
            Races =
            [
                CreateTestRace()
            ],
            EquipmentItems =
            [
                CreateLightBundle(),
                CreateExactBundle(),
                CreateSmallBundle()
            ]
        };
    }

    private static RulesetDefinition CreateEquipmentAndInventoryRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.equipped_and_inventory_carrying_capacity_validation",
            Name = "Equipped and Inventory Carrying Capacity Validation Ruleset",
            Races =
            [
                CreateTestRace()
            ],
            Classes =
            [
                new ClassDefinition
                {
                    Id = "class.test",
                    Name = "Test Class",
                    HitDie = DieType.D8,
                    ArmorProficiencies =
                    [
                        "armor.heavy"
                    ]
                }
            ],
            Armors =
            [
                new ArmorDefinition
                {
                    Id = "armor.heavy_load",
                    Name = "Heavy Load",
                    Category = ArmorCategory.Heavy,
                    BaseArmorClass = 16,
                    WeightPounds = 115m
                }
            ],
            EquipmentItems =
            [
                CreateSmallBundle()
            ]
        };
    }

    private static RulesetDefinition CreateStrongRaceInventoryRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.strong_race_inventory_carrying_capacity_validation",
            Name = "Strong Race Inventory Carrying Capacity Validation Ruleset",
            Races =
            [
                new RaceDefinition
                {
                    Id = "race.strong",
                    Name = "Strong Race",
                    BaseSpeedFeet = 30,
                    AbilityScoreIncreases =
                    [
                        new AbilityScoreIncrease(Ability.Strength, 1)
                    ]
                }
            ],
            EquipmentItems =
            [
                CreateExactBundle()
            ]
        };
    }

    private static RaceDefinition CreateTestRace()
    {
        return new RaceDefinition
        {
            Id = "race.test",
            Name = "Test Race",
            BaseSpeedFeet = 30
        };
    }

    private static EquipmentItemDefinition CreateLightBundle()
    {
        return new EquipmentItemDefinition
        {
            Id = "equipment.light_bundle",
            Name = "Light Bundle",
            WeightPounds = 60m
        };
    }

    private static EquipmentItemDefinition CreateExactBundle()
    {
        return new EquipmentItemDefinition
        {
            Id = "equipment.exact_bundle",
            Name = "Exact Bundle",
            WeightPounds = 120m
        };
    }

    private static EquipmentItemDefinition CreateSmallBundle()
    {
        return new EquipmentItemDefinition
        {
            Id = "equipment.small_bundle",
            Name = "Small Bundle",
            WeightPounds = 5m
        };
    }
}
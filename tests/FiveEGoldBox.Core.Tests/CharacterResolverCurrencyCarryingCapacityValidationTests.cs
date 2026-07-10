using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverCurrencyCarryingCapacityValidationTests
{
    [Fact]
    public void Validate_WithCurrencyWeightBelowCarryingCapacity_DoesNotReturnCarryingCapacityWarning()
    {
        RulesetDefinition ruleset = CreateCurrencyRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.test",
            Currency = new CurrencyAmount
            {
                GoldPieces = 5000
            }
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code == "character.carrying_capacity.exceeded");
    }

    [Fact]
    public void Validate_WithCurrencyWeightEqualToCarryingCapacity_DoesNotReturnCarryingCapacityWarning()
    {
        RulesetDefinition ruleset = CreateCurrencyRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.test",
            Currency = new CurrencyAmount
            {
                GoldPieces = 6000
            }
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code == "character.carrying_capacity.exceeded");
    }

    [Fact]
    public void Validate_WithCurrencyWeightAboveCarryingCapacity_ReturnsWarningButRemainsValid()
    {
        RulesetDefinition ruleset = CreateCurrencyRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.test",
            Currency = new CurrencyAmount
            {
                GoldPieces = 6050
            }
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
    public void Validate_WithEquippedInventoryAndCurrencyWeightAboveCarryingCapacity_ReturnsWarningButRemainsValid()
    {
        RulesetDefinition ruleset = CreateEquipmentInventoryAndCurrencyRuleset();

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
                    Quantity = 1
                }
            ],
            Currency = new CurrencyAmount
            {
                GoldPieces = 100
            }
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
    public void Validate_WithNegativeCurrency_DoesNotReturnCarryingCapacityWarning()
    {
        RulesetDefinition ruleset = CreateCurrencyRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.test",
            Currency = new CurrencyAmount
            {
                GoldPieces = -1
            }
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.currency.amount.invalid");

        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code == "character.carrying_capacity.exceeded");
    }

    [Fact]
    public void Validate_WithRacialStrengthIncrease_UsesFinalStrengthForCurrencyWeightWarning()
    {
        RulesetDefinition ruleset = CreateStrongRaceCurrencyRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.strong",
            Currency = new CurrencyAmount
            {
                GoldPieces = 6000
            }
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
            Name = "Currency Carrying Character",
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

    private static RulesetDefinition CreateCurrencyRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.currency_carrying_capacity_validation",
            Name = "Currency Carrying Capacity Validation Ruleset",
            Races =
            [
                CreateTestRace()
            ]
        };
    }

    private static RulesetDefinition CreateEquipmentInventoryAndCurrencyRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.equipment_inventory_currency_carrying_capacity_validation",
            Name = "Equipment Inventory Currency Carrying Capacity Validation Ruleset",
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
                new EquipmentItemDefinition
                {
                    Id = "equipment.small_bundle",
                    Name = "Small Bundle",
                    WeightPounds = 4m
                }
            ]
        };
    }

    private static RulesetDefinition CreateStrongRaceCurrencyRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.strong_race_currency_carrying_capacity_validation",
            Name = "Strong Race Currency Carrying Capacity Validation Ruleset",
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
}
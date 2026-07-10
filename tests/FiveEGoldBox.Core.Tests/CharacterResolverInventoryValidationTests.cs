using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverInventoryValidationTests
{
    [Fact]
    public void Validate_WithValidInventoryItems_ReturnsValidResult()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            InventoryItems =
            [
                new InventoryItemDraft
                {
                    ItemId = "equipment.backpack",
                    Quantity = 1
                },
                new InventoryItemDraft
                {
                    ItemId = "equipment.torch",
                    Quantity = 10
                }
            ]
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithZeroInventoryQuantity_ReturnsInvalidQuantityError()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            InventoryItems =
            [
                new InventoryItemDraft
                {
                    ItemId = "equipment.backpack",
                    Quantity = 0
                }
            ]
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.inventory.quantity.invalid");
    }

    [Fact]
    public void Validate_WithNegativeInventoryQuantity_ReturnsInvalidQuantityError()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            InventoryItems =
            [
                new InventoryItemDraft
                {
                    ItemId = "equipment.backpack",
                    Quantity = -1
                }
            ]
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.inventory.quantity.invalid");
    }

    [Fact]
    public void Validate_WithDuplicateInventoryItems_ReturnsDuplicateError()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            InventoryItems =
            [
                new InventoryItemDraft
                {
                    ItemId = "equipment.torch",
                    Quantity = 5
                },
                new InventoryItemDraft
                {
                    ItemId = "equipment.torch",
                    Quantity = 5
                }
            ]
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.inventory.duplicate");
    }

    [Fact]
    public void Validate_WithUnknownInventoryItem_ReturnsItemNotFoundError()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            InventoryItems =
            [
                new InventoryItemDraft
                {
                    ItemId = "equipment.not_real",
                    Quantity = 1
                }
            ]
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.inventory.item_not_found");
    }

    [Fact]
    public void Validate_WithoutRuleset_DoesNotValidateItemExistence()
    {
        CharacterDraft draft = CreateValidDraft() with
        {
            InventoryItems =
            [
                new InventoryItemDraft
                {
                    ItemId = "equipment.not_real",
                    Quantity = 1
                }
            ]
        };

        CharacterResolver resolver = new();

        ValidationResult result = resolver.Validate(draft);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithRulesetThatHasNoEquipmentItems_DoesNotValidateItemExistence()
    {
        RulesetDefinition ruleset = new()
        {
            Id = "ruleset.no_equipment_items",
            Name = "No Equipment Items Ruleset",
            Races =
            [
                CreateTestRace()
            ]
        };

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            InventoryItems =
            [
                new InventoryItemDraft
                {
                    ItemId = "equipment.not_real",
                    Quantity = 1
                }
            ]
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.True(result.IsValid);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Inventory Validation Character",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.Manual,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 10,
                [Ability.Dexterity] = 10,
                [Ability.Constitution] = 10,
                [Ability.Intelligence] = 10,
                [Ability.Wisdom] = 10,
                [Ability.Charisma] = 10
            }
        };
    }

    private static RulesetDefinition CreateEquipmentRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.inventory_validation",
            Name = "Inventory Validation Ruleset",
            Races =
            [
                CreateTestRace()
            ],
            EquipmentItems =
            [
                new EquipmentItemDefinition
                {
                    Id = "equipment.backpack",
                    Name = "Backpack",
                    WeightPounds = 5m
                },
                new EquipmentItemDefinition
                {
                    Id = "equipment.torch",
                    Name = "Torch",
                    WeightPounds = 1m
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
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverInventoryResolutionTests
{
    [Fact]
    public void Resolve_WithInventoryItems_SetsResolvedInventoryItemDetails()
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

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(2, snapshot.InventoryItems.Count);

        InventoryItemSnapshot backpack = GetInventoryItem(snapshot, "equipment.backpack");

        Assert.Equal("Backpack", backpack.ItemName);
        Assert.Equal(1, backpack.Quantity);
        Assert.Equal(5m, backpack.UnitWeightPounds);
        Assert.Equal(5m, backpack.TotalWeightPounds);
        Assert.Equal(200, backpack.UnitValueInCopperPieces);
        Assert.Equal(200, backpack.TotalValueInCopperPieces);
        Assert.Contains("equipment_tag.container", backpack.Tags);

        InventoryItemSnapshot torch = GetInventoryItem(snapshot, "equipment.torch");

        Assert.Equal("Torch", torch.ItemName);
        Assert.Equal(10, torch.Quantity);
        Assert.Equal(1m, torch.UnitWeightPounds);
        Assert.Equal(10m, torch.TotalWeightPounds);
        Assert.Equal(1, torch.UnitValueInCopperPieces);
        Assert.Equal(10, torch.TotalValueInCopperPieces);
        Assert.Contains("equipment_tag.adventuring_gear", torch.Tags);
    }

    [Fact]
    public void Resolve_WithInventoryItemThatHasNoCost_LeavesValueNull()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            InventoryItems =
            [
                new InventoryItemDraft
                {
                    ItemId = "equipment.keepsake",
                    Quantity = 1
                }
            ]
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        InventoryItemSnapshot keepsake = GetInventoryItem(snapshot, "equipment.keepsake");

        Assert.Null(keepsake.UnitValueInCopperPieces);
        Assert.Null(keepsake.TotalValueInCopperPieces);
    }

    [Fact]
    public void Resolve_WithNoInventoryItems_LeavesInventoryItemsEmpty()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            InventoryItems = []
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Empty(snapshot.InventoryItems);
    }

    [Fact]
    public void Resolve_WithoutRuleset_LeavesInventoryItemsEmpty()
    {
        CharacterDraft draft = CreateValidDraft() with
        {
            InventoryItems =
            [
                new InventoryItemDraft
                {
                    ItemId = "equipment.backpack",
                    Quantity = 1
                }
            ]
        };

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Empty(snapshot.InventoryItems);
    }

    [Fact]
    public void Resolve_WithRulesetThatHasNoEquipmentItems_LeavesInventoryItemsEmpty()
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
                    ItemId = "equipment.backpack",
                    Quantity = 1
                }
            ]
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Empty(snapshot.InventoryItems);
    }

    private static InventoryItemSnapshot GetInventoryItem(
        CharacterSnapshot snapshot,
        string itemId)
    {
        return Assert.Single(
            snapshot.InventoryItems,
            item => item.ItemId == itemId);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Inventory Resolution Character",
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
            Id = "ruleset.inventory_resolution",
            Name = "Inventory Resolution Ruleset",
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
                    WeightPounds = 5m,
                    CostInCopperPieces = 200,
                    Tags =
                    [
                        "equipment_tag.container",
                        "equipment_tag.adventuring_gear"
                    ]
                },
                new EquipmentItemDefinition
                {
                    Id = "equipment.torch",
                    Name = "Torch",
                    WeightPounds = 1m,
                    CostInCopperPieces = 1,
                    Tags =
                    [
                        "equipment_tag.adventuring_gear"
                    ]
                },
                new EquipmentItemDefinition
                {
                    Id = "equipment.keepsake",
                    Name = "Keepsake",
                    WeightPounds = 0m
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
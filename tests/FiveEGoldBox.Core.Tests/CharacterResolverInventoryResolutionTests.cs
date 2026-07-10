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
        Assert.Contains("equipment_tag.container", backpack.Tags);

        InventoryItemSnapshot torch = GetInventoryItem(snapshot, "equipment.torch");

        Assert.Equal("Torch", torch.ItemName);
        Assert.Equal(10, torch.Quantity);
        Assert.Equal(1m, torch.UnitWeightPounds);
        Assert.Equal(10m, torch.TotalWeightPounds);
        Assert.Contains("equipment_tag.adventuring_gear", torch.Tags);
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
                    Tags =
                    [
                        "equipment_tag.adventuring_gear"
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
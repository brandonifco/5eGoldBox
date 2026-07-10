using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class InventoryModelTests
{
    [Fact]
    public void EquipmentItemDefinition_CanRepresentAdventuringGear()
    {
        EquipmentItemDefinition backpack = new()
        {
            Id = "equipment.backpack",
            Name = "Backpack",
            WeightPounds = 5m,
            Tags =
            [
                "equipment_tag.container",
                "equipment_tag.adventuring_gear"
            ]
        };

        Assert.Equal("equipment.backpack", backpack.Id);
        Assert.Equal("Backpack", backpack.Name);
        Assert.Equal(5m, backpack.WeightPounds);
        Assert.Contains("equipment_tag.container", backpack.Tags);
        Assert.Contains("equipment_tag.adventuring_gear", backpack.Tags);
    }

    [Fact]
    public void InventoryItemDraft_CanRepresentItemQuantity()
    {
        InventoryItemDraft torches = new()
        {
            ItemId = "equipment.torch",
            Quantity = 10
        };

        Assert.Equal("equipment.torch", torches.ItemId);
        Assert.Equal(10, torches.Quantity);
    }

    [Fact]
    public void RulesetDefinition_CanContainEquipmentItems()
    {
        RulesetDefinition ruleset = new()
        {
            Id = "ruleset.inventory",
            Name = "Inventory Ruleset",
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

        Assert.Equal(2, ruleset.EquipmentItems.Count);

        Assert.Contains(
            ruleset.EquipmentItems,
            item => item.Id == "equipment.backpack"
                && item.WeightPounds == 5m);

        Assert.Contains(
            ruleset.EquipmentItems,
            item => item.Id == "equipment.torch"
                && item.WeightPounds == 1m);
    }

    [Fact]
    public void CharacterDraft_CanContainInventoryItems()
    {
        CharacterDraft draft = new()
        {
            Name = "Inventory Character",
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
            },
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

        Assert.Equal(2, draft.InventoryItems.Count);

        Assert.Contains(
            draft.InventoryItems,
            item => item.ItemId == "equipment.backpack"
                && item.Quantity == 1);

        Assert.Contains(
            draft.InventoryItems,
            item => item.ItemId == "equipment.torch"
                && item.Quantity == 10);
    }
}
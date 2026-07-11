using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverInventoryWeightTests
{
    [Fact]
    public void Resolve_WithNoInventoryItems_SetsInventoryWeightToZero()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateDraftWithStrength(10) with
        {
            RaceId = "race.test"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(0m, snapshot.EquippedWeightPounds);
        Assert.Equal(0m, snapshot.InventoryWeightPounds);
        Assert.Equal(0m, snapshot.TotalCarriedWeightPounds);
        Assert.False(snapshot.IsOverCarryingCapacity);
    }

    [Fact]
    public void Resolve_WithInventoryItems_SetsInventoryWeight()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateDraftWithStrength(10) with
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

        Assert.Equal(0m, snapshot.EquippedWeightPounds);
        Assert.Equal(15m, snapshot.InventoryWeightPounds);
        Assert.Equal(15m, snapshot.TotalCarriedWeightPounds);
        Assert.False(snapshot.IsOverCarryingCapacity);
    }

    [Fact]
    public void Resolve_WithEquippedGearAndInventoryItems_SetsTotalCarriedWeight()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateDraftWithStrength(10) with
        {
            RaceId = "race.test",
            EquippedArmorId = "armor.leather",
            EquippedShieldId = "armor.shield",
            EquippedWeaponIds =
            [
                "weapon.longsword"
            ],
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

        Assert.Equal(19m, snapshot.EquippedWeightPounds);
        Assert.Equal(15m, snapshot.InventoryWeightPounds);
        Assert.Equal(34m, snapshot.TotalCarriedWeightPounds);
        Assert.False(snapshot.IsOverCarryingCapacity);
    }

    [Fact]
    public void Resolve_WithInventoryPushingTotalAboveCarryingCapacity_IsOverCarryingCapacity()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.test",
            EquippedArmorId = "armor.heavy_load",
            InventoryItems =
            [
                new InventoryItemDraft
                {
                    ItemId = "equipment.torch",
                    Quantity = 10
                }
            ]
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(120, snapshot.CarryingCapacityPounds);
        Assert.Equal(115m, snapshot.EquippedWeightPounds);
        Assert.Equal(10m, snapshot.InventoryWeightPounds);
        Assert.Equal(125m, snapshot.TotalCarriedWeightPounds);
        Assert.True(snapshot.IsOverCarryingCapacity);
    }

    [Fact]
    public void Resolve_WithTotalWeightEqualToCarryingCapacity_IsNotOverCarryingCapacity()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.test",
            EquippedArmorId = "armor.heavy_load",
            InventoryItems =
            [
                new InventoryItemDraft
                {
                    ItemId = "equipment.torch",
                    Quantity = 5
                }
            ]
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(120, snapshot.CarryingCapacityPounds);
        Assert.Equal(115m, snapshot.EquippedWeightPounds);
        Assert.Equal(5m, snapshot.InventoryWeightPounds);
        Assert.Equal(120m, snapshot.TotalCarriedWeightPounds);
        Assert.False(snapshot.IsOverCarryingCapacity);
    }

    private static CharacterDraft CreateDraftWithStrength(int strength)
    {
        return new CharacterDraft
        {
            Name = "Inventory Weight Character",
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

    private static RulesetDefinition CreateEquipmentRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.inventory_weight",
            Name = "Inventory Weight Ruleset",
            Races =
            [
                new RaceDefinition
                {
                    Id = "race.test",
                    Name = "Test Race",
                    BaseSpeedFeet = 30
                }
            ],
            Armors =
            [
                CreateLeatherArmor(),
                CreateShield(),
                CreateHeavyLoadArmor()
            ],
            Weapons =
            [
                CreateLongsword()
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

    private static ArmorDefinition CreateLeatherArmor()
    {
        return new ArmorDefinition
        {
            Id = "armor.leather",
            Name = "Leather",
            Category = ArmorCategory.Light,
            BaseArmorClass = 11,
            AddsDexterityModifier = true,
            WeightPounds = 10m
        };
    }

    private static ArmorDefinition CreateShield()
    {
        return new ArmorDefinition
        {
            Id = "armor.shield",
            Name = "Shield",
            Category = ArmorCategory.Shield,
            BaseArmorClass = 0,
            ArmorClassBonus = 2,
            WeightPounds = 6m
        };
    }

    private static ArmorDefinition CreateHeavyLoadArmor()
    {
        return new ArmorDefinition
        {
            Id = "armor.heavy_load",
            Name = "Heavy Load",
            Category = ArmorCategory.Heavy,
            BaseArmorClass = 16,
            WeightPounds = 115m
        };
    }

    private static WeaponDefinition CreateLongsword()
    {
        return new WeaponDefinition
        {
            Id = "weapon.longsword",
            Name = "Longsword",
            Category = WeaponCategory.Martial,
            AttackKind = WeaponAttackKind.Melee,
            Damage = new DamageDice
            {
                Count = 1,
                Die = DieType.D8
            },
            DamageType = "damage.slashing",
            WeightPounds = 3m
        };
    }
}

using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverCurrencyWeightTests
{
    [Fact]
    public void Resolve_WithNoCurrency_SetsCurrencyWeightToZero()
    {
        CharacterDraft draft = CreateDraftWithStrength(10);

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(0m, snapshot.CurrencyWeightPounds);
        Assert.Equal(0m, snapshot.TotalCarriedWeightPounds);
        Assert.False(snapshot.IsOverCarryingCapacity);
    }

    [Fact]
    public void Resolve_WithFiftyCoins_SetsCurrencyWeightToOnePound()
    {
        CharacterDraft draft = CreateDraftWithStrength(10) with
        {
            Currency = new CurrencyAmount
            {
                GoldPieces = 50
            }
        };

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(1m, snapshot.CurrencyWeightPounds);
        Assert.Equal(1m, snapshot.TotalCarriedWeightPounds);
        Assert.False(snapshot.IsOverCarryingCapacity);
    }

    [Fact]
    public void Resolve_WithMixedCoins_UsesTotalCoinCountForCurrencyWeight()
    {
        CharacterDraft draft = CreateDraftWithStrength(10) with
        {
            Currency = new CurrencyAmount
            {
                CopperPieces = 10,
                SilverPieces = 20,
                ElectrumPieces = 30,
                GoldPieces = 40,
                PlatinumPieces = 50
            }
        };

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(3m, snapshot.CurrencyWeightPounds);
        Assert.Equal(3m, snapshot.TotalCarriedWeightPounds);
        Assert.False(snapshot.IsOverCarryingCapacity);
    }

    [Fact]
    public void Resolve_WithEquippedGearInventoryAndCurrency_IncludesCurrencyInTotalCarriedWeight()
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
            ],
            Currency = new CurrencyAmount
            {
                GoldPieces = 50
            }
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(19m, snapshot.EquippedWeightPounds);
        Assert.Equal(15m, snapshot.InventoryWeightPounds);
        Assert.Equal(1m, snapshot.CurrencyWeightPounds);
        Assert.Equal(35m, snapshot.TotalCarriedWeightPounds);
        Assert.False(snapshot.IsOverCarryingCapacity);
    }

    [Fact]
    public void Resolve_WithCurrencyPushingTotalAboveCarryingCapacity_IsOverCarryingCapacity()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.test",
            EquippedArmorId = "armor.heavy_load",
            Currency = new CurrencyAmount
            {
                GoldPieces = 300
            }
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(120, snapshot.CarryingCapacityPounds);
        Assert.Equal(115m, snapshot.EquippedWeightPounds);
        Assert.Equal(0m, snapshot.InventoryWeightPounds);
        Assert.Equal(6m, snapshot.CurrencyWeightPounds);
        Assert.Equal(121m, snapshot.TotalCarriedWeightPounds);
        Assert.True(snapshot.IsOverCarryingCapacity);
    }

    private static CharacterDraft CreateDraftWithStrength(int strength)
    {
        return new CharacterDraft
        {
            Name = "Currency Weight Character",
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
            Id = "ruleset.currency_weight",
            Name = "Currency Weight Ruleset",
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
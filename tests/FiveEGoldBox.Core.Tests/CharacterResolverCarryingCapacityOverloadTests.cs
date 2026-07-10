using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverCarryingCapacityOverloadTests
{
    [Fact]
    public void Resolve_WithNoEquippedGear_IsNotOverCarryingCapacity()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.test"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(120, snapshot.CarryingCapacityPounds);
        Assert.Equal(0m, snapshot.EquippedWeightPounds);
        Assert.False(snapshot.IsOverCarryingCapacity);
    }

    [Fact]
    public void Resolve_WithEquippedWeightBelowCarryingCapacity_IsNotOverCarryingCapacity()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.test",
            EquippedArmorId = "armor.light_load"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(120, snapshot.CarryingCapacityPounds);
        Assert.Equal(60m, snapshot.EquippedWeightPounds);
        Assert.False(snapshot.IsOverCarryingCapacity);
    }

    [Fact]
    public void Resolve_WithEquippedWeightEqualToCarryingCapacity_IsNotOverCarryingCapacity()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.test",
            EquippedArmorId = "armor.exact_load"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(120, snapshot.CarryingCapacityPounds);
        Assert.Equal(120m, snapshot.EquippedWeightPounds);
        Assert.False(snapshot.IsOverCarryingCapacity);
    }

    [Fact]
    public void Resolve_WithEquippedWeightAboveCarryingCapacity_IsOverCarryingCapacity()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.test",
            EquippedArmorId = "armor.exact_load",
            EquippedWeaponIds =
            [
                "weapon.heavy_weapon"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(120, snapshot.CarryingCapacityPounds);
        Assert.Equal(125m, snapshot.EquippedWeightPounds);
        Assert.True(snapshot.IsOverCarryingCapacity);
    }

    [Fact]
    public void Resolve_WithRacialStrengthIncrease_UsesFinalStrengthForOverloadCheck()
    {
        RulesetDefinition ruleset = CreateStrongRaceEquipmentRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.strong",
            EquippedArmorId = "armor.exact_load"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(135, snapshot.CarryingCapacityPounds);
        Assert.Equal(120m, snapshot.EquippedWeightPounds);
        Assert.False(snapshot.IsOverCarryingCapacity);
    }

    private static CharacterDraft CreateDraftWithStrength(int strength)
    {
        return new CharacterDraft
        {
            Name = "Loaded Character",
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
            Id = "ruleset.carrying_overload",
            Name = "Carrying Overload Ruleset",
            Races =
            [
                CreateTestRace()
            ],
            Armors =
            [
                CreateLightLoadArmor(),
                CreateExactLoadArmor()
            ],
            Weapons =
            [
                CreateHeavyWeapon()
            ]
        };
    }

    private static RulesetDefinition CreateStrongRaceEquipmentRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.strong_race_carrying_overload",
            Name = "Strong Race Carrying Overload Ruleset",
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
            Armors =
            [
                CreateExactLoadArmor()
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

    private static ArmorDefinition CreateLightLoadArmor()
    {
        return new ArmorDefinition
        {
            Id = "armor.light_load",
            Name = "Light Load",
            Category = ArmorCategory.Light,
            BaseArmorClass = 11,
            AddsDexterityModifier = true,
            WeightPounds = 60m
        };
    }

    private static ArmorDefinition CreateExactLoadArmor()
    {
        return new ArmorDefinition
        {
            Id = "armor.exact_load",
            Name = "Exact Load",
            Category = ArmorCategory.Heavy,
            BaseArmorClass = 16,
            WeightPounds = 120m
        };
    }

    private static WeaponDefinition CreateHeavyWeapon()
    {
        return new WeaponDefinition
        {
            Id = "weapon.heavy_weapon",
            Name = "Heavy Weapon",
            Category = WeaponCategory.Martial,
            AttackKind = WeaponAttackKind.Melee,
            Damage = new DamageDice
            {
                Count = 1,
                Die = DieType.D12
            },
            DamageType = "damage.bludgeoning",
            WeightPounds = 5m
        };
    }
}
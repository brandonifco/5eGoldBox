using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverCarryingCapacityValidationTests
{
    [Fact]
    public void Validate_WithNoEquippedGear_DoesNotReturnCarryingCapacityWarning()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.test",
            ClassId = "class.test"
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code == "character.carrying_capacity.exceeded");
    }

    [Fact]
    public void Validate_WithEquippedWeightBelowCarryingCapacity_DoesNotReturnCarryingCapacityWarning()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.test",
            ClassId = "class.test",
            EquippedArmorId = "armor.light_load"
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code == "character.carrying_capacity.exceeded");
    }

    [Fact]
    public void Validate_WithEquippedWeightEqualToCarryingCapacity_DoesNotReturnCarryingCapacityWarning()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.test",
            ClassId = "class.test",
            EquippedArmorId = "armor.exact_load"
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code == "character.carrying_capacity.exceeded");
    }

    [Fact]
    public void Validate_WithEquippedWeightAboveCarryingCapacity_ReturnsWarningButRemainsValid()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.test",
            ClassId = "class.test",
            EquippedArmorId = "armor.exact_load",
            EquippedWeaponIds =
            [
                "weapon.heavy_weapon"
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
    public void Validate_WithRacialStrengthIncrease_UsesFinalStrengthForCarryingCapacityWarning()
    {
        RulesetDefinition ruleset = CreateStrongRaceEquipmentRuleset();

        CharacterDraft draft = CreateDraftWithStrength(8) with
        {
            RaceId = "race.strong",
            ClassId = "class.test",
            EquippedArmorId = "armor.exact_load"
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
            Id = "ruleset.carrying_capacity_validation",
            Name = "Carrying Capacity Validation Ruleset",
            Races =
            [
                CreateTestRace()
            ],
            Classes =
            [
                CreateTestClass()
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
            Id = "ruleset.strong_race_carrying_capacity_validation",
            Name = "Strong Race Carrying Capacity Validation Ruleset",
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
            Classes =
            [
                CreateTestClass()
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

    private static ClassDefinition CreateTestClass()
    {
        return new ClassDefinition
        {
            Id = "class.test",
            Name = "Test Class",
            HitDie = DieType.D8,
            ArmorProficiencies =
            [
                "armor.light",
                "armor.heavy"
            ],
            WeaponProficiencies =
            [
                "weapon.martial"
            ]
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

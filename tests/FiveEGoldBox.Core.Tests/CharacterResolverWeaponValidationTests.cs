using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverWeaponValidationTests
{
    [Fact]
    public void Validate_WithValidEquippedWeapons_ReturnsValidResult()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedWeaponIds =
            [
                "weapon.longsword",
                "weapon.shortbow"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithDuplicateEquippedWeapons_ReturnsDuplicateError()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedWeaponIds =
            [
                "weapon.longsword",
                "weapon.longsword"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.weapons.duplicate");
    }

    [Fact]
    public void Validate_WithUnknownEquippedWeapon_ReturnsWeaponNotFoundError()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedWeaponIds =
            [
                "weapon.not_real"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.weapon.not_found");
    }

    [Fact]
    public void Validate_WithoutRuleset_DoesNotValidateWeapons()
    {
        CharacterDraft draft = CreateValidDraft() with
        {
            EquippedWeaponIds =
            [
                "weapon.not_real",
                "weapon.not_real"
            ]
        };

        CharacterResolver resolver = new();

        var result = resolver.Validate(draft);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithRulesetThatHasNoWeapons_DoesNotValidateWeapons()
    {
        RulesetDefinition ruleset = new()
        {
            Id = "ruleset.no_weapons",
            Name = "No Weapons Ruleset",
            Races =
            [
                CreateHumanRace()
            ],
            Classes =
            [
                CreateFighterClass()
            ]
        };

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedWeaponIds =
            [
                "weapon.not_real",
                "weapon.not_real"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.True(result.IsValid);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Test Fighter",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.StandardArray,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 15,
                [Ability.Dexterity] = 14,
                [Ability.Constitution] = 13,
                [Ability.Intelligence] = 12,
                [Ability.Wisdom] = 10,
                [Ability.Charisma] = 8
            }
        };
    }

    private static RulesetDefinition CreateTestRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.test",
            Name = "Test Ruleset",
            Races =
            [
                CreateHumanRace()
            ],
            Classes =
            [
                CreateFighterClass()
            ],
            Weapons =
            [
                new WeaponDefinition
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
                },
                new WeaponDefinition
                {
                    Id = "weapon.shortbow",
                    Name = "Shortbow",
                    Category = WeaponCategory.Simple,
                    AttackKind = WeaponAttackKind.Ranged,
                    Damage = new DamageDice
                    {
                        Count = 1,
                        Die = DieType.D6
                    },
                    DamageType = "damage.piercing",
                    NormalRangeFeet = 80,
                    LongRangeFeet = 320,
                    WeightPounds = 2m
                }
            ]
        };
    }

    private static RaceDefinition CreateHumanRace()
    {
        return new RaceDefinition
        {
            Id = "race.human",
            Name = "Human",
            BaseSpeedFeet = 30,
            AbilityScoreIncreases =
            [
                new AbilityScoreIncrease(Ability.Strength, 1),
                new AbilityScoreIncrease(Ability.Dexterity, 1),
                new AbilityScoreIncrease(Ability.Constitution, 1),
                new AbilityScoreIncrease(Ability.Intelligence, 1),
                new AbilityScoreIncrease(Ability.Wisdom, 1),
                new AbilityScoreIncrease(Ability.Charisma, 1)
            ],
            Languages =
            [
                "language.common"
            ]
        };
    }

    private static ClassDefinition CreateFighterClass()
    {
        return new ClassDefinition
        {
            Id = "class.fighter",
            Name = "Fighter",
            HitDie = DieType.D10,
            SavingThrowProficiencies =
            [
                Ability.Strength,
                Ability.Constitution
            ]
        };
    }
}

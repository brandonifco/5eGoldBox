using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverWeaponProficiencyTests
{
    [Fact]
    public void Validate_WithProficientSimpleAndMartialWeapons_DoesNotReturnWeaponProficiencyWarnings()
    {
        RulesetDefinition ruleset = CreateFighterRuleset();

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

        ValidationResult result = resolver.Validate(draft);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code == "character.weapon.not_proficient");
    }

    [Fact]
    public void Validate_WithNonProficientWeapon_ReturnsWeaponWarningButRemainsValid()
    {
        RulesetDefinition ruleset = CreateSimpleOnlyRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.simple_only",
            EquippedWeaponIds =
            [
                "weapon.longsword"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.True(result.IsValid);

        ValidationIssue issue = Assert.Single(
            result.Issues,
            issue => issue.Code == "character.weapon.not_proficient");

        Assert.Equal(ValidationSeverity.Warning, issue.Severity);
    }

    [Fact]
    public void Validate_WithSpecificWeaponProficiency_DoesNotReturnWeaponProficiencyWarning()
    {
        RulesetDefinition ruleset = CreateSpecificWeaponProficiencyRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.longsword_only",
            EquippedWeaponIds =
            [
                "weapon.longsword"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code == "character.weapon.not_proficient");
    }

    [Fact]
    public void Validate_WithoutSelectedClass_ReturnsWeaponWarningButRemainsValid()
    {
        RulesetDefinition ruleset = new()
        {
            Id = "ruleset.weapons_only",
            Name = "Weapons Only Ruleset",
            Races =
            [
                CreateHumanRace()
            ],
            Weapons =
            [
                CreateLongsword()
            ]
        };

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            EquippedWeaponIds =
            [
                "weapon.longsword"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.True(result.IsValid);

        ValidationIssue issue = Assert.Single(
            result.Issues,
            issue => issue.Code == "character.weapon.not_proficient");

        Assert.Equal(ValidationSeverity.Warning, issue.Severity);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Test Character",
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

    private static RulesetDefinition CreateFighterRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.fighter",
            Name = "Fighter Ruleset",
            Races =
            [
                CreateHumanRace()
            ],
            Classes =
            [
                new ClassDefinition
                {
                    Id = "class.fighter",
                    Name = "Fighter",
                    HitDie = DieType.D10,
                    WeaponProficiencies =
                    [
                        "weapon.simple",
                        "weapon.martial"
                    ]
                }
            ],
            Weapons =
            [
                CreateLongsword(),
                CreateShortbow()
            ]
        };
    }

    private static RulesetDefinition CreateSimpleOnlyRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.simple_only",
            Name = "Simple Only Ruleset",
            Races =
            [
                CreateHumanRace()
            ],
            Classes =
            [
                new ClassDefinition
                {
                    Id = "class.simple_only",
                    Name = "Simple Only",
                    HitDie = DieType.D8,
                    WeaponProficiencies =
                    [
                        "weapon.simple"
                    ]
                }
            ],
            Weapons =
            [
                CreateLongsword()
            ]
        };
    }

    private static RulesetDefinition CreateSpecificWeaponProficiencyRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.longsword_only",
            Name = "Longsword Only Ruleset",
            Races =
            [
                CreateHumanRace()
            ],
            Classes =
            [
                new ClassDefinition
                {
                    Id = "class.longsword_only",
                    Name = "Longsword Only",
                    HitDie = DieType.D8,
                    WeaponProficiencies =
                    [
                        "weapon.longsword"
                    ]
                }
            ],
            Weapons =
            [
                CreateLongsword()
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

    private static WeaponDefinition CreateShortbow()
    {
        return new WeaponDefinition
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
        };
    }
}

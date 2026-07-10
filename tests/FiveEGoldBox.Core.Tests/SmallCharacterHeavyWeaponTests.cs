using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Core.Tests;

public sealed class SmallCharacterHeavyWeaponTests
{
    [Fact]
    public void Validate_WithSmallCharacterAndHeavyWeapon_AddsWarning()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.halfling",
            EquippedWeaponIds =
            [
                "weapon.greatsword"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        ValidationIssue issue = Assert.Single(
            result.Issues,
            issue => issue.Code == "character.weapon.heavy.small_size");

        Assert.Equal(ValidationSeverity.Warning, issue.Severity);
        Assert.Contains("Small", issue.Message);
        Assert.Contains("Greatsword", issue.Message);
    }

    [Fact]
    public void Validate_WithMediumCharacterAndHeavyWeapon_DoesNotAddWarning()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            EquippedWeaponIds =
            [
                "weapon.greatsword"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code == "character.weapon.heavy.small_size");
    }

    [Fact]
    public void Validate_WithSmallCharacterAndNonHeavyWeapon_DoesNotAddWarning()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.halfling",
            EquippedWeaponIds =
            [
                "weapon.longsword"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code == "character.weapon.heavy.small_size");
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Small Heavy Weapon Character",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.Manual,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 15,
                [Ability.Dexterity] = 10,
                [Ability.Constitution] = 10,
                [Ability.Intelligence] = 10,
                [Ability.Wisdom] = 10,
                [Ability.Charisma] = 10
            }
        };
    }

    private static RulesetDefinition CreateRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.small_heavy_weapon",
            Name = "Small Heavy Weapon Ruleset",
            Races =
            [
                new RaceDefinition
                {
                    Id = "race.human",
                    Name = "Human",
                    Size = CharacterSize.Medium,
                    BaseSpeedFeet = 30
                },
                new RaceDefinition
                {
                    Id = "race.halfling",
                    Name = "Halfling",
                    Size = CharacterSize.Small,
                    BaseSpeedFeet = 25
                }
            ],
            Weapons =
            [
                CreateGreatsword(),
                CreateLongsword()
            ]
        };
    }

    private static WeaponDefinition CreateGreatsword()
    {
        return new WeaponDefinition
        {
            Id = "weapon.greatsword",
            Name = "Greatsword",
            Category = WeaponCategory.Martial,
            AttackKind = WeaponAttackKind.Melee,
            Damage = new DamageDice
            {
                Count = 2,
                Die = DieType.D6
            },
            DamageType = "damage.slashing",
            Properties =
            [
                "weapon_property.heavy",
                "weapon_property.two_handed"
            ],
            WeightPounds = 6m,
            CostInCopperPieces = 5000
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
            VersatileDamage = new DamageDice
            {
                Count = 1,
                Die = DieType.D10
            },
            DamageType = "damage.slashing",
            Properties =
            [
                "weapon_property.versatile"
            ],
            WeightPounds = 3m,
            CostInCopperPieces = 1500
        };
    }
}
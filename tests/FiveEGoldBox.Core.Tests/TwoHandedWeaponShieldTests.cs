using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Core.Tests;

public sealed class TwoHandedWeaponShieldTests
{
    [Fact]
    public void Validate_WithShieldAndTwoHandedWeapon_AddsWarning()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            EquippedShieldId = "armor.shield",
            EquippedWeaponIds =
            [
                "weapon.greatsword"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        ValidationIssue issue = Assert.Single(
            result.Issues,
            issue => issue.Code == "character.weapon.two_handed.shield_equipped");

        Assert.Equal(ValidationSeverity.Warning, issue.Severity);
        Assert.Contains("Greatsword", issue.Message);
        Assert.Contains("Shield", issue.Message);
    }

    [Fact]
    public void Validate_WithTwoHandedWeaponAndNoShield_DoesNotAddWarning()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            EquippedWeaponIds =
            [
                "weapon.greatsword"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code == "character.weapon.two_handed.shield_equipped");
    }

    [Fact]
    public void Validate_WithShieldAndNonTwoHandedWeapon_DoesNotAddWarning()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            EquippedShieldId = "armor.shield",
            EquippedWeaponIds =
            [
                "weapon.longsword"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code == "character.weapon.two_handed.shield_equipped");
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Two-Handed Weapon Shield Character",
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
            Id = "ruleset.two_handed_weapon_shield",
            Name = "Two-Handed Weapon Shield Ruleset",
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
                new ArmorDefinition
                {
                    Id = "armor.shield",
                    Name = "Shield",
                    Category = ArmorCategory.Shield,
                    BaseArmorClass = 0,
                    ArmorClassBonus = 2,
                    WeightPounds = 6m,
                    CostInCopperPieces = 1000
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

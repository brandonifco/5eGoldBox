using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverArmorProficiencyTests
{
    [Fact]
    public void Validate_WithProficientArmorAndShield_DoesNotReturnProficiencyWarnings()
    {
        RulesetDefinition ruleset = CreateLightArmorAndShieldRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.light_armor_user",
            EquippedArmorId = "armor.leather",
            EquippedShieldId = "armor.shield"
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code is "character.armor.not_proficient" or "character.shield.not_proficient");
    }

    [Fact]
    public void Validate_WithNonProficientArmor_ReturnsArmorWarningButRemainsValid()
    {
        RulesetDefinition ruleset = CreateLightArmorAndShieldRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.light_armor_user",
            EquippedArmorId = "armor.chain_mail"
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.True(result.IsValid);

        ValidationIssue issue = Assert.Single(
            result.Issues,
            issue => issue.Code == "character.armor.not_proficient");

        Assert.Equal(ValidationSeverity.Warning, issue.Severity);
    }

    [Fact]
    public void Validate_WithNonProficientShield_ReturnsShieldWarningButRemainsValid()
    {
        RulesetDefinition ruleset = CreateNoShieldRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.no_shield_user",
            EquippedShieldId = "armor.shield"
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.True(result.IsValid);

        ValidationIssue issue = Assert.Single(
            result.Issues,
            issue => issue.Code == "character.shield.not_proficient");

        Assert.Equal(ValidationSeverity.Warning, issue.Severity);
    }

    [Fact]
    public void Validate_WithSpecificArmorProficiency_DoesNotReturnArmorWarning()
    {
        RulesetDefinition ruleset = CreateSpecificArmorProficiencyRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.chain_mail_only",
            EquippedArmorId = "armor.chain_mail"
        };

        CharacterResolver resolver = new(ruleset);

        ValidationResult result = resolver.Validate(draft);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code == "character.armor.not_proficient");
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

    private static RulesetDefinition CreateLightArmorAndShieldRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.light_armor_and_shield",
            Name = "Light Armor and Shield Ruleset",
            Races =
            [
                CreateHumanRace()
            ],
            Classes =
            [
                new ClassDefinition
                {
                    Id = "class.light_armor_user",
                    Name = "Light Armor User",
                    HitDie = DieType.D8,
                    ArmorProficiencies =
                    [
                        "armor.light",
                        "armor.shields"
                    ]
                }
            ],
            Armors =
            [
                CreateLeatherArmor(),
                CreateChainMail(),
                CreateShield()
            ]
        };
    }

    private static RulesetDefinition CreateNoShieldRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.no_shield",
            Name = "No Shield Ruleset",
            Races =
            [
                CreateHumanRace()
            ],
            Classes =
            [
                new ClassDefinition
                {
                    Id = "class.no_shield_user",
                    Name = "No Shield User",
                    HitDie = DieType.D8,
                    ArmorProficiencies =
                    [
                        "armor.light"
                    ]
                }
            ],
            Armors =
            [
                CreateLeatherArmor(),
                CreateShield()
            ]
        };
    }

    private static RulesetDefinition CreateSpecificArmorProficiencyRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.chain_mail_only",
            Name = "Chain Mail Only Ruleset",
            Races =
            [
                CreateHumanRace()
            ],
            Classes =
            [
                new ClassDefinition
                {
                    Id = "class.chain_mail_only",
                    Name = "Chain Mail Only",
                    HitDie = DieType.D8,
                    ArmorProficiencies =
                    [
                        "armor.chain_mail"
                    ]
                }
            ],
            Armors =
            [
                CreateChainMail()
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

    private static ArmorDefinition CreateChainMail()
    {
        return new ArmorDefinition
        {
            Id = "armor.chain_mail",
            Name = "Chain Mail",
            Category = ArmorCategory.Heavy,
            BaseArmorClass = 16,
            StrengthRequirement = 13,
            HasStealthDisadvantage = true,
            WeightPounds = 55m
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
}

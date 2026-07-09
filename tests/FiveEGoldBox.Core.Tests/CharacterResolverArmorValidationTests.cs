using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverArmorValidationTests
{
    [Fact]
    public void Validate_WithValidArmorAndShield_ReturnsValidResult()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedArmorId = "armor.chain_mail",
            EquippedShieldId = "armor.shield"
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithUnknownArmor_ReturnsArmorNotFoundError()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedArmorId = "armor.not_real"
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.armor.not_found");
    }

    [Fact]
    public void Validate_WithShieldEquippedAsArmor_ReturnsArmorInvalidCategoryError()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedArmorId = "armor.shield"
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.armor.invalid_category");
    }

    [Fact]
    public void Validate_WithUnknownShield_ReturnsShieldNotFoundError()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedShieldId = "armor.not_real"
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.shield.not_found");
    }

    [Fact]
    public void Validate_WithBodyArmorEquippedAsShield_ReturnsShieldInvalidCategoryError()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedShieldId = "armor.chain_mail"
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.shield.invalid_category");
    }

    [Fact]
    public void Validate_WithoutRuleset_DoesNotValidateArmor()
    {
        CharacterDraft draft = CreateValidDraft() with
        {
            EquippedArmorId = "armor.not_real",
            EquippedShieldId = "armor.not_real"
        };

        CharacterResolver resolver = new();

        var result = resolver.Validate(draft);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithRulesetThatHasNoArmors_DoesNotValidateArmor()
    {
        RulesetDefinition ruleset = new()
        {
            Id = "ruleset.no_armors",
            Name = "No Armors Ruleset",
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
            EquippedArmorId = "armor.not_real",
            EquippedShieldId = "armor.not_real"
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
            Armors =
            [
                new ArmorDefinition
                {
                    Id = "armor.chain_mail",
                    Name = "Chain Mail",
                    Category = ArmorCategory.Heavy,
                    BaseArmorClass = 16,
                    AddsDexterityModifier = false,
                    StrengthRequirement = 13,
                    HasStealthDisadvantage = true,
                    WeightPounds = 55m
                },
                new ArmorDefinition
                {
                    Id = "armor.shield",
                    Name = "Shield",
                    Category = ArmorCategory.Shield,
                    BaseArmorClass = 0,
                    ArmorClassBonus = 2,
                    WeightPounds = 6m
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
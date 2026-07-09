using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverClassSkillValidationTests
{
    [Fact]
    public void Validate_WithValidClassSkillChoices_ReturnsValidResult()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            SelectedSkillIds =
            [
                "skill.athletics",
                "skill.perception"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithTooFewClassSkillChoices_ReturnsInvalidCountError()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            SelectedSkillIds =
            [
                "skill.athletics"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.skills.invalid_count");
    }

    [Fact]
    public void Validate_WithDuplicateClassSkillChoices_ReturnsDuplicateError()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            SelectedSkillIds =
            [
                "skill.athletics",
                "skill.athletics"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.skills.duplicate");
    }

    [Fact]
    public void Validate_WithUnavailableClassSkillChoice_ReturnsNotAvailableError()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            SelectedSkillIds =
            [
                "skill.athletics",
                "skill.arcana"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.skills.not_available");
    }

    [Fact]
    public void Validate_WithClassThatAllowsNoSkillChoicesAndSelectedSkill_ReturnsNotAllowedError()
    {
        RulesetDefinition ruleset = CreateNoSkillChoiceRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.test_no_skills",
            SelectedSkillIds =
            [
                "skill.athletics"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.skills.not_allowed");
    }

    [Fact]
    public void Validate_WithoutRuleset_DoesNotValidateSelectedSkills()
    {
        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = null,
            ClassId = null,
            SelectedSkillIds =
            [
                "skill.not_real"
            ]
        };

        CharacterResolver resolver = new();

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
                new ClassDefinition
                {
                    Id = "class.fighter",
                    Name = "Fighter",
                    HitDie = DieType.D10,
                    SavingThrowProficiencies =
                    [
                        Ability.Strength,
                        Ability.Constitution
                    ],
                    SkillChoices =
                    [
                        "skill.acrobatics",
                        "skill.animal_handling",
                        "skill.athletics",
                        "skill.history",
                        "skill.insight",
                        "skill.intimidation",
                        "skill.perception",
                        "skill.survival"
                    ],
                    NumberOfSkillChoices = 2
                }
            ]
        };
    }

    private static RulesetDefinition CreateNoSkillChoiceRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.no_skill_choices",
            Name = "No Skill Choices Ruleset",
            Races =
            [
                CreateHumanRace()
            ],
            Classes =
            [
                new ClassDefinition
                {
                    Id = "class.test_no_skills",
                    Name = "Test No Skills",
                    HitDie = DieType.D8,
                    NumberOfSkillChoices = 0
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
}
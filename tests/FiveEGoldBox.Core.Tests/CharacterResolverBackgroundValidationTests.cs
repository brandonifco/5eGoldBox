using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Tests.Builders;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverBackgroundValidationTests
{
    [Fact]
    public void Validate_WithRulesetAndValidBackground_ReturnsValidResult()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            BackgroundId = "background.folk_hero"
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithRulesetAndMissingBackground_ReturnsBackgroundRequiredError()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            BackgroundId = null
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.background.required");
    }

    [Fact]
    public void Validate_WithRulesetAndUnknownBackground_ReturnsBackgroundNotFoundError()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            BackgroundId = "background.not_real"
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.background.not_found");
    }

    [Fact]
    public void Validate_WithRulesetThatHasNoBackgrounds_DoesNotRequireBackground()
    {
        RulesetDefinition ruleset = new()
        {
            Id = "ruleset.no_backgrounds",
            Name = "No Backgrounds Ruleset",
            Races =
            [
                TestRulesetBuilder.HumanRace()
            ]
        };

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            BackgroundId = null
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.True(result.IsValid);
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

    private static RulesetDefinition CreateTestRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.test",
            Name = "Test Ruleset",
            Races =
            [
                TestRulesetBuilder.HumanRace()
            ],
            Backgrounds =
            [
                new BackgroundDefinition
                {
                    Id = "background.folk_hero",
                    Name = "Folk Hero",
                    SkillProficiencies =
                    [
                        "skill.animal_handling",
                        "skill.survival"
                    ],
                    ToolProficiencies =
                    [
                        "tool.artisans_tools",
                        "tool.vehicles_land"
                    ],
                    FeatureId = "feature.rustic_hospitality"
                }
            ]
        };
    }

}

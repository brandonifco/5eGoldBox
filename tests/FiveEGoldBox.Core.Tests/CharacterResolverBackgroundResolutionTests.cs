using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverBackgroundResolutionTests
{
    [Fact]
    public void Resolve_WithSelectedBackground_SetsBackgroundMetadata()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            BackgroundId = "background.folk_hero"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal("background.folk_hero", snapshot.BackgroundId);
        Assert.Equal("Folk Hero", snapshot.BackgroundName);
        Assert.Equal("feature.rustic_hospitality", snapshot.BackgroundFeatureId);
    }

    [Fact]
    public void Resolve_WithSelectedBackground_AddsBackgroundSkillProficiencies()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            BackgroundId = "background.folk_hero"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Contains("skill.animal_handling", snapshot.SkillProficiencies);
        Assert.Contains("skill.survival", snapshot.SkillProficiencies);
    }

    [Fact]
    public void Resolve_WithSelectedBackground_AddsBackgroundToolProficiencies()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            BackgroundId = "background.folk_hero"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Contains("tool.artisans_tools", snapshot.ToolProficiencies);
        Assert.Contains("tool.vehicles_land", snapshot.ToolProficiencies);
    }

    [Fact]
    public void Resolve_WithSelectedBackground_CombinesRaceAndBackgroundLanguages()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            BackgroundId = "background.folk_hero"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Contains("language.common", snapshot.Languages);
        Assert.Contains("language.elvish", snapshot.Languages);
        Assert.Equal(2, snapshot.Languages.Count);
    }

    [Fact]
    public void Resolve_WithClassSkillsAndBackgroundSkills_CombinesAllSkillProficiencies()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            BackgroundId = "background.folk_hero",
            SelectedSkillIds =
            [
                "skill.athletics",
                "skill.perception"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Contains("skill.athletics", snapshot.SkillProficiencies);
        Assert.Contains("skill.perception", snapshot.SkillProficiencies);
        Assert.Contains("skill.animal_handling", snapshot.SkillProficiencies);
        Assert.Contains("skill.survival", snapshot.SkillProficiencies);

        Assert.Equal(4, snapshot.SkillProficiencies.Count);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Test Folk Hero",
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
            },
            SelectedSkillIds =
            [
                "skill.athletics",
                "skill.perception"
            ]
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
                new RaceDefinition
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
                }
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
                    Languages =
                    [
                        "language.elvish"
                    ],
                    FeatureId = "feature.rustic_hospitality"
                }
            ]
        };
    }
}

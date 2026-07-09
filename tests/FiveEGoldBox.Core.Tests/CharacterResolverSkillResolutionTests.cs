using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverSkillResolutionTests
{
    [Fact]
    public void Resolve_WithSelectedClassSkills_AddsSkillProficienciesToSnapshot()
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

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Contains("skill.athletics", snapshot.SkillProficiencies);
        Assert.Contains("skill.perception", snapshot.SkillProficiencies);
        Assert.Equal(2, snapshot.SkillProficiencies.Count);
    }

    [Fact]
    public void Resolve_WithoutSelectedClass_LeavesSkillProficienciesEmpty()
    {
        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = null,
            ClassId = null,
            SelectedSkillIds =
            [
                "skill.athletics",
                "skill.perception"
            ]
        };

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Empty(snapshot.SkillProficiencies);
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
            ]
        };
    }
}
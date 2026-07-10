using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverPassivePerceptionTests
{
    [Fact]
    public void Resolve_WithoutPerceptionSkill_FallsBackToWisdomModifier()
    {
        CharacterDraft draft = CreateWisdomFourteenDraft();

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(12, snapshot.PassivePerception);
    }

    [Fact]
    public void Resolve_WithPerceptionSkillButNoProficiency_UsesPerceptionSkillBonus()
    {
        CharacterDraft draft = CreateWisdomFourteenDraft() with
        {
            RaceId = "race.test"
        };

        CharacterResolver resolver = new(CreatePerceptionSkillRuleset());

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(12, snapshot.PassivePerception);
    }

    [Fact]
    public void Resolve_WithPerceptionProficiency_AddsProficiencyBonus()
    {
        CharacterDraft draft = CreateWisdomFourteenDraft() with
        {
            RaceId = "race.test",
            BackgroundId = "background.observer"
        };

        CharacterResolver resolver = new(CreatePerceptionBackgroundRuleset());

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(14, snapshot.PassivePerception);
    }

    [Fact]
    public void Resolve_WithRacialWisdomIncrease_UsesFinalWisdomModifier()
    {
        CharacterDraft draft = new()
        {
            Name = "Wise Human",
            Level = 1,
            RaceId = "race.human",
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.Manual,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 10,
                [Ability.Dexterity] = 10,
                [Ability.Constitution] = 10,
                [Ability.Intelligence] = 10,
                [Ability.Wisdom] = 15,
                [Ability.Charisma] = 10
            }
        };

        CharacterResolver resolver = new(CreateWisdomRaceRuleset());

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(13, snapshot.PassivePerception);
    }

    private static CharacterDraft CreateWisdomFourteenDraft()
    {
        return new CharacterDraft
        {
            Name = "Perceptive Character",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.Manual,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 10,
                [Ability.Dexterity] = 10,
                [Ability.Constitution] = 10,
                [Ability.Intelligence] = 10,
                [Ability.Wisdom] = 14,
                [Ability.Charisma] = 10
            }
        };
    }

    private static RulesetDefinition CreatePerceptionSkillRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.perception_skill",
            Name = "Perception Skill Ruleset",
            Races =
            [
                CreateTestRace()
            ],
            Skills =
            [
                CreatePerceptionSkill()
            ]
        };
    }

    private static RulesetDefinition CreatePerceptionBackgroundRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.perception_background",
            Name = "Perception Background Ruleset",
            Races =
            [
                CreateTestRace()
            ],
            Backgrounds =
            [
                new BackgroundDefinition
                {
                    Id = "background.observer",
                    Name = "Observer",
                    SkillProficiencies =
                    [
                        "skill.perception"
                    ]
                }
            ],
            Skills =
            [
                CreatePerceptionSkill()
            ]
        };
    }

    private static RulesetDefinition CreateWisdomRaceRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.wisdom_race",
            Name = "Wisdom Race Ruleset",
            Races =
            [
                new RaceDefinition
                {
                    Id = "race.human",
                    Name = "Human",
                    BaseSpeedFeet = 30,
                    AbilityScoreIncreases =
                    [
                        new AbilityScoreIncrease(Ability.Wisdom, 1)
                    ]
                }
            ]
        };
    }

    private static SkillDefinition CreatePerceptionSkill()
    {
        return new SkillDefinition
        {
            Id = "skill.perception",
            Name = "Perception",
            Ability = Ability.Wisdom
        };
    }
    private static RaceDefinition CreateTestRace()
    {
        return new RaceDefinition
        {
            Id = "race.test",
            Name = "Test Race",
            BaseSpeedFeet = 30
        };
    }
}
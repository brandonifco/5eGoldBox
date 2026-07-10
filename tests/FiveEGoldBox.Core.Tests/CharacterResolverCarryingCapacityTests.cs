using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverCarryingCapacityTests
{
    [Fact]
    public void Resolve_WithStrengthTen_SetsCarryingCapacityAndPushDragLift()
    {
        CharacterDraft draft = CreateDraftWithStrength(10);

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(150, snapshot.CarryingCapacityPounds);
        Assert.Equal(300, snapshot.PushDragLiftPounds);
    }

    [Fact]
    public void Resolve_WithStrengthFifteen_SetsCarryingCapacityAndPushDragLift()
    {
        CharacterDraft draft = CreateDraftWithStrength(15);

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(225, snapshot.CarryingCapacityPounds);
        Assert.Equal(450, snapshot.PushDragLiftPounds);
    }

    [Fact]
    public void Resolve_WithLowStrength_SetsCarryingCapacityAndPushDragLift()
    {
        CharacterDraft draft = CreateDraftWithStrength(8);

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(120, snapshot.CarryingCapacityPounds);
        Assert.Equal(240, snapshot.PushDragLiftPounds);
    }

    [Fact]
    public void Resolve_WithRacialStrengthIncrease_UsesFinalStrengthScore()
    {
        CharacterDraft draft = CreateDraftWithStrength(15) with
        {
            RaceId = "race.strong"
        };

        CharacterResolver resolver = new(CreateStrongRaceRuleset());

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(240, snapshot.CarryingCapacityPounds);
        Assert.Equal(480, snapshot.PushDragLiftPounds);
    }

    private static CharacterDraft CreateDraftWithStrength(int strength)
    {
        return new CharacterDraft
        {
            Name = "Carrying Character",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.Manual,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = strength,
                [Ability.Dexterity] = 10,
                [Ability.Constitution] = 10,
                [Ability.Intelligence] = 10,
                [Ability.Wisdom] = 10,
                [Ability.Charisma] = 10
            }
        };
    }

    private static RulesetDefinition CreateStrongRaceRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.strong_race",
            Name = "Strong Race Ruleset",
            Races =
            [
                new RaceDefinition
                {
                    Id = "race.strong",
                    Name = "Strong Race",
                    BaseSpeedFeet = 30,
                    AbilityScoreIncreases =
                    [
                        new AbilityScoreIncrease(Ability.Strength, 1)
                    ]
                }
            ]
        };
    }
}
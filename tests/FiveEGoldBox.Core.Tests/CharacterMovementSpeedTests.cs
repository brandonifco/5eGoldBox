using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterMovementSpeedTests
{
    [Fact]
    public void RaceDefinition_DefaultsToNoMovementSpeeds()
    {
        RaceDefinition race = new()
        {
            Id = "race.human",
            Name = "Human",
            BaseSpeedFeet = 30
        };

        Assert.Empty(race.MovementSpeeds);
    }

    [Fact]
    public void SubraceDefinition_DefaultsToNoMovementSpeeds()
    {
        SubraceDefinition subrace = new()
        {
            Id = "subrace.hill_dwarf",
            Name = "Hill Dwarf"
        };

        Assert.Empty(subrace.MovementSpeeds);
    }

    [Fact]
    public void Resolve_WithRaceBaseSpeed_AddsWalkMovementSpeed()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        CharacterMovementSpeed walk = Assert.Single(
            snapshot.MovementSpeeds,
            speed => speed.Mode == MovementMode.Walk);

        Assert.Equal(30, walk.SpeedFeet);
    }

    [Fact]
    public void Resolve_WithRaceMovementSpeed_AddsCharacterMovementSpeed()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.tabaxi"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(2, snapshot.MovementSpeeds.Count);

        CharacterMovementSpeed walk = Assert.Single(
            snapshot.MovementSpeeds,
            speed => speed.Mode == MovementMode.Walk);

        Assert.Equal(30, walk.SpeedFeet);

        CharacterMovementSpeed climb = Assert.Single(
            snapshot.MovementSpeeds,
            speed => speed.Mode == MovementMode.Climb);

        Assert.Equal(20, climb.SpeedFeet);
    }

    [Fact]
    public void Resolve_WithRaceAndSubraceMovementSpeeds_MergesSpeedsUsingHighestSpeedPerMovementMode()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.aquatic",
            SubraceId = "subrace.deep_aquatic"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(3, snapshot.MovementSpeeds.Count);

        CharacterMovementSpeed walk = Assert.Single(
            snapshot.MovementSpeeds,
            speed => speed.Mode == MovementMode.Walk);

        Assert.Equal(30, walk.SpeedFeet);

        CharacterMovementSpeed swim = Assert.Single(
            snapshot.MovementSpeeds,
            speed => speed.Mode == MovementMode.Swim);

        Assert.Equal(40, swim.SpeedFeet);

        CharacterMovementSpeed climb = Assert.Single(
            snapshot.MovementSpeeds,
            speed => speed.Mode == MovementMode.Climb);

        Assert.Equal(10, climb.SpeedFeet);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Character Movement Speed Character",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.Manual,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 10,
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
            Id = "ruleset.character_movement_speeds",
            Name = "Character Movement Speeds Ruleset",
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
                    Id = "race.tabaxi",
                    Name = "Tabaxi",
                    Size = CharacterSize.Medium,
                    BaseSpeedFeet = 30,
                    MovementSpeeds =
                    [
                        new MovementSpeedDefinition
                        {
                            Mode = MovementMode.Climb,
                            SpeedFeet = 20
                        }
                    ]
                },
                new RaceDefinition
                {
                    Id = "race.aquatic",
                    Name = "Aquatic",
                    Size = CharacterSize.Medium,
                    BaseSpeedFeet = 30,
                    MovementSpeeds =
                    [
                        new MovementSpeedDefinition
                        {
                            Mode = MovementMode.Swim,
                            SpeedFeet = 30
                        }
                    ],
                    Subraces =
                    [
                        new SubraceDefinition
                        {
                            Id = "subrace.deep_aquatic",
                            Name = "Deep Aquatic",
                            MovementSpeeds =
                            [
                                new MovementSpeedDefinition
                                {
                                    Mode = MovementMode.Swim,
                                    SpeedFeet = 40
                                },
                                new MovementSpeedDefinition
                                {
                                    Mode = MovementMode.Climb,
                                    SpeedFeet = 10
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }
}

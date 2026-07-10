using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterSenseTests
{
    [Fact]
    public void RaceDefinition_DefaultsToNoSenses()
    {
        RaceDefinition race = new()
        {
            Id = "race.human",
            Name = "Human",
            BaseSpeedFeet = 30
        };

        Assert.Empty(race.Senses);
    }

    [Fact]
    public void SubraceDefinition_DefaultsToNoSenses()
    {
        SubraceDefinition subrace = new()
        {
            Id = "subrace.hill_dwarf",
            Name = "Hill Dwarf"
        };

        Assert.Empty(subrace.Senses);
    }

    [Fact]
    public void Resolve_WithRaceSense_AddsCharacterSense()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.elf"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        CharacterSense darkvision = Assert.Single(
            snapshot.Senses,
            sense => sense.Type == SenseType.Darkvision);

        Assert.Equal(60, darkvision.RangeFeet);
    }

    [Fact]
    public void Resolve_WithNoRaceOrSubraceSenses_LeavesSensesEmpty()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Empty(snapshot.Senses);
    }

    [Fact]
    public void Resolve_WithRaceAndSubraceSenses_MergesSensesUsingHighestRangePerSenseType()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.dwarf",
            SubraceId = "subrace.deep_dwarf"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(2, snapshot.Senses.Count);

        CharacterSense darkvision = Assert.Single(
            snapshot.Senses,
            sense => sense.Type == SenseType.Darkvision);

        Assert.Equal(120, darkvision.RangeFeet);

        CharacterSense tremorsense = Assert.Single(
            snapshot.Senses,
            sense => sense.Type == SenseType.Tremorsense);

        Assert.Equal(30, tremorsense.RangeFeet);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Character Sense Character",
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
            Id = "ruleset.character_senses",
            Name = "Character Senses Ruleset",
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
                    Id = "race.elf",
                    Name = "Elf",
                    Size = CharacterSize.Medium,
                    BaseSpeedFeet = 30,
                    Senses =
                    [
                        new SenseDefinition
                        {
                            Type = SenseType.Darkvision,
                            RangeFeet = 60
                        }
                    ]
                },
                new RaceDefinition
                {
                    Id = "race.dwarf",
                    Name = "Dwarf",
                    Size = CharacterSize.Medium,
                    BaseSpeedFeet = 25,
                    Senses =
                    [
                        new SenseDefinition
                        {
                            Type = SenseType.Darkvision,
                            RangeFeet = 60
                        }
                    ],
                    Subraces =
                    [
                        new SubraceDefinition
                        {
                            Id = "subrace.deep_dwarf",
                            Name = "Deep Dwarf",
                            Senses =
                            [
                                new SenseDefinition
                                {
                                    Type = SenseType.Darkvision,
                                    RangeFeet = 120
                                },
                                new SenseDefinition
                                {
                                    Type = SenseType.Tremorsense,
                                    RangeFeet = 30
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }
}
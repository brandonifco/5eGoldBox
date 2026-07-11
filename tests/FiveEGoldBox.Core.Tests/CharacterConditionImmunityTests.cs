using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterConditionImmunityTests
{
    [Fact]
    public void RaceDefinition_DefaultsToNoConditionImmunities()
    {
        RaceDefinition race = new()
        {
            Id = "race.human",
            Name = "Human",
            BaseSpeedFeet = 30
        };

        Assert.Empty(race.ConditionImmunities);
    }

    [Fact]
    public void SubraceDefinition_DefaultsToNoConditionImmunities()
    {
        SubraceDefinition subrace = new()
        {
            Id = "subrace.hill_dwarf",
            Name = "Hill Dwarf"
        };

        Assert.Empty(subrace.ConditionImmunities);
    }

    [Fact]
    public void Resolve_WithRaceConditionImmunity_AddsCharacterConditionImmunity()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.construct"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        CharacterConditionImmunity immunity = Assert.Single(
            snapshot.ConditionImmunities,
            immunity => immunity.Condition == ConditionType.Poisoned);

        Assert.Equal(ConditionType.Poisoned, immunity.Condition);
    }

    [Fact]
    public void Resolve_WithNoRaceOrSubraceConditionImmunities_LeavesConditionImmunitiesEmpty()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Empty(snapshot.ConditionImmunities);
    }

    [Fact]
    public void Resolve_WithRaceAndSubraceConditionImmunities_MergesUniqueConditionImmunities()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.fey",
            SubraceId = "subrace.deep_fey"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(2, snapshot.ConditionImmunities.Count);

        Assert.Contains(
            snapshot.ConditionImmunities,
            immunity => immunity.Condition == ConditionType.Charmed);

        Assert.Contains(
            snapshot.ConditionImmunities,
            immunity => immunity.Condition == ConditionType.Frightened);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Character Condition Immunity Character",
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
            Id = "ruleset.character_condition_immunities",
            Name = "Character Condition Immunities Ruleset",
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
                    Id = "race.construct",
                    Name = "Construct",
                    Size = CharacterSize.Medium,
                    BaseSpeedFeet = 30,
                    ConditionImmunities =
                    [
                        new ConditionImmunityDefinition
                        {
                            Condition = ConditionType.Poisoned
                        }
                    ]
                },
                new RaceDefinition
                {
                    Id = "race.fey",
                    Name = "Fey",
                    Size = CharacterSize.Medium,
                    BaseSpeedFeet = 30,
                    ConditionImmunities =
                    [
                        new ConditionImmunityDefinition
                        {
                            Condition = ConditionType.Charmed
                        }
                    ],
                    Subraces =
                    [
                        new SubraceDefinition
                        {
                            Id = "subrace.deep_fey",
                            Name = "Deep Fey",
                            ConditionImmunities =
                            [
                                new ConditionImmunityDefinition
                                {
                                    Condition = ConditionType.Charmed
                                },
                                new ConditionImmunityDefinition
                                {
                                    Condition = ConditionType.Frightened
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }
}

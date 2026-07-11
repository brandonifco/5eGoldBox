using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterDamageResponseTests
{
    [Fact]
    public void RaceDefinition_DefaultsToNoDamageResponses()
    {
        RaceDefinition race = new()
        {
            Id = "race.human",
            Name = "Human",
            BaseSpeedFeet = 30
        };

        Assert.Empty(race.DamageResponses);
    }

    [Fact]
    public void SubraceDefinition_DefaultsToNoDamageResponses()
    {
        SubraceDefinition subrace = new()
        {
            Id = "subrace.hill_dwarf",
            Name = "Hill Dwarf"
        };

        Assert.Empty(subrace.DamageResponses);
    }

    [Fact]
    public void Resolve_WithRaceDamageResponse_AddsCharacterDamageResponse()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.tiefling"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        CharacterDamageResponse response = Assert.Single(
            snapshot.DamageResponses,
            response => response.DamageType == "damage.fire");

        Assert.Equal(DamageResponseType.Resistance, response.ResponseType);
    }

    [Fact]
    public void Resolve_WithNoRaceOrSubraceDamageResponses_LeavesDamageResponsesEmpty()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Empty(snapshot.DamageResponses);
    }

    [Fact]
    public void Resolve_WithRaceAndSubraceDamageResponses_MergesUniqueDamageResponses()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.dwarf",
            SubraceId = "subrace.deep_dwarf"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(2, snapshot.DamageResponses.Count);

        CharacterDamageResponse poisonResistance = Assert.Single(
            snapshot.DamageResponses,
            response => response.DamageType == "damage.poison");

        Assert.Equal(DamageResponseType.Resistance, poisonResistance.ResponseType);

        CharacterDamageResponse psychicResistance = Assert.Single(
            snapshot.DamageResponses,
            response => response.DamageType == "damage.psychic");

        Assert.Equal(DamageResponseType.Resistance, psychicResistance.ResponseType);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Character Damage Response Character",
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
            Id = "ruleset.character_damage_responses",
            Name = "Character Damage Responses Ruleset",
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
                    Id = "race.tiefling",
                    Name = "Tiefling",
                    Size = CharacterSize.Medium,
                    BaseSpeedFeet = 30,
                    DamageResponses =
                    [
                        new DamageResponseDefinition
                        {
                            DamageType = "damage.fire",
                            ResponseType = DamageResponseType.Resistance
                        }
                    ]
                },
                new RaceDefinition
                {
                    Id = "race.dwarf",
                    Name = "Dwarf",
                    Size = CharacterSize.Medium,
                    BaseSpeedFeet = 25,
                    DamageResponses =
                    [
                        new DamageResponseDefinition
                        {
                            DamageType = "damage.poison",
                            ResponseType = DamageResponseType.Resistance
                        }
                    ],
                    Subraces =
                    [
                        new SubraceDefinition
                        {
                            Id = "subrace.deep_dwarf",
                            Name = "Deep Dwarf",
                            DamageResponses =
                            [
                                new DamageResponseDefinition
                                {
                                    DamageType = "damage.poison",
                                    ResponseType = DamageResponseType.Resistance
                                },
                                new DamageResponseDefinition
                                {
                                    DamageType = "damage.psychic",
                                    ResponseType = DamageResponseType.Resistance
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }
}

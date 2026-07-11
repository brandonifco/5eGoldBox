using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverSubraceValidationTests
{
    [Fact]
    public void Validate_WithRaceThatHasSubracesAndValidSubrace_ReturnsValidResult()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.dwarf",
            SubraceId = "subrace.hill_dwarf"
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithRaceThatHasSubracesAndMissingSubrace_ReturnsSubraceRequiredError()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.dwarf",
            SubraceId = null
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.subrace.required");
    }

    [Fact]
    public void Validate_WithRaceThatHasSubracesAndUnknownSubrace_ReturnsSubraceNotFoundError()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.dwarf",
            SubraceId = "subrace.not_real"
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.subrace.not_found");
    }

    [Fact]
    public void Validate_WithRaceThatHasNoSubracesAndSubraceSelected_ReturnsSubraceNotAllowedError()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            SubraceId = "subrace.hill_dwarf"
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.subrace.not_allowed");
    }

    [Fact]
    public void Validate_WithoutRuleset_DoesNotRequireSubrace()
    {
        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.dwarf",
            SubraceId = null
        };

        CharacterResolver resolver = new();

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
                new RaceDefinition
                {
                    Id = "race.dwarf",
                    Name = "Dwarf",
                    BaseSpeedFeet = 25,
                    AbilityScoreIncreases =
                    [
                        new AbilityScoreIncrease(Ability.Constitution, 2)
                    ],
                    Languages =
                    [
                        "language.common",
                        "language.dwarvish"
                    ],
                    Subraces =
                    [
                        new SubraceDefinition
                        {
                            Id = "subrace.hill_dwarf",
                            Name = "Hill Dwarf",
                            AbilityScoreIncreases =
                            [
                                new AbilityScoreIncrease(Ability.Wisdom, 1)
                            ],
                            Traits =
                            [
                                "trait.dwarven_toughness"
                            ]
                        },
                        new SubraceDefinition
                        {
                            Id = "subrace.mountain_dwarf",
                            Name = "Mountain Dwarf",
                            AbilityScoreIncreases =
                            [
                                new AbilityScoreIncrease(Ability.Strength, 2)
                            ],
                            Traits =
                            [
                                "trait.dwarven_armor_training"
                            ]
                        }
                    ]
                },
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
            ]
        };
    }
}

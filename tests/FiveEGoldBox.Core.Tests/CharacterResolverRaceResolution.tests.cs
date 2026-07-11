using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverRaceResolutionTests
{
    [Fact]
    public void Resolve_WithSelectedRace_AppliesRacialAbilityScoreIncrease()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.dwarf"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(14, draft.BaseAbilityScores[Ability.Constitution]);
        Assert.Equal(16, snapshot.AbilityScores[Ability.Constitution]);
        Assert.Equal(3, snapshot.AbilityModifiers[Ability.Constitution]);
    }

    [Fact]
    public void Resolve_WithSelectedRace_SetsRaceMetadataSpeedLanguagesAndTraits()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.dwarf"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal("race.dwarf", snapshot.RaceId);
        Assert.Equal("Dwarf", snapshot.RaceName);
        Assert.Equal(25, snapshot.SpeedFeet);

        Assert.Contains("language.common", snapshot.Languages);
        Assert.Contains("language.dwarvish", snapshot.Languages);

        Assert.Contains("trait.darkvision", snapshot.Traits);
        Assert.Contains("trait.dwarven_resilience", snapshot.Traits);
    }

    [Fact]
    public void Resolve_WithoutRuleset_DoesNotApplyRaceData()
    {
        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.dwarf"
        };

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Null(snapshot.RaceId);
        Assert.Null(snapshot.RaceName);
        Assert.Null(snapshot.SpeedFeet);

        Assert.Equal(14, snapshot.AbilityScores[Ability.Constitution]);
        Assert.Empty(snapshot.Languages);
        Assert.Empty(snapshot.Traits);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Test Dwarf",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.StandardArray,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 15,
                [Ability.Dexterity] = 13,
                [Ability.Constitution] = 14,
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
                    Traits =
                    [
                        "trait.darkvision",
                        "trait.dwarven_resilience"
                    ]
                }
            ]
        };
    }
}

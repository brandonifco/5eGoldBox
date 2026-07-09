using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class RulesetDefinitionTests
{
    [Fact]
    public void RulesetDefinition_CanContainRaceDefinitions()
    {
        RaceDefinition dwarf = new()
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
            ]
        };

        RulesetDefinition ruleset = new()
        {
            Id = "ruleset.5e.2014",
            Name = "5e 2014 Compatible",
            Races =
            [
                dwarf
            ]
        };

        RaceDefinition race = Assert.Single(ruleset.Races);

        Assert.Equal("ruleset.5e.2014", ruleset.Id);
        Assert.Equal("5e 2014 Compatible", ruleset.Name);
        Assert.Equal("race.dwarf", race.Id);
        Assert.Equal("Dwarf", race.Name);
    }
}
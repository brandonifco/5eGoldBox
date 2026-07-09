using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class RaceDefinitionTests
{
    [Fact]
    public void RaceDefinition_CanRepresentDwarf()
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
            ],
            Traits =
            [
                "trait.darkvision",
                "trait.dwarven_resilience",
                "trait.dwarven_combat_training",
                "trait.tool_proficiency",
                "trait.stonecunning"
            ]
        };

        Assert.Equal("race.dwarf", dwarf.Id);
        Assert.Equal("Dwarf", dwarf.Name);
        Assert.Equal(25, dwarf.BaseSpeedFeet);

        AbilityScoreIncrease increase = Assert.Single(dwarf.AbilityScoreIncreases);
        Assert.Equal(Ability.Constitution, increase.Ability);
        Assert.Equal(2, increase.Amount);

        Assert.Contains("language.common", dwarf.Languages);
        Assert.Contains("language.dwarvish", dwarf.Languages);
        Assert.Contains("trait.darkvision", dwarf.Traits);
    }
}
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class SubraceDefinitionTests
{
    [Fact]
    public void SubraceDefinition_CanRepresentHillDwarf()
    {
        SubraceDefinition hillDwarf = new()
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
        };

        Assert.Equal("subrace.hill_dwarf", hillDwarf.Id);
        Assert.Equal("Hill Dwarf", hillDwarf.Name);

        AbilityScoreIncrease increase = Assert.Single(hillDwarf.AbilityScoreIncreases);
        Assert.Equal(Ability.Wisdom, increase.Ability);
        Assert.Equal(1, increase.Amount);

        Assert.Contains("trait.dwarven_toughness", hillDwarf.Traits);
    }

    [Fact]
    public void RaceDefinition_CanContainSubraces()
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
        };

        Assert.Equal(2, dwarf.Subraces.Count);

        Assert.Contains(
            dwarf.Subraces,
            subrace => subrace.Id == "subrace.hill_dwarf");

        Assert.Contains(
            dwarf.Subraces,
            subrace => subrace.Id == "subrace.mountain_dwarf");
    }
}
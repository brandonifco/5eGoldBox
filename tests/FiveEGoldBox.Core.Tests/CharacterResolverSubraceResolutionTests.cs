using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverSubraceResolutionTests
{
    [Fact]
    public void Resolve_WithHillDwarf_AppliesRaceAndSubraceAbilityScoreIncreases()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.dwarf",
            SubraceId = "subrace.hill_dwarf"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(14, draft.BaseAbilityScores[Ability.Constitution]);
        Assert.Equal(16, snapshot.AbilityScores[Ability.Constitution]);
        Assert.Equal(3, snapshot.AbilityModifiers[Ability.Constitution]);

        Assert.Equal(13, draft.BaseAbilityScores[Ability.Wisdom]);
        Assert.Equal(14, snapshot.AbilityScores[Ability.Wisdom]);
        Assert.Equal(2, snapshot.AbilityModifiers[Ability.Wisdom]);
    }

    [Fact]
    public void Resolve_WithMountainDwarf_AppliesDifferentSubraceAbilityScoreIncrease()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.dwarf",
            SubraceId = "subrace.mountain_dwarf"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(15, draft.BaseAbilityScores[Ability.Strength]);
        Assert.Equal(17, snapshot.AbilityScores[Ability.Strength]);
        Assert.Equal(3, snapshot.AbilityModifiers[Ability.Strength]);

        Assert.Equal(14, draft.BaseAbilityScores[Ability.Constitution]);
        Assert.Equal(16, snapshot.AbilityScores[Ability.Constitution]);
        Assert.Equal(3, snapshot.AbilityModifiers[Ability.Constitution]);
    }

    [Fact]
    public void Resolve_WithSelectedSubrace_SetsSubraceMetadata()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.dwarf",
            SubraceId = "subrace.hill_dwarf"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal("race.dwarf", snapshot.RaceId);
        Assert.Equal("Dwarf", snapshot.RaceName);

        Assert.Equal("subrace.hill_dwarf", snapshot.SubraceId);
        Assert.Equal("Hill Dwarf", snapshot.SubraceName);
    }

    [Fact]
    public void Resolve_WithSelectedSubrace_CombinesRaceAndSubraceLanguagesAndTraits()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.dwarf",
            SubraceId = "subrace.hill_dwarf"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Contains("language.common", snapshot.Languages);
        Assert.Contains("language.dwarvish", snapshot.Languages);
        Assert.Contains("language.hill_dialect", snapshot.Languages);

        Assert.Contains("trait.darkvision", snapshot.Traits);
        Assert.Contains("trait.dwarven_resilience", snapshot.Traits);
        Assert.Contains("trait.dwarven_toughness", snapshot.Traits);
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
                [Ability.Dexterity] = 12,
                [Ability.Constitution] = 14,
                [Ability.Intelligence] = 10,
                [Ability.Wisdom] = 13,
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
                            Languages =
                            [
                                "language.hill_dialect"
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
                }
            ]
        };
    }
}

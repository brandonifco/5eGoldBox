using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterSizeTests
{
    [Fact]
    public void RaceDefinition_DefaultsToMediumSize()
    {
        RaceDefinition race = new()
        {
            Id = "race.human",
            Name = "Human",
            BaseSpeedFeet = 30
        };

        Assert.Equal(CharacterSize.Medium, race.Size);
    }

    [Fact]
    public void Resolve_WithMediumRace_SetsCharacterSizeToMedium()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(CharacterSize.Medium, snapshot.Size);
    }

    [Fact]
    public void Resolve_WithSmallRace_SetsCharacterSizeToSmall()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.halfling"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(CharacterSize.Small, snapshot.Size);
    }

    [Fact]
    public void Resolve_WithoutRuleset_DefaultsCharacterSizeToMedium()
    {
        CharacterDraft draft = CreateValidDraft();

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(CharacterSize.Medium, snapshot.Size);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Character Size Character",
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
            Id = "ruleset.character_size",
            Name = "Character Size Ruleset",
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
                    Id = "race.halfling",
                    Name = "Halfling",
                    Size = CharacterSize.Small,
                    BaseSpeedFeet = 25
                }
            ]
        };
    }
}

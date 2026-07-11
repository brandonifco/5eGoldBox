using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverStandardArrayTests
{
    [Fact]
    public void Validate_WithValidStandardArrayDraft_ReturnsValidResult()
    {
        CharacterDraft draft = new()
        {
            Name = "Valid Standard Array Character",
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

        CharacterResolver resolver = new();

        var result = resolver.Validate(draft);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithValidStandardArrayInDifferentOrder_ReturnsValidResult()
    {
        CharacterDraft draft = new()
        {
            Name = "Valid Reordered Standard Array Character",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.StandardArray,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 8,
                [Ability.Dexterity] = 15,
                [Ability.Constitution] = 14,
                [Ability.Intelligence] = 13,
                [Ability.Wisdom] = 12,
                [Ability.Charisma] = 10
            }
        };

        CharacterResolver resolver = new();

        var result = resolver.Validate(draft);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithInvalidStandardArray_ReturnsStandardArrayError()
    {
        CharacterDraft draft = new()
        {
            Name = "Invalid Standard Array Character",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.StandardArray,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 15,
                [Ability.Dexterity] = 15,
                [Ability.Constitution] = 13,
                [Ability.Intelligence] = 12,
                [Ability.Wisdom] = 10,
                [Ability.Charisma] = 8
            }
        };

        CharacterResolver resolver = new();

        var result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "ability.standard_array.invalid");
    }

    [Fact]
    public void Validate_WithStandardArrayAndMissingAbility_DoesNotAddStandardArrayError()
    {
        CharacterDraft draft = new()
        {
            Name = "Incomplete Standard Array Character",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.StandardArray,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 15,
                [Ability.Dexterity] = 14,
                [Ability.Constitution] = 13,
                [Ability.Intelligence] = 12,
                [Ability.Wisdom] = 10
            }
        };

        CharacterResolver resolver = new();

        var result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "ability.charisma.missing");

        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code == "ability.standard_array.invalid");
    }
}

using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverPointBuyTests
{
    [Fact]
    public void Validate_WithValidPointBuyDraft_ReturnsValidResult()
    {
        CharacterDraft draft = new()
        {
            Name = "Valid Point Buy Character",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.PointBuy,
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
    public void Validate_WithPointBuyScoreAboveMaximum_ReturnsPointBuyRangeError()
    {
        CharacterDraft draft = new()
        {
            Name = "Invalid Point Buy Character",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.PointBuy,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 16,
                [Ability.Dexterity] = 14,
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
            issue => issue.Code == "ability.strength.point_buy_out_of_range");
    }

    [Fact]
    public void Validate_WithPointBuyTotalCostTooHigh_ReturnsTotalCostError()
    {
        CharacterDraft draft = new()
        {
            Name = "Too Expensive Point Buy Character",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.PointBuy,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 15,
                [Ability.Dexterity] = 15,
                [Ability.Constitution] = 15,
                [Ability.Intelligence] = 15,
                [Ability.Wisdom] = 15,
                [Ability.Charisma] = 15
            }
        };

        CharacterResolver resolver = new();

        var result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "ability.point_buy.total_cost_too_high");
    }
}
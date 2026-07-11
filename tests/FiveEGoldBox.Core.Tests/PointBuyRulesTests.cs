using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class PointBuyRulesTests
{
    [Theory]
    [InlineData(8, 0)]
    [InlineData(9, 1)]
    [InlineData(10, 2)]
    [InlineData(11, 3)]
    [InlineData(12, 4)]
    [InlineData(13, 5)]
    [InlineData(14, 7)]
    [InlineData(15, 9)]
    public void GetCost_ReturnsExpectedCost(int score, int expectedCost)
    {
        int actual = PointBuyRules.GetCost(score);

        Assert.Equal(expectedCost, actual);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(16)]
    public void GetCost_RejectsScoresOutsidePointBuyRange(int score)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PointBuyRules.GetCost(score));
    }

    [Fact]
    public void GetTotalCost_WithValidScores_ReturnsExpectedTotal()
    {
        Dictionary<Ability, int> scores = new()
        {
            [Ability.Strength] = 15,
            [Ability.Dexterity] = 14,
            [Ability.Constitution] = 13,
            [Ability.Intelligence] = 12,
            [Ability.Wisdom] = 10,
            [Ability.Charisma] = 8
        };

        int actual = PointBuyRules.GetTotalCost(scores);

        Assert.Equal(27, actual);
    }

    [Fact]
    public void IsValid_WithTwentySevenPointBuild_ReturnsTrue()
    {
        Dictionary<Ability, int> scores = new()
        {
            [Ability.Strength] = 15,
            [Ability.Dexterity] = 14,
            [Ability.Constitution] = 13,
            [Ability.Intelligence] = 12,
            [Ability.Wisdom] = 10,
            [Ability.Charisma] = 8
        };

        bool actual = PointBuyRules.IsValid(scores);

        Assert.True(actual);
    }

    [Fact]
    public void IsValid_WithMoreThanTwentySevenPoints_ReturnsFalse()
    {
        Dictionary<Ability, int> scores = new()
        {
            [Ability.Strength] = 15,
            [Ability.Dexterity] = 15,
            [Ability.Constitution] = 15,
            [Ability.Intelligence] = 15,
            [Ability.Wisdom] = 15,
            [Ability.Charisma] = 15
        };

        bool actual = PointBuyRules.IsValid(scores);

        Assert.False(actual);
    }

    [Fact]
    public void GetTotalCost_WithMissingAbility_ThrowsArgumentException()
    {
        Dictionary<Ability, int> scores = new()
        {
            [Ability.Strength] = 15
        };

        Assert.Throws<ArgumentException>(() => PointBuyRules.GetTotalCost(scores));
    }
}

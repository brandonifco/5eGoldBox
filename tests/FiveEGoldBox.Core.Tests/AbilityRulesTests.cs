using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class AbilityRulesTests
{
    [Theory]
    [InlineData(1, -5)]
    [InlineData(2, -4)]
    [InlineData(3, -4)]
    [InlineData(4, -3)]
    [InlineData(5, -3)]
    [InlineData(6, -2)]
    [InlineData(7, -2)]
    [InlineData(8, -1)]
    [InlineData(9, -1)]
    [InlineData(10, 0)]
    [InlineData(11, 0)]
    [InlineData(12, 1)]
    [InlineData(13, 1)]
    [InlineData(14, 2)]
    [InlineData(15, 2)]
    [InlineData(16, 3)]
    [InlineData(17, 3)]
    [InlineData(18, 4)]
    [InlineData(19, 4)]
    [InlineData(20, 5)]
    [InlineData(21, 5)]
    [InlineData(22, 6)]
    [InlineData(23, 6)]
    [InlineData(24, 7)]
    [InlineData(25, 7)]
    [InlineData(26, 8)]
    [InlineData(27, 8)]
    [InlineData(28, 9)]
    [InlineData(29, 9)]
    [InlineData(30, 10)]
    public void GetModifier_ReturnsExpectedModifier(int score, int expectedModifier)
    {
        int actual = AbilityRules.GetModifier(score);

        Assert.Equal(expectedModifier, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void GetModifier_RejectsInvalidScores(int score)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AbilityRules.GetModifier(score));
    }
}
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class AdvancementRulesTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(299, 1)]
    [InlineData(300, 2)]
    [InlineData(899, 2)]
    [InlineData(900, 3)]
    [InlineData(2_699, 3)]
    [InlineData(2_700, 4)]
    [InlineData(6_499, 4)]
    [InlineData(6_500, 5)]
    [InlineData(100_000, 5)]
    public void GetLevelForExperience_ReturnsExpectedLevel(
        int experienceTotal,
        int expectedLevel)
    {
        int actual = AdvancementRules.GetLevelForExperience(experienceTotal);

        Assert.Equal(expectedLevel, actual);
    }

    [Fact]
    public void GetLevelForExperience_RejectsNegativeTotal()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => AdvancementRules.GetLevelForExperience(-1));
    }
}

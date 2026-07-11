using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class ProficiencyRulesTests
{
    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    [InlineData(4, 2)]
    [InlineData(5, 3)]
    [InlineData(6, 3)]
    [InlineData(7, 3)]
    [InlineData(8, 3)]
    [InlineData(9, 4)]
    [InlineData(10, 4)]
    [InlineData(11, 4)]
    [InlineData(12, 4)]
    [InlineData(13, 5)]
    [InlineData(14, 5)]
    [InlineData(15, 5)]
    [InlineData(16, 5)]
    [InlineData(17, 6)]
    [InlineData(18, 6)]
    [InlineData(19, 6)]
    [InlineData(20, 6)]
    public void GetBonus_ReturnsExpectedBonus(int level, int expectedBonus)
    {
        int actual = ProficiencyRules.GetBonus(level);

        Assert.Equal(expectedBonus, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void GetBonus_RejectsInvalidLevels(int level)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProficiencyRules.GetBonus(level));
    }
}

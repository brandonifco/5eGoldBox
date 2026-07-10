using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class D20TestRulesTests
{
    [Fact]
    public void ResolveOutcome_WhenTotalMeetsDifficultyClass_ReturnsSuccess()
    {
        D20TestOutcome result = D20TestRules.ResolveOutcome(
            naturalRoll: 12,
            bonus: 5,
            difficultyClass: 17);

        Assert.Equal(D20TestOutcome.Success, result);
    }

    [Fact]
    public void ResolveOutcome_WhenTotalExceedsDifficultyClass_ReturnsSuccess()
    {
        D20TestOutcome result = D20TestRules.ResolveOutcome(
            naturalRoll: 13,
            bonus: 5,
            difficultyClass: 17);

        Assert.Equal(D20TestOutcome.Success, result);
    }

    [Fact]
    public void ResolveOutcome_WhenTotalIsBelowDifficultyClass_ReturnsFailure()
    {
        D20TestOutcome result = D20TestRules.ResolveOutcome(
            naturalRoll: 11,
            bonus: 5,
            difficultyClass: 17);

        Assert.Equal(D20TestOutcome.Failure, result);
    }

    [Fact]
    public void ResolveOutcome_WithNaturalOne_CanStillSucceed()
    {
        D20TestOutcome result = D20TestRules.ResolveOutcome(
            naturalRoll: 1,
            bonus: 99,
            difficultyClass: 20);

        Assert.Equal(D20TestOutcome.Success, result);
    }

    [Fact]
    public void ResolveOutcome_WithNaturalTwenty_CanStillFail()
    {
        D20TestOutcome result = D20TestRules.ResolveOutcome(
            naturalRoll: 20,
            bonus: -99,
            difficultyClass: 10);

        Assert.Equal(D20TestOutcome.Failure, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void ResolveOutcome_WithInvalidNaturalRoll_Throws(int naturalRoll)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            D20TestRules.ResolveOutcome(
                naturalRoll,
                bonus: 0,
                difficultyClass: 10));
    }
}
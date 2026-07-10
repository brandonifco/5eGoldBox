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
    [Fact]
    public void ResolveOutcome_WithRollModeNormal_UsesFirstRoll()
    {
        D20TestOutcome result = D20TestRules.ResolveOutcome(
            D20RollMode.Normal,
            firstRoll: 12,
            secondRoll: 1,
            bonus: 5,
            difficultyClass: 17);

        Assert.Equal(D20TestOutcome.Success, result);
    }

    [Fact]
    public void ResolveOutcome_WithRollModeAdvantage_UsesHigherRoll()
    {
        D20TestOutcome result = D20TestRules.ResolveOutcome(
            D20RollMode.Advantage,
            firstRoll: 5,
            secondRoll: 15,
            bonus: 2,
            difficultyClass: 17);

        Assert.Equal(D20TestOutcome.Success, result);
    }

    [Fact]
    public void ResolveOutcome_WithRollModeDisadvantage_UsesLowerRoll()
    {
        D20TestOutcome result = D20TestRules.ResolveOutcome(
            D20RollMode.Disadvantage,
            firstRoll: 20,
            secondRoll: 1,
            bonus: 99,
            difficultyClass: 20);

        Assert.Equal(D20TestOutcome.Success, result);
    }

    [Fact]
    public void ResolveOutcome_WithRollModeAdvantageAndMissingSecondRoll_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            D20TestRules.ResolveOutcome(
                D20RollMode.Advantage,
                firstRoll: 7,
                secondRoll: null,
                bonus: 5,
                difficultyClass: 17));
    }

    [Fact]
    public void ResolveResult_WithNormalRoll_ReturnsD20TestDetails()
    {
        D20TestResult result = D20TestRules.ResolveResult(
            D20RollMode.Normal,
            firstRoll: 12,
            secondRoll: null,
            bonus: 5,
            difficultyClass: 17);

        Assert.Equal(D20RollMode.Normal, result.RollMode);
        Assert.Equal(12, result.FirstRoll);
        Assert.Null(result.SecondRoll);
        Assert.Equal(12, result.NaturalRoll);
        Assert.Equal(5, result.Bonus);
        Assert.Equal(17, result.Total);
        Assert.Equal(17, result.DifficultyClass);
        Assert.Equal(D20TestOutcome.Success, result.Outcome);
    }

    [Fact]
    public void ResolveResult_WithAdvantage_ReturnsHigherNaturalRoll()
    {
        D20TestResult result = D20TestRules.ResolveResult(
            D20RollMode.Advantage,
            firstRoll: 5,
            secondRoll: 15,
            bonus: 2,
            difficultyClass: 17);

        Assert.Equal(D20RollMode.Advantage, result.RollMode);
        Assert.Equal(5, result.FirstRoll);
        Assert.Equal(15, result.SecondRoll);
        Assert.Equal(15, result.NaturalRoll);
        Assert.Equal(17, result.Total);
        Assert.Equal(D20TestOutcome.Success, result.Outcome);
    }

    [Fact]
    public void ResolveResult_WithDisadvantage_ReturnsLowerNaturalRoll()
    {
        D20TestResult result = D20TestRules.ResolveResult(
            D20RollMode.Disadvantage,
            firstRoll: 20,
            secondRoll: 1,
            bonus: 99,
            difficultyClass: 20);

        Assert.Equal(D20RollMode.Disadvantage, result.RollMode);
        Assert.Equal(20, result.FirstRoll);
        Assert.Equal(1, result.SecondRoll);
        Assert.Equal(1, result.NaturalRoll);
        Assert.Equal(100, result.Total);
        Assert.Equal(D20TestOutcome.Success, result.Outcome);
    }

    [Fact]
    public void ResolveResult_WithRollModeAdvantageAndMissingSecondRoll_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            D20TestRules.ResolveResult(
                D20RollMode.Advantage,
                firstRoll: 7,
                secondRoll: null,
                bonus: 5,
                difficultyClass: 17));
    }
}
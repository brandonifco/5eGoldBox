using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class D20ContestRulesTests
{
    [Fact]
    public void ResolveOutcome_WhenFirstTotalIsHigher_ReturnsFirstWins()
    {
        D20ContestOutcome result = D20ContestRules.ResolveOutcome(
            firstTotal: 18,
            secondTotal: 14);

        Assert.Equal(D20ContestOutcome.FirstWins, result);
    }

    [Fact]
    public void ResolveOutcome_WhenSecondTotalIsHigher_ReturnsSecondWins()
    {
        D20ContestOutcome result = D20ContestRules.ResolveOutcome(
            firstTotal: 11,
            secondTotal: 19);

        Assert.Equal(D20ContestOutcome.SecondWins, result);
    }

    [Fact]
    public void ResolveOutcome_WhenTotalsAreEqual_ReturnsTie()
    {
        D20ContestOutcome result = D20ContestRules.ResolveOutcome(
            firstTotal: 15,
            secondTotal: 15);

        Assert.Equal(D20ContestOutcome.Tie, result);
    }

    [Fact]
    public void ResolveContest_WhenFirstTotalIsHigher_ReturnsFirstWins()
    {
        D20ContestResult result = D20ContestRules.ResolveContest(
            firstRollMode: D20RollMode.Normal,
            firstRoll: 12,
            firstSecondRoll: null,
            firstBonus: 5,
            secondRollMode: D20RollMode.Normal,
            secondRoll: 10,
            secondSecondRoll: null,
            secondBonus: 2);

        Assert.Equal(D20ContestOutcome.FirstWins, result.Outcome);

        Assert.Equal(D20RollMode.Normal, result.FirstContestant.RollMode);
        Assert.Equal(12, result.FirstContestant.FirstRoll);
        Assert.Null(result.FirstContestant.SecondRoll);
        Assert.Equal(12, result.FirstContestant.NaturalRoll);
        Assert.Equal(5, result.FirstContestant.Bonus);
        Assert.Equal(17, result.FirstContestant.Total);

        Assert.Equal(D20RollMode.Normal, result.SecondContestant.RollMode);
        Assert.Equal(10, result.SecondContestant.FirstRoll);
        Assert.Null(result.SecondContestant.SecondRoll);
        Assert.Equal(10, result.SecondContestant.NaturalRoll);
        Assert.Equal(2, result.SecondContestant.Bonus);
        Assert.Equal(12, result.SecondContestant.Total);
    }

    [Fact]
    public void ResolveContest_WhenSecondTotalIsHigher_ReturnsSecondWins()
    {
        D20ContestResult result = D20ContestRules.ResolveContest(
            firstRollMode: D20RollMode.Normal,
            firstRoll: 8,
            firstSecondRoll: null,
            firstBonus: 3,
            secondRollMode: D20RollMode.Normal,
            secondRoll: 14,
            secondSecondRoll: null,
            secondBonus: 4);

        Assert.Equal(D20ContestOutcome.SecondWins, result.Outcome);
        Assert.Equal(11, result.FirstContestant.Total);
        Assert.Equal(18, result.SecondContestant.Total);
    }

    [Fact]
    public void ResolveContest_WhenTotalsAreEqual_ReturnsTie()
    {
        D20ContestResult result = D20ContestRules.ResolveContest(
            firstRollMode: D20RollMode.Normal,
            firstRoll: 12,
            firstSecondRoll: null,
            firstBonus: 3,
            secondRollMode: D20RollMode.Normal,
            secondRoll: 10,
            secondSecondRoll: null,
            secondBonus: 5);

        Assert.Equal(D20ContestOutcome.Tie, result.Outcome);
        Assert.Equal(15, result.FirstContestant.Total);
        Assert.Equal(15, result.SecondContestant.Total);
    }

    [Fact]
    public void ResolveContest_WithFirstContestantAdvantage_UsesHigherRoll()
    {
        D20ContestResult result = D20ContestRules.ResolveContest(
            firstRollMode: D20RollMode.Advantage,
            firstRoll: 5,
            firstSecondRoll: 15,
            firstBonus: 2,
            secondRollMode: D20RollMode.Normal,
            secondRoll: 12,
            secondSecondRoll: null,
            secondBonus: 4);

        Assert.Equal(15, result.FirstContestant.NaturalRoll);
        Assert.Equal(17, result.FirstContestant.Total);
        Assert.Equal(16, result.SecondContestant.Total);
        Assert.Equal(D20ContestOutcome.FirstWins, result.Outcome);
    }

    [Fact]
    public void ResolveContest_WithSecondContestantDisadvantage_UsesLowerRoll()
    {
        D20ContestResult result = D20ContestRules.ResolveContest(
            firstRollMode: D20RollMode.Normal,
            firstRoll: 10,
            firstSecondRoll: null,
            firstBonus: 3,
            secondRollMode: D20RollMode.Disadvantage,
            secondRoll: 20,
            secondSecondRoll: 1,
            secondBonus: 99);

        Assert.Equal(13, result.FirstContestant.Total);
        Assert.Equal(1, result.SecondContestant.NaturalRoll);
        Assert.Equal(100, result.SecondContestant.Total);
        Assert.Equal(D20ContestOutcome.SecondWins, result.Outcome);
    }

    [Fact]
    public void ResolveContest_WithFirstContestantAdvantageAndMissingSecondRoll_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            D20ContestRules.ResolveContest(
                firstRollMode: D20RollMode.Advantage,
                firstRoll: 7,
                firstSecondRoll: null,
                firstBonus: 5,
                secondRollMode: D20RollMode.Normal,
                secondRoll: 12,
                secondSecondRoll: null,
                secondBonus: 3));
    }

    [Fact]
    public void ResolveContest_WithSecondContestantDisadvantageAndMissingSecondRoll_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            D20ContestRules.ResolveContest(
                firstRollMode: D20RollMode.Normal,
                firstRoll: 12,
                firstSecondRoll: null,
                firstBonus: 3,
                secondRollMode: D20RollMode.Disadvantage,
                secondRoll: 7,
                secondSecondRoll: null,
                secondBonus: 5));
    }
}
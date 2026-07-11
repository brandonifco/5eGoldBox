using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class D20RulesTests
{
    [Fact]
    public void ResolveRollMode_WithNoAdvantageAndNoDisadvantage_ReturnsNormal()
    {
        D20RollMode result = D20Rules.ResolveRollMode(
            hasAdvantage: false,
            hasDisadvantage: false);

        Assert.Equal(D20RollMode.Normal, result);
    }

    [Fact]
    public void ResolveRollMode_WithAdvantageAndNoDisadvantage_ReturnsAdvantage()
    {
        D20RollMode result = D20Rules.ResolveRollMode(
            hasAdvantage: true,
            hasDisadvantage: false);

        Assert.Equal(D20RollMode.Advantage, result);
    }

    [Fact]
    public void ResolveRollMode_WithNoAdvantageAndDisadvantage_ReturnsDisadvantage()
    {
        D20RollMode result = D20Rules.ResolveRollMode(
            hasAdvantage: false,
            hasDisadvantage: true);

        Assert.Equal(D20RollMode.Disadvantage, result);
    }

    [Fact]
    public void ResolveRollMode_WithAdvantageAndDisadvantage_ReturnsNormal()
    {
        D20RollMode result = D20Rules.ResolveRollMode(
            hasAdvantage: true,
            hasDisadvantage: true);

        Assert.Equal(D20RollMode.Normal, result);
    }

    [Fact]
    public void ResolveNaturalRoll_WithNormalRoll_UsesFirstRoll()
    {
        int result = D20Rules.ResolveNaturalRoll(
            D20RollMode.Normal,
            firstRoll: 7,
            secondRoll: 20);

        Assert.Equal(7, result);
    }

    [Fact]
    public void ResolveNaturalRoll_WithAdvantage_UsesHigherRoll()
    {
        int result = D20Rules.ResolveNaturalRoll(
            D20RollMode.Advantage,
            firstRoll: 7,
            secondRoll: 20);

        Assert.Equal(20, result);
    }

    [Fact]
    public void ResolveNaturalRoll_WithDisadvantage_UsesLowerRoll()
    {
        int result = D20Rules.ResolveNaturalRoll(
            D20RollMode.Disadvantage,
            firstRoll: 7,
            secondRoll: 20);

        Assert.Equal(7, result);
    }

    [Fact]
    public void ResolveNaturalRoll_WithAdvantageAndMissingSecondRoll_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            D20Rules.ResolveNaturalRoll(
                D20RollMode.Advantage,
                firstRoll: 7));
    }

    [Fact]
    public void ResolveNaturalRoll_WithDisadvantageAndMissingSecondRoll_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            D20Rules.ResolveNaturalRoll(
                D20RollMode.Disadvantage,
                firstRoll: 7));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void ResolveNaturalRoll_WithInvalidFirstRoll_Throws(int firstRoll)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            D20Rules.ResolveNaturalRoll(
                D20RollMode.Normal,
                firstRoll));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void ResolveNaturalRoll_WithInvalidSecondRoll_Throws(int secondRoll)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            D20Rules.ResolveNaturalRoll(
                D20RollMode.Advantage,
                firstRoll: 10,
                secondRoll));
    }
}

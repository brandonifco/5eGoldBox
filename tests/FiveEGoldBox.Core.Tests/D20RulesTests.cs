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
}
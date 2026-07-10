using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class InitiativeRulesTests
{
    [Fact]
    public void ResolveInitiative_WithNormalRoll_ReturnsInitiativeDetails()
    {
        InitiativeRollResult result = InitiativeRules.ResolveInitiative(
            D20RollMode.Normal,
            firstRoll: 12,
            secondRoll: null,
            initiativeBonus: 3);

        Assert.Equal(D20RollMode.Normal, result.RollMode);
        Assert.Equal(12, result.FirstRoll);
        Assert.Null(result.SecondRoll);
        Assert.Equal(12, result.NaturalRoll);
        Assert.Equal(3, result.InitiativeBonus);
        Assert.Equal(15, result.Total);
    }

    [Fact]
    public void ResolveInitiative_WithAdvantage_UsesHigherRoll()
    {
        InitiativeRollResult result = InitiativeRules.ResolveInitiative(
            D20RollMode.Advantage,
            firstRoll: 5,
            secondRoll: 15,
            initiativeBonus: 2);

        Assert.Equal(D20RollMode.Advantage, result.RollMode);
        Assert.Equal(5, result.FirstRoll);
        Assert.Equal(15, result.SecondRoll);
        Assert.Equal(15, result.NaturalRoll);
        Assert.Equal(2, result.InitiativeBonus);
        Assert.Equal(17, result.Total);
    }

    [Fact]
    public void ResolveInitiative_WithDisadvantage_UsesLowerRoll()
    {
        InitiativeRollResult result = InitiativeRules.ResolveInitiative(
            D20RollMode.Disadvantage,
            firstRoll: 20,
            secondRoll: 1,
            initiativeBonus: 4);

        Assert.Equal(D20RollMode.Disadvantage, result.RollMode);
        Assert.Equal(20, result.FirstRoll);
        Assert.Equal(1, result.SecondRoll);
        Assert.Equal(1, result.NaturalRoll);
        Assert.Equal(4, result.InitiativeBonus);
        Assert.Equal(5, result.Total);
    }

    [Fact]
    public void ResolveInitiative_WithNegativeInitiativeBonus_ReturnsReducedTotal()
    {
        InitiativeRollResult result = InitiativeRules.ResolveInitiative(
            D20RollMode.Normal,
            firstRoll: 8,
            secondRoll: null,
            initiativeBonus: -1);

        Assert.Equal(8, result.NaturalRoll);
        Assert.Equal(-1, result.InitiativeBonus);
        Assert.Equal(7, result.Total);
    }

    [Fact]
    public void ResolveInitiative_WithAdvantageAndMissingSecondRoll_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            InitiativeRules.ResolveInitiative(
                D20RollMode.Advantage,
                firstRoll: 7,
                secondRoll: null,
                initiativeBonus: 3));
    }
}
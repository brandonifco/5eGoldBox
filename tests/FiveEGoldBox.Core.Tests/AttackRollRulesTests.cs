using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class AttackRollRulesTests
{
    [Fact]
    public void ResolveOutcome_WithNaturalOne_ReturnsMissEvenIfTotalWouldHit()
    {
        AttackRollOutcome result = AttackRollRules.ResolveOutcome(
            naturalRoll: 1,
            attackBonus: 99,
            targetArmorClass: 10);

        Assert.Equal(AttackRollOutcome.Miss, result);
    }

    [Fact]
    public void ResolveOutcome_WithNaturalTwenty_ReturnsCriticalHitEvenIfTotalWouldMiss()
    {
        AttackRollOutcome result = AttackRollRules.ResolveOutcome(
            naturalRoll: 20,
            attackBonus: -99,
            targetArmorClass: 30);

        Assert.Equal(AttackRollOutcome.CriticalHit, result);
    }

    [Fact]
    public void ResolveOutcome_WhenTotalMeetsArmorClass_ReturnsHit()
    {
        AttackRollOutcome result = AttackRollRules.ResolveOutcome(
            naturalRoll: 12,
            attackBonus: 5,
            targetArmorClass: 17);

        Assert.Equal(AttackRollOutcome.Hit, result);
    }

    [Fact]
    public void ResolveOutcome_WhenTotalExceedsArmorClass_ReturnsHit()
    {
        AttackRollOutcome result = AttackRollRules.ResolveOutcome(
            naturalRoll: 13,
            attackBonus: 5,
            targetArmorClass: 17);

        Assert.Equal(AttackRollOutcome.Hit, result);
    }

    [Fact]
    public void ResolveOutcome_WhenTotalIsBelowArmorClass_ReturnsMiss()
    {
        AttackRollOutcome result = AttackRollRules.ResolveOutcome(
            naturalRoll: 11,
            attackBonus: 5,
            targetArmorClass: 17);

        Assert.Equal(AttackRollOutcome.Miss, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void ResolveOutcome_WithInvalidNaturalRoll_Throws(int naturalRoll)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AttackRollRules.ResolveOutcome(
                naturalRoll,
                attackBonus: 0,
                targetArmorClass: 10));
    }

    [Fact]
    public void ResolveOutcome_WithRollModeNormal_UsesFirstRoll()
    {
        AttackRollOutcome result = AttackRollRules.ResolveOutcome(
            D20RollMode.Normal,
            firstRoll: 12,
            secondRoll: 20,
            attackBonus: 5,
            targetArmorClass: 17);

        Assert.Equal(AttackRollOutcome.Hit, result);
    }

    [Fact]
    public void ResolveOutcome_WithRollModeAdvantage_UsesHigherRoll()
    {
        AttackRollOutcome result = AttackRollRules.ResolveOutcome(
            D20RollMode.Advantage,
            firstRoll: 7,
            secondRoll: 20,
            attackBonus: 0,
            targetArmorClass: 30);

        Assert.Equal(AttackRollOutcome.CriticalHit, result);
    }

    [Fact]
    public void ResolveOutcome_WithRollModeDisadvantage_UsesLowerRoll()
    {
        AttackRollOutcome result = AttackRollRules.ResolveOutcome(
            D20RollMode.Disadvantage,
            firstRoll: 20,
            secondRoll: 1,
            attackBonus: 99,
            targetArmorClass: 10);

        Assert.Equal(AttackRollOutcome.Miss, result);
    }

    [Fact]
    public void ResolveOutcome_WithRollModeAdvantageAndMissingSecondRoll_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            AttackRollRules.ResolveOutcome(
                D20RollMode.Advantage,
                firstRoll: 7,
                secondRoll: null,
                attackBonus: 5,
                targetArmorClass: 17));
    }

    [Fact]
    public void ResolveResult_WithNormalRoll_ReturnsAttackRollDetails()
    {
        AttackRollResult result = AttackRollRules.ResolveResult(
            D20RollMode.Normal,
            firstRoll: 12,
            secondRoll: null,
            attackBonus: 5,
            targetArmorClass: 17);

        Assert.Equal(D20RollMode.Normal, result.RollMode);
        Assert.Equal(12, result.FirstRoll);
        Assert.Null(result.SecondRoll);
        Assert.Equal(12, result.NaturalRoll);
        Assert.Equal(5, result.AttackBonus);
        Assert.Equal(17, result.Total);
        Assert.Equal(17, result.TargetArmorClass);
        Assert.Equal(AttackRollOutcome.Hit, result.Outcome);
    }

    [Fact]
    public void ResolveResult_WithAdvantage_ReturnsHigherNaturalRoll()
    {
        AttackRollResult result = AttackRollRules.ResolveResult(
            D20RollMode.Advantage,
            firstRoll: 7,
            secondRoll: 20,
            attackBonus: 0,
            targetArmorClass: 30);

        Assert.Equal(D20RollMode.Advantage, result.RollMode);
        Assert.Equal(7, result.FirstRoll);
        Assert.Equal(20, result.SecondRoll);
        Assert.Equal(20, result.NaturalRoll);
        Assert.Equal(20, result.Total);
        Assert.Equal(AttackRollOutcome.CriticalHit, result.Outcome);
    }

    [Fact]
    public void ResolveResult_WithDisadvantage_ReturnsLowerNaturalRoll()
    {
        AttackRollResult result = AttackRollRules.ResolveResult(
            D20RollMode.Disadvantage,
            firstRoll: 20,
            secondRoll: 1,
            attackBonus: 99,
            targetArmorClass: 10);

        Assert.Equal(D20RollMode.Disadvantage, result.RollMode);
        Assert.Equal(20, result.FirstRoll);
        Assert.Equal(1, result.SecondRoll);
        Assert.Equal(1, result.NaturalRoll);
        Assert.Equal(100, result.Total);
        Assert.Equal(AttackRollOutcome.Miss, result.Outcome);
    }

    [Fact]
    public void ResolveResult_WithRollModeAdvantageAndMissingSecondRoll_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            AttackRollRules.ResolveResult(
                D20RollMode.Advantage,
                firstRoll: 7,
                secondRoll: null,
                attackBonus: 5,
                targetArmorClass: 17));
    }
}
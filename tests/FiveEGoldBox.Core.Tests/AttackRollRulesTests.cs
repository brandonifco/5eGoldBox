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
}
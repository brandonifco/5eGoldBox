using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class ZeroHitPointDamageRulesTests
{
    [Fact]
    public void ResolveDamage_WithZeroDamage_ReturnsNoEffect()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create() with
            {
                IsStable = true
            };

        ZeroHitPointDamageResult result =
            ZeroHitPointDamageRules.ResolveDamage(
                state,
                maximumHitPoints: 20,
                damageAmount: 0,
                isCriticalHit: true);

        Assert.Equal(20, result.MaximumHitPoints);
        Assert.Equal(0, result.DamageAmount);
        Assert.True(result.IsCriticalHit);
        Assert.Equal(0, result.DeathSavingThrowFailuresCaused);
        Assert.Equal(
            ZeroHitPointDamageOutcome.NoEffect,
            result.Outcome);
        Assert.Same(state, result.State);
        Assert.True(result.State.IsStable);
        Assert.False(result.IsDead);
    }

    [Fact]
    public void ResolveDamage_WithNormalDamage_AddsOneFailure()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create();

        ZeroHitPointDamageResult result =
            ZeroHitPointDamageRules.ResolveDamage(
                state,
                maximumHitPoints: 20,
                damageAmount: 5,
                isCriticalHit: false);

        Assert.Equal(1, result.DeathSavingThrowFailuresCaused);
        Assert.Equal(
            ZeroHitPointDamageOutcome.DeathSavingThrowFailure,
            result.Outcome);
        Assert.Equal(0, result.State.SuccessCount);
        Assert.Equal(1, result.State.FailureCount);
        Assert.False(result.State.IsStable);
        Assert.False(result.State.IsDead);
        Assert.False(result.IsDead);
    }

    [Fact]
    public void ResolveDamage_WithCriticalHit_AddsTwoFailures()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create();

        ZeroHitPointDamageResult result =
            ZeroHitPointDamageRules.ResolveDamage(
                state,
                maximumHitPoints: 20,
                damageAmount: 5,
                isCriticalHit: true);

        Assert.Equal(2, result.DeathSavingThrowFailuresCaused);
        Assert.Equal(
            ZeroHitPointDamageOutcome.DeathSavingThrowFailure,
            result.Outcome);
        Assert.Equal(2, result.State.FailureCount);
        Assert.False(result.State.IsDead);
        Assert.False(result.IsDead);
    }

    [Fact]
    public void ResolveDamage_WithNormalDamageAndTwoExistingFailures_Dies()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create() with
            {
                SuccessCount = 1,
                FailureCount = 2
            };

        ZeroHitPointDamageResult result =
            ZeroHitPointDamageRules.ResolveDamage(
                state,
                maximumHitPoints: 20,
                damageAmount: 5,
                isCriticalHit: false);

        Assert.Equal(1, result.DeathSavingThrowFailuresCaused);
        Assert.Equal(
            ZeroHitPointDamageOutcome.Dead,
            result.Outcome);
        Assert.Equal(1, result.State.SuccessCount);
        Assert.Equal(3, result.State.FailureCount);
        Assert.True(result.State.IsDead);
        Assert.True(result.IsDead);
    }

    [Fact]
    public void ResolveDamage_WithCriticalHitAndOneExistingFailure_Dies()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create() with
            {
                FailureCount = 1
            };

        ZeroHitPointDamageResult result =
            ZeroHitPointDamageRules.ResolveDamage(
                state,
                maximumHitPoints: 20,
                damageAmount: 5,
                isCriticalHit: true);

        Assert.Equal(2, result.DeathSavingThrowFailuresCaused);
        Assert.Equal(
            ZeroHitPointDamageOutcome.Dead,
            result.Outcome);
        Assert.Equal(3, result.State.FailureCount);
        Assert.True(result.IsDead);
    }

    [Fact]
    public void ResolveDamage_WithCriticalHitAndTwoExistingFailures_ReportsOneAppliedFailure()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create() with
            {
                FailureCount = 2
            };

        ZeroHitPointDamageResult result =
            ZeroHitPointDamageRules.ResolveDamage(
                state,
                maximumHitPoints: 20,
                damageAmount: 5,
                isCriticalHit: true);

        Assert.Equal(1, result.DeathSavingThrowFailuresCaused);
        Assert.Equal(
            ZeroHitPointDamageOutcome.Dead,
            result.Outcome);
        Assert.Equal(3, result.State.FailureCount);
        Assert.True(result.IsDead);
    }

    [Fact]
    public void ResolveDamage_ToStableCreature_MakesCreatureUnstable()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create() with
            {
                IsStable = true
            };

        ZeroHitPointDamageResult result =
            ZeroHitPointDamageRules.ResolveDamage(
                state,
                maximumHitPoints: 20,
                damageAmount: 5,
                isCriticalHit: false);

        Assert.Equal(
            ZeroHitPointDamageOutcome.DeathSavingThrowFailure,
            result.Outcome);
        Assert.Equal(1, result.State.FailureCount);
        Assert.False(result.State.IsStable);
        Assert.False(result.IsDead);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(25)]
    public void ResolveDamage_WithDamageAtLeastMaximumHitPoints_CausesInstantDeath(
        int damageAmount)
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create() with
            {
                SuccessCount = 1,
                FailureCount = 2,
                IsStable = false
            };

        ZeroHitPointDamageResult result =
            ZeroHitPointDamageRules.ResolveDamage(
                state,
                maximumHitPoints: 20,
                damageAmount: damageAmount,
                isCriticalHit: false);

        Assert.Equal(0, result.DeathSavingThrowFailuresCaused);
        Assert.Equal(
            ZeroHitPointDamageOutcome.InstantDeath,
            result.Outcome);
        Assert.Equal(1, result.State.SuccessCount);
        Assert.Equal(2, result.State.FailureCount);
        Assert.False(result.State.IsStable);
        Assert.False(result.State.IsDead);
        Assert.True(result.IsDead);
    }

    [Fact]
    public void ResolveDamage_WithInstantDeath_MakesStableCreatureUnstable()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create() with
            {
                IsStable = true
            };

        ZeroHitPointDamageResult result =
            ZeroHitPointDamageRules.ResolveDamage(
                state,
                maximumHitPoints: 20,
                damageAmount: 20,
                isCriticalHit: true);

        Assert.Equal(
            ZeroHitPointDamageOutcome.InstantDeath,
            result.Outcome);
        Assert.False(result.State.IsStable);
        Assert.Equal(0, result.State.FailureCount);
        Assert.True(result.IsDead);
    }

    [Fact]
    public void ResolveDamage_WithNullState_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ZeroHitPointDamageRules.ResolveDamage(
                null!,
                maximumHitPoints: 20,
                damageAmount: 5,
                isCriticalHit: false));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ResolveDamage_WithInvalidMaximumHitPoints_Throws(
        int maximumHitPoints)
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ZeroHitPointDamageRules.ResolveDamage(
                state,
                maximumHitPoints,
                damageAmount: 5,
                isCriticalHit: false));
    }

    [Fact]
    public void ResolveDamage_WithNegativeDamage_Throws()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ZeroHitPointDamageRules.ResolveDamage(
                state,
                maximumHitPoints: 20,
                damageAmount: -1,
                isCriticalHit: false));
    }

    [Fact]
    public void ResolveDamage_WithInvalidDeathSavingThrowState_Throws()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create() with
            {
                SuccessCount = 3
            };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ZeroHitPointDamageRules.ResolveDamage(
                state,
                maximumHitPoints: 20,
                damageAmount: 5,
                isCriticalHit: false));
    }

    [Fact]
    public void ResolveDamage_WithDeadState_Throws()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create() with
            {
                FailureCount = 3
            };

        Assert.Throws<InvalidOperationException>(() =>
            ZeroHitPointDamageRules.ResolveDamage(
                state,
                maximumHitPoints: 20,
                damageAmount: 5,
                isCriticalHit: false));
    }
}

using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class DeathSavingThrowRulesTests
{
    [Fact]
    public void Create_ReturnsEmptyActiveState()
    {
        DeathSavingThrowState result =
            DeathSavingThrowRules.Create();

        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.False(result.IsStable);
        Assert.False(result.IsDead);
    }

    [Fact]
    public void ResolveDeathSavingThrow_WhenTotalMeetsDifficultyClass_AddsSuccess()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create();

        DeathSavingThrowResult result =
            DeathSavingThrowRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 8,
                secondRoll: null,
                savingThrowBonus: 2);

        Assert.Equal(D20RollMode.Normal, result.RollMode);
        Assert.Equal(8, result.FirstRoll);
        Assert.Null(result.SecondRoll);
        Assert.Equal(8, result.NaturalRoll);
        Assert.Equal(2, result.SavingThrowBonus);
        Assert.Equal(10, result.Total);
        Assert.Equal(10, result.DifficultyClass);
        Assert.Equal(
            DeathSavingThrowOutcome.Success,
            result.Outcome);
        Assert.Equal(1, result.State.SuccessCount);
        Assert.Equal(0, result.State.FailureCount);
        Assert.False(result.State.IsStable);
        Assert.False(result.State.IsDead);
    }

    [Fact]
    public void ResolveDeathSavingThrow_WhenTotalIsBelowDifficultyClass_AddsFailure()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create();

        DeathSavingThrowResult result =
            DeathSavingThrowRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 7,
                secondRoll: null,
                savingThrowBonus: 2);

        Assert.Equal(9, result.Total);
        Assert.Equal(
            DeathSavingThrowOutcome.Failure,
            result.Outcome);
        Assert.Equal(0, result.State.SuccessCount);
        Assert.Equal(1, result.State.FailureCount);
        Assert.False(result.State.IsStable);
        Assert.False(result.State.IsDead);
    }

    [Fact]
    public void ResolveDeathSavingThrow_WithNaturalOne_AddsTwoFailuresRegardlessOfBonus()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create();

        DeathSavingThrowResult result =
            DeathSavingThrowRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 1,
                secondRoll: null,
                savingThrowBonus: 100);

        Assert.Equal(101, result.Total);
        Assert.Equal(
            DeathSavingThrowOutcome.Failure,
            result.Outcome);
        Assert.Equal(2, result.State.FailureCount);
        Assert.False(result.State.IsDead);
    }

    [Fact]
    public void ResolveDeathSavingThrow_WithNaturalOneAndExistingFailure_Dies()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create() with
            {
                FailureCount = 1
            };

        DeathSavingThrowResult result =
            DeathSavingThrowRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 1,
                secondRoll: null,
                savingThrowBonus: 0);

        Assert.Equal(
            DeathSavingThrowOutcome.Dead,
            result.Outcome);
        Assert.Equal(3, result.State.FailureCount);
        Assert.True(result.State.IsDead);
    }

    [Fact]
    public void ResolveDeathSavingThrow_WithThirdSuccess_StabilizesAndResetsCounts()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create() with
            {
                SuccessCount = 2,
                FailureCount = 1
            };

        DeathSavingThrowResult result =
            DeathSavingThrowRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 10,
                secondRoll: null,
                savingThrowBonus: 0);

        Assert.Equal(
            DeathSavingThrowOutcome.Stabilized,
            result.Outcome);
        Assert.Equal(0, result.State.SuccessCount);
        Assert.Equal(0, result.State.FailureCount);
        Assert.True(result.State.IsStable);
        Assert.False(result.State.IsDead);
    }

    [Fact]
    public void ResolveDeathSavingThrow_WithThirdFailure_Dies()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create() with
            {
                SuccessCount = 2,
                FailureCount = 2
            };

        DeathSavingThrowResult result =
            DeathSavingThrowRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 9,
                secondRoll: null,
                savingThrowBonus: 0);

        Assert.Equal(
            DeathSavingThrowOutcome.Dead,
            result.Outcome);
        Assert.Equal(2, result.State.SuccessCount);
        Assert.Equal(3, result.State.FailureCount);
        Assert.False(result.State.IsStable);
        Assert.True(result.State.IsDead);
    }

    [Fact]
    public void ResolveDeathSavingThrow_WithNaturalTwenty_RegainsHitPointAndResetsState()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create() with
            {
                SuccessCount = 2,
                FailureCount = 2
            };

        DeathSavingThrowResult result =
            DeathSavingThrowRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 20,
                secondRoll: null,
                savingThrowBonus: -100);

        Assert.Equal(-80, result.Total);
        Assert.Equal(
            DeathSavingThrowOutcome.RegainedHitPoint,
            result.Outcome);
        Assert.Equal(0, result.State.SuccessCount);
        Assert.Equal(0, result.State.FailureCount);
        Assert.False(result.State.IsStable);
        Assert.False(result.State.IsDead);
    }

    [Fact]
    public void ResolveDeathSavingThrow_WithAdvantage_UsesHigherRoll()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create();

        DeathSavingThrowResult result =
            DeathSavingThrowRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Advantage,
                firstRoll: 4,
                secondRoll: 14,
                savingThrowBonus: 0);

        Assert.Equal(14, result.NaturalRoll);
        Assert.Equal(
            DeathSavingThrowOutcome.Success,
            result.Outcome);
        Assert.Equal(1, result.State.SuccessCount);
    }

    [Fact]
    public void ResolveDeathSavingThrow_WithDisadvantage_UsesLowerRoll()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create();

        DeathSavingThrowResult result =
            DeathSavingThrowRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Disadvantage,
                firstRoll: 20,
                secondRoll: 1,
                savingThrowBonus: 100);

        Assert.Equal(1, result.NaturalRoll);
        Assert.Equal(
            DeathSavingThrowOutcome.Failure,
            result.Outcome);
        Assert.Equal(2, result.State.FailureCount);
    }

    [Fact]
    public void ResolveDeathSavingThrow_WithAdvantageAndMissingSecondRoll_Throws()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create();

        Assert.Throws<ArgumentException>(() =>
            DeathSavingThrowRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Advantage,
                firstRoll: 10,
                secondRoll: null,
                savingThrowBonus: 0));
    }

    [Fact]
    public void ResolveDeathSavingThrow_WithStableState_Throws()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create() with
            {
                IsStable = true
            };

        Assert.Throws<InvalidOperationException>(() =>
            DeathSavingThrowRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 10,
                secondRoll: null,
                savingThrowBonus: 0));
    }

    [Fact]
    public void ResolveDeathSavingThrow_WithDeadState_Throws()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create() with
            {
                FailureCount = 3,
            };

        Assert.Throws<InvalidOperationException>(() =>
            DeathSavingThrowRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 10,
                secondRoll: null,
                savingThrowBonus: 0));
    }

    [Fact]
    public void ResolveDeathSavingThrow_WithNullState_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DeathSavingThrowRules.ResolveDeathSavingThrow(
                null!,
                D20RollMode.Normal,
                firstRoll: 10,
                secondRoll: null,
                savingThrowBonus: 0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void ResolveDeathSavingThrow_WithInvalidSuccessCount_Throws(
        int successCount)
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create() with
            {
                SuccessCount = successCount
            };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DeathSavingThrowRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 10,
                secondRoll: null,
                savingThrowBonus: 0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void ResolveDeathSavingThrow_WithInvalidFailureCount_Throws(
        int failureCount)
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create() with
            {
                FailureCount = failureCount
            };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DeathSavingThrowRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 10,
                secondRoll: null,
                savingThrowBonus: 0));
    }

    [Fact]
    public void ResolveDeathSavingThrow_WithStableAndDeadState_Throws()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create() with
            {
                FailureCount = 3,
                IsStable = true
            };

        Assert.Throws<ArgumentException>(() =>
            DeathSavingThrowRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 10,
                secondRoll: null,
                savingThrowBonus: 0));
    }

    [Fact]
    public void ResolveDeathSavingThrow_WithStableStateContainingCounts_Throws()
    {
        DeathSavingThrowState state =
            DeathSavingThrowRules.Create() with
            {
                SuccessCount = 1,
                IsStable = true
            };

        Assert.Throws<ArgumentException>(() =>
            DeathSavingThrowRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 10,
                secondRoll: null,
                savingThrowBonus: 0));
    }
}

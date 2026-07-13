using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class HitPointRulesTests
{
    [Fact]
    public void Create_WithMaximumHitPoints_ReturnsFullHitPointState()
    {
        HitPointState result = HitPointRules.Create(
            maximumHitPoints: 12);

        Assert.Equal(12, result.MaximumHitPoints);
        Assert.Equal(12, result.CurrentHitPoints);
        Assert.Equal(0, result.TemporaryHitPoints);
        Assert.False(result.IsAtZeroHitPoints);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidMaximumHitPoints_Throws(int maximumHitPoints)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HitPointRules.Create(maximumHitPoints));
    }

    [Fact]
    public void ApplyDamage_WithZeroDamage_ReturnsSameState()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20);

        HitPointState result = HitPointRules.ApplyDamage(
            state,
            damageAmount: 0);

        Assert.Same(state, result);
    }

    [Fact]
    public void ApplyDamage_WhenTemporaryHitPointsAbsorbAllDamage_ReducesTemporaryHitPointsOnly()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20) with
        {
            TemporaryHitPoints = 8
        };

        HitPointState result = HitPointRules.ApplyDamage(
            state,
            damageAmount: 5);

        Assert.Equal(20, result.MaximumHitPoints);
        Assert.Equal(20, result.CurrentHitPoints);
        Assert.Equal(3, result.TemporaryHitPoints);
        Assert.False(result.IsAtZeroHitPoints);
    }

    [Fact]
    public void ApplyDamage_WhenDamageExceedsTemporaryHitPoints_ReducesTemporaryHitPointsThenCurrentHitPoints()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20) with
        {
            TemporaryHitPoints = 5
        };

        HitPointState result = HitPointRules.ApplyDamage(
            state,
            damageAmount: 12);

        Assert.Equal(20, result.MaximumHitPoints);
        Assert.Equal(13, result.CurrentHitPoints);
        Assert.Equal(0, result.TemporaryHitPoints);
    }

    [Fact]
    public void ApplyDamage_WhenNoTemporaryHitPoints_ReducesCurrentHitPoints()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20);

        HitPointState result = HitPointRules.ApplyDamage(
            state,
            damageAmount: 7);

        Assert.Equal(13, result.CurrentHitPoints);
        Assert.Equal(0, result.TemporaryHitPoints);
    }

    [Fact]
    public void ApplyDamage_WhenDamageExceedsCurrentHitPoints_DoesNotReduceBelowZero()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20) with
        {
            CurrentHitPoints = 6
        };

        HitPointState result = HitPointRules.ApplyDamage(
            state,
            damageAmount: 10);

        Assert.Equal(0, result.CurrentHitPoints);
        Assert.Equal(0, result.TemporaryHitPoints);
        Assert.True(result.IsAtZeroHitPoints);
    }

    [Fact]
    public void ApplyDamage_WithNegativeDamage_Throws()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HitPointRules.ApplyDamage(
                state,
                damageAmount: -1));
    }

    [Fact]
    public void ApplyDamage_WithCurrentHitPointsGreaterThanMaximumHitPoints_Throws()
    {
        HitPointState state = new()
        {
            MaximumHitPoints = 20,
            CurrentHitPoints = 21,
            TemporaryHitPoints = 0
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HitPointRules.ApplyDamage(
                state,
                damageAmount: 1));
    }

    [Fact]
    public void ApplyDamage_WithNegativeTemporaryHitPoints_Throws()
    {
        HitPointState state = new()
        {
            MaximumHitPoints = 20,
            CurrentHitPoints = 20,
            TemporaryHitPoints = -1
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HitPointRules.ApplyDamage(
                state,
                damageAmount: 1));
    }

    [Fact]
    public void ApplyHealing_WithHealingAmount_IncreasesCurrentHitPoints()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20) with
        {
            CurrentHitPoints = 8
        };

        HitPointState result = HitPointRules.ApplyHealing(
            state,
            healingAmount: 5);

        Assert.Equal(13, result.CurrentHitPoints);
        Assert.Equal(0, result.TemporaryHitPoints);
    }

    [Fact]
    public void ApplyHealing_WhenHealingExceedsMissingHitPoints_CapsAtMaximumHitPoints()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20) with
        {
            CurrentHitPoints = 8
        };

        HitPointState result = HitPointRules.ApplyHealing(
            state,
            healingAmount: 50);

        Assert.Equal(20, result.CurrentHitPoints);
        Assert.False(result.IsAtZeroHitPoints);
    }

    [Fact]
    public void ApplyHealing_DoesNotChangeTemporaryHitPoints()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20) with
        {
            CurrentHitPoints = 8,
            TemporaryHitPoints = 6
        };

        HitPointState result = HitPointRules.ApplyHealing(
            state,
            healingAmount: 5);

        Assert.Equal(13, result.CurrentHitPoints);
        Assert.Equal(6, result.TemporaryHitPoints);
    }

    [Fact]
    public void ApplyHealing_WithZeroHealing_ReturnsSameState()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20) with
        {
            CurrentHitPoints = 8
        };

        HitPointState result = HitPointRules.ApplyHealing(
            state,
            healingAmount: 0);

        Assert.Same(state, result);
    }

    [Fact]
    public void ApplyHealing_WithNegativeHealing_Throws()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HitPointRules.ApplyHealing(
                state,
                healingAmount: -1));
    }

    [Fact]
    public void ApplyTemporaryHitPoints_WhenNewAmountIsHigher_ReplacesTemporaryHitPoints()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20) with
        {
            TemporaryHitPoints = 3
        };

        HitPointState result = HitPointRules.ApplyTemporaryHitPoints(
            state,
            temporaryHitPoints: 8);

        Assert.Equal(20, result.CurrentHitPoints);
        Assert.Equal(8, result.TemporaryHitPoints);
    }

    [Fact]
    public void ApplyTemporaryHitPoints_WhenNewAmountIsEqual_ReturnsSameState()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20) with
        {
            TemporaryHitPoints = 5
        };

        HitPointState result = HitPointRules.ApplyTemporaryHitPoints(
            state,
            temporaryHitPoints: 5);

        Assert.Same(state, result);
    }

    [Fact]
    public void ApplyTemporaryHitPoints_WhenNewAmountIsLower_ReturnsSameState()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20) with
        {
            TemporaryHitPoints = 5
        };

        HitPointState result = HitPointRules.ApplyTemporaryHitPoints(
            state,
            temporaryHitPoints: 3);

        Assert.Same(state, result);
    }

    [Fact]
    public void ApplyTemporaryHitPoints_WithNegativeTemporaryHitPoints_Throws()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HitPointRules.ApplyTemporaryHitPoints(
                state,
                temporaryHitPoints: -1));
    }
    [Fact]
    public void ResolveDamage_WithZeroDamage_ReturnsDetailedUnchangedResult()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20);

        HitPointDamageResult result =
            HitPointRules.ResolveDamage(
                state,
                damageAmount: 0);

        Assert.Equal(0, result.DamageAmount);
        Assert.Equal(
            0,
            result.DamageAbsorbedByTemporaryHitPoints);
        Assert.Equal(
            0,
            result.DamageAppliedToCurrentHitPoints);
        Assert.Equal(
            0,
            result.DamageRemainingAfterReachingZeroHitPoints);
        Assert.False(result.StartedAtZeroHitPoints);
        Assert.False(result.ReducedToZeroHitPoints);
        Assert.Same(state, result.State);
    }

    [Fact]
    public void ResolveDamage_WhenTemporaryHitPointsAbsorbAllDamage_ReportsAbsorbedDamage()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20) with
        {
            TemporaryHitPoints = 8
        };

        HitPointDamageResult result =
            HitPointRules.ResolveDamage(
                state,
                damageAmount: 5);

        Assert.Equal(5, result.DamageAmount);
        Assert.Equal(
            5,
            result.DamageAbsorbedByTemporaryHitPoints);
        Assert.Equal(
            0,
            result.DamageAppliedToCurrentHitPoints);
        Assert.Equal(
            0,
            result.DamageRemainingAfterReachingZeroHitPoints);
        Assert.False(result.StartedAtZeroHitPoints);
        Assert.False(result.ReducedToZeroHitPoints);
        Assert.Equal(20, result.State.CurrentHitPoints);
        Assert.Equal(3, result.State.TemporaryHitPoints);
    }

    [Fact]
    public void ResolveDamage_WhenDamageExceedsTemporaryHitPoints_ReportsCurrentHitPointDamage()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20) with
        {
            TemporaryHitPoints = 5
        };

        HitPointDamageResult result =
            HitPointRules.ResolveDamage(
                state,
                damageAmount: 12);

        Assert.Equal(
            5,
            result.DamageAbsorbedByTemporaryHitPoints);
        Assert.Equal(
            7,
            result.DamageAppliedToCurrentHitPoints);
        Assert.Equal(
            0,
            result.DamageRemainingAfterReachingZeroHitPoints);
        Assert.False(result.StartedAtZeroHitPoints);
        Assert.False(result.ReducedToZeroHitPoints);
        Assert.Equal(13, result.State.CurrentHitPoints);
        Assert.Equal(0, result.State.TemporaryHitPoints);
    }

    [Fact]
    public void ResolveDamage_WhenDamageExceedsCurrentHitPoints_ReportsRemainingDamage()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20) with
        {
            CurrentHitPoints = 6,
            TemporaryHitPoints = 2
        };

        HitPointDamageResult result =
            HitPointRules.ResolveDamage(
                state,
                damageAmount: 10);

        Assert.Equal(
            2,
            result.DamageAbsorbedByTemporaryHitPoints);
        Assert.Equal(
            6,
            result.DamageAppliedToCurrentHitPoints);
        Assert.Equal(
            2,
            result.DamageRemainingAfterReachingZeroHitPoints);
        Assert.False(result.StartedAtZeroHitPoints);
        Assert.True(result.ReducedToZeroHitPoints);
        Assert.Equal(0, result.State.CurrentHitPoints);
        Assert.Equal(0, result.State.TemporaryHitPoints);
    }

    [Fact]
    public void ResolveDamage_WhenDamageExactlyReachesZero_ReportsNoRemainingDamage()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20) with
        {
            CurrentHitPoints = 6,
            TemporaryHitPoints = 2
        };

        HitPointDamageResult result =
            HitPointRules.ResolveDamage(
                state,
                damageAmount: 8);

        Assert.Equal(
            2,
            result.DamageAbsorbedByTemporaryHitPoints);
        Assert.Equal(
            6,
            result.DamageAppliedToCurrentHitPoints);
        Assert.Equal(
            0,
            result.DamageRemainingAfterReachingZeroHitPoints);
        Assert.False(result.StartedAtZeroHitPoints);
        Assert.True(result.ReducedToZeroHitPoints);
        Assert.True(result.State.IsAtZeroHitPoints);
    }

    [Fact]
    public void ResolveDamage_WhenStartedAtZeroHitPoints_ReportsAllUnabsorbedDamageAsRemaining()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20) with
        {
            CurrentHitPoints = 0
        };

        HitPointDamageResult result =
            HitPointRules.ResolveDamage(
                state,
                damageAmount: 5);

        Assert.Equal(
            0,
            result.DamageAbsorbedByTemporaryHitPoints);
        Assert.Equal(
            0,
            result.DamageAppliedToCurrentHitPoints);
        Assert.Equal(
            5,
            result.DamageRemainingAfterReachingZeroHitPoints);
        Assert.True(result.StartedAtZeroHitPoints);
        Assert.False(result.ReducedToZeroHitPoints);
        Assert.True(result.State.IsAtZeroHitPoints);
    }

    [Fact]
    public void ResolveDamage_WhenStartedAtZeroWithTemporaryHitPoints_ReportsAbsorbedDamage()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20) with
        {
            CurrentHitPoints = 0,
            TemporaryHitPoints = 5
        };

        HitPointDamageResult result =
            HitPointRules.ResolveDamage(
                state,
                damageAmount: 3);

        Assert.Equal(
            3,
            result.DamageAbsorbedByTemporaryHitPoints);
        Assert.Equal(
            0,
            result.DamageAppliedToCurrentHitPoints);
        Assert.Equal(
            0,
            result.DamageRemainingAfterReachingZeroHitPoints);
        Assert.True(result.StartedAtZeroHitPoints);
        Assert.False(result.ReducedToZeroHitPoints);
        Assert.Equal(0, result.State.CurrentHitPoints);
        Assert.Equal(2, result.State.TemporaryHitPoints);
    }

    [Fact]
    public void ResolveDamage_WithNullState_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            HitPointRules.ResolveDamage(
                null!,
                damageAmount: 1));
    }

    [Fact]
    public void ResolveDamage_WithNegativeDamage_Throws()
    {
        HitPointState state = HitPointRules.Create(
            maximumHitPoints: 20);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HitPointRules.ResolveDamage(
                state,
                damageAmount: -1));
    }

    [Fact]
    public void ResolveDamage_WithInvalidState_Throws()
    {
        HitPointState state = new()
        {
            MaximumHitPoints = 20,
            CurrentHitPoints = 21,
            TemporaryHitPoints = 0
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HitPointRules.ResolveDamage(
                state,
                damageAmount: 1));
    }
}

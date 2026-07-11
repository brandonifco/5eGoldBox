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
}

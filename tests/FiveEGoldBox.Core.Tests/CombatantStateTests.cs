using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

public sealed class CombatantStateTests
{
    [Fact]
    public void LifecycleState_WhenAboveZeroHitPoints_IsConscious()
    {
        CombatantState state = CreateCombatant();

        Assert.Equal(
            CombatantLifecycleState.Conscious,
            state.LifecycleState);
        Assert.False(state.IsUnconscious);
        Assert.False(state.IsTerminal);
    }

    [Fact]
    public void LifecycleState_WhenAtZeroWithDeathSavingThrows_IsDying()
    {
        CombatantState state = CreateCombatant() with
        {
            Health = CreateHealthAtZero()
        };

        Assert.Equal(
            CombatantLifecycleState.Dying,
            state.LifecycleState);
        Assert.True(state.IsUnconscious);
        Assert.False(state.IsTerminal);
    }

    [Fact]
    public void LifecycleState_WhenStableAtZero_IsStable()
    {
        CombatantState state = CreateCombatant() with
        {
            Health = CreateHealthAtZero() with
            {
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        IsStable = true
                    }
            }
        };

        Assert.Equal(
            CombatantLifecycleState.Stable,
            state.LifecycleState);
        Assert.True(state.IsUnconscious);
        Assert.False(state.IsTerminal);
    }

    [Fact]
    public void LifecycleState_WhenDeathSavingThrowsHaveFailed_IsDead()
    {
        CombatantState state = CreateCombatant() with
        {
            Health = CreateHealthAtZero() with
            {
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        FailureCount =
                            DeathSavingThrowRules
                                .FailuresRequired
                    }
            }
        };

        Assert.Equal(
            CombatantLifecycleState.Dead,
            state.LifecycleState);
        Assert.False(state.IsUnconscious);
        Assert.True(state.IsTerminal);
    }

    [Fact]
    public void LifecycleState_WhenZeroHitPointPolicyIsDefeated_IsDefeated()
    {
        CombatantState state = CreateCombatant() with
        {
            ZeroHitPointPolicy =
                CombatantZeroHitPointPolicy.Defeated,
            Health = CreateHealthAtZero()
        };

        Assert.Equal(
            CombatantLifecycleState.Defeated,
            state.LifecycleState);
        Assert.False(state.IsUnconscious);
        Assert.True(state.IsTerminal);
    }

    private static CombatantState CreateCombatant()
    {
        return new CombatantState
        {
            CombatantId = "combatant.test",
            ZeroHitPointPolicy =
                CombatantZeroHitPointPolicy
                    .DeathSavingThrows,
            Health = CombatantHealthRules.Create(
                maximumHitPoints: 10)
        };
    }

    private static CombatantHealthState CreateHealthAtZero()
    {
        return CombatantHealthRules.Create(
            maximumHitPoints: 10) with
        {
            HitPoints = HitPointRules.Create(
                maximumHitPoints: 10) with
            {
                CurrentHitPoints = 0
            }
        };
    }
    [Fact]
    public void Create_ReturnsConsciousCombatantWithFullHitPoints()
    {
        CombatantState state = CombatantRules.Create(
            combatantId: "combatant.hero",
            maximumHitPoints: 12,
            zeroHitPointPolicy:
                CombatantZeroHitPointPolicy.DeathSavingThrows);

        Assert.Equal("combatant.hero", state.CombatantId);
        Assert.Equal(
            CombatantZeroHitPointPolicy.DeathSavingThrows,
            state.ZeroHitPointPolicy);
        Assert.Equal(12, state.Health.HitPoints.MaximumHitPoints);
        Assert.Equal(12, state.Health.HitPoints.CurrentHitPoints);
        Assert.Equal(
            CombatantLifecycleState.Conscious,
            state.LifecycleState);
    }
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Create_WithBlankCombatantId_Throws(
    string combatantId)
    {
        Assert.Throws<ArgumentException>(() =>
            CombatantRules.Create(
                combatantId,
                maximumHitPoints: 10,
                CombatantZeroHitPointPolicy
                    .DeathSavingThrows));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidMaximumHitPoints_Throws(
        int maximumHitPoints)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CombatantRules.Create(
                combatantId: "combatant.test",
                maximumHitPoints,
                CombatantZeroHitPointPolicy
                    .DeathSavingThrows));
    }

    [Fact]
    public void Create_WithUnsupportedZeroHitPointPolicy_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CombatantRules.Create(
                combatantId: "combatant.test",
                maximumHitPoints: 10,
                zeroHitPointPolicy:
                    (CombatantZeroHitPointPolicy)999));
    }
}

using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CombatantHealthRulesTests
{
    [Fact]
    public void Create_ReturnsInitializedHealthState()
    {
        CombatantHealthState result =
            CombatantHealthRules.Create(
                maximumHitPoints: 20);

        Assert.Equal(20, result.HitPoints.MaximumHitPoints);
        Assert.Equal(20, result.HitPoints.CurrentHitPoints);
        Assert.Equal(0, result.HitPoints.TemporaryHitPoints);
        Assert.Equal(0, result.DeathSavingThrows.SuccessCount);
        Assert.Equal(0, result.DeathSavingThrows.FailureCount);
        Assert.False(result.DeathSavingThrows.IsStable);
        Assert.False(result.IsInstantlyDead);
        Assert.False(result.IsDead);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidMaximumHitPoints_Throws(
        int maximumHitPoints)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CombatantHealthRules.Create(
                maximumHitPoints));
    }

    [Fact]
    public void ResolveDamage_WhenCreatureRemainsAboveZero_DoesNotApplyZeroHitPointRules()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20);

        CombatantHealthDamageResult result =
            CombatantHealthRules.ResolveDamage(
                state,
                damageAmount: 5,
                isCriticalHit: false);

        Assert.False(result.IsCriticalHit);
        Assert.Equal(15, result.State.HitPoints.CurrentHitPoints);
        Assert.Null(result.ZeroHitPointDamage);
        Assert.Same(
            state.DeathSavingThrows,
            result.State.DeathSavingThrows);
        Assert.False(result.State.IsDead);
    }

    [Fact]
    public void ResolveDamage_WhenDamageExactlyReducesCreatureToZero_DoesNotApplyZeroHitPointRules()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                HitPoints = HitPointRules.Create(
                    maximumHitPoints: 20) with
                {
                    CurrentHitPoints = 5
                }
            };

        CombatantHealthDamageResult result =
            CombatantHealthRules.ResolveDamage(
                state,
                damageAmount: 5,
                isCriticalHit: true);

        Assert.True(result.IsCriticalHit);
        Assert.True(
            result.HitPointDamage.ReducedToZeroHitPoints);
        Assert.Equal(
            0,
            result.HitPointDamage
                .DamageRemainingAfterReachingZeroHitPoints);
        Assert.Null(result.ZeroHitPointDamage);
        Assert.False(result.State.IsDead);
    }

    [Fact]
    public void ResolveDamage_WhenExcessDamageIsBelowMaximumHitPoints_DoesNotApplyDeathSaveFailure()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                HitPoints = HitPointRules.Create(
                    maximumHitPoints: 20) with
                {
                    CurrentHitPoints = 5
                }
            };

        CombatantHealthDamageResult result =
            CombatantHealthRules.ResolveDamage(
                state,
                damageAmount: 24,
                isCriticalHit: true);

        Assert.True(
            result.HitPointDamage.ReducedToZeroHitPoints);
        Assert.Equal(
            19,
            result.HitPointDamage
                .DamageRemainingAfterReachingZeroHitPoints);
        Assert.Null(result.ZeroHitPointDamage);
        Assert.Equal(
            0,
            result.State.DeathSavingThrows.FailureCount);
        Assert.False(result.State.IsDead);
    }

    [Fact]
    public void ResolveDamage_WhenExcessDamageEqualsMaximumHitPoints_CausesInstantDeath()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                HitPoints = HitPointRules.Create(
                    maximumHitPoints: 20) with
                {
                    CurrentHitPoints = 5
                }
            };

        CombatantHealthDamageResult result =
            CombatantHealthRules.ResolveDamage(
                state,
                damageAmount: 25,
                isCriticalHit: false);

        Assert.NotNull(result.ZeroHitPointDamage);
        Assert.Equal(
            ZeroHitPointDamageOutcome.InstantDeath,
            result.ZeroHitPointDamage.Outcome);
        Assert.Equal(
            0,
            result.ZeroHitPointDamage
                .DeathSavingThrowFailuresCaused);
        Assert.True(result.State.IsInstantlyDead);
        Assert.True(result.State.IsDead);
        Assert.False(
            result.State.DeathSavingThrows.IsDead);
    }

    [Fact]
    public void ResolveDamage_WhenCreatureStartedAtZero_AddsOneFailure()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                HitPoints = HitPointRules.Create(
                    maximumHitPoints: 20) with
                {
                    CurrentHitPoints = 0
                }
            };

        CombatantHealthDamageResult result =
            CombatantHealthRules.ResolveDamage(
                state,
                damageAmount: 5,
                isCriticalHit: false);

        Assert.True(
            result.HitPointDamage.StartedAtZeroHitPoints);
        Assert.NotNull(result.ZeroHitPointDamage);
        Assert.Equal(
            ZeroHitPointDamageOutcome
                .DeathSavingThrowFailure,
            result.ZeroHitPointDamage.Outcome);
        Assert.Equal(
            1,
            result.State.DeathSavingThrows.FailureCount);
        Assert.False(result.State.IsDead);
    }

    [Fact]
    public void ResolveDamage_WhenCreatureStartedAtZeroAndHitWasCritical_AddsTwoFailures()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                HitPoints = HitPointRules.Create(
                    maximumHitPoints: 20) with
                {
                    CurrentHitPoints = 0
                }
            };

        CombatantHealthDamageResult result =
            CombatantHealthRules.ResolveDamage(
                state,
                damageAmount: 5,
                isCriticalHit: true);

        Assert.NotNull(result.ZeroHitPointDamage);
        Assert.Equal(
            2,
            result.ZeroHitPointDamage
                .DeathSavingThrowFailuresCaused);
        Assert.Equal(
            2,
            result.State.DeathSavingThrows.FailureCount);
        Assert.False(result.State.IsDead);
    }

    [Fact]
    public void ResolveDamage_WhenCreatureAtZeroHasTwoFailures_Dies()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                HitPoints = HitPointRules.Create(
                    maximumHitPoints: 20) with
                {
                    CurrentHitPoints = 0
                },
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        SuccessCount = 1,
                        FailureCount = 2
                    }
            };

        CombatantHealthDamageResult result =
            CombatantHealthRules.ResolveDamage(
                state,
                damageAmount: 5,
                isCriticalHit: false);

        Assert.NotNull(result.ZeroHitPointDamage);
        Assert.Equal(
            ZeroHitPointDamageOutcome.Dead,
            result.ZeroHitPointDamage.Outcome);
        Assert.Equal(
            3,
            result.State.DeathSavingThrows.FailureCount);
        Assert.True(
            result.State.DeathSavingThrows.IsDead);
        Assert.True(result.State.IsDead);
    }

    [Fact]
    public void ResolveDamage_WhenStableCreatureAtZeroTakesDamage_MakesCreatureUnstable()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                HitPoints = HitPointRules.Create(
                    maximumHitPoints: 20) with
                {
                    CurrentHitPoints = 0
                },
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        IsStable = true
                    }
            };

        CombatantHealthDamageResult result =
            CombatantHealthRules.ResolveDamage(
                state,
                damageAmount: 5,
                isCriticalHit: false);

        Assert.NotNull(result.ZeroHitPointDamage);
        Assert.False(
            result.State.DeathSavingThrows.IsStable);
        Assert.Equal(
            1,
            result.State.DeathSavingThrows.FailureCount);
        Assert.False(result.State.IsDead);
    }

    [Fact]
    public void ResolveDamage_WhenTemporaryHitPointsAbsorbAllDamageAtZero_AppliesDeathSaveFailures()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                HitPoints = HitPointRules.Create(
                    maximumHitPoints: 20) with
                {
                    CurrentHitPoints = 0,
                    TemporaryHitPoints = 5
                }
            };

        CombatantHealthDamageResult result =
            CombatantHealthRules.ResolveDamage(
                state,
                damageAmount: 3,
                isCriticalHit: true);

        Assert.Equal(
            3,
            result.HitPointDamage
                .DamageAbsorbedByTemporaryHitPoints);
        Assert.Equal(
            0,
            result.HitPointDamage
                .DamageRemainingAfterReachingZeroHitPoints);
        Assert.Equal(
            2,
            result.State.HitPoints.TemporaryHitPoints);

        Assert.NotNull(result.ZeroHitPointDamage);
        Assert.Equal(
            ZeroHitPointDamageOutcome.DeathSavingThrowFailure,
            result.ZeroHitPointDamage.Outcome);
        Assert.Equal(
            2,
            result.ZeroHitPointDamage
                .DeathSavingThrowFailuresCaused);
        Assert.Equal(
            2,
            result.State.DeathSavingThrows.FailureCount);
        Assert.False(result.State.IsDead);
    }

    [Fact]
    public void ResolveDamage_WhenTemporaryHitPointsPartiallyAbsorbDamageAtZero_AppliesFailure()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                HitPoints = HitPointRules.Create(
                    maximumHitPoints: 20) with
                {
                    CurrentHitPoints = 0,
                    TemporaryHitPoints = 2
                }
            };

        CombatantHealthDamageResult result =
            CombatantHealthRules.ResolveDamage(
                state,
                damageAmount: 5,
                isCriticalHit: false);

        Assert.Equal(
            2,
            result.HitPointDamage
                .DamageAbsorbedByTemporaryHitPoints);
        Assert.Equal(
            3,
            result.HitPointDamage
                .DamageRemainingAfterReachingZeroHitPoints);
        Assert.NotNull(result.ZeroHitPointDamage);
        Assert.Equal(
            1,
            result.State.DeathSavingThrows.FailureCount);
    }

    [Fact]
    public void ResolveDamage_WithZeroDamageAtZero_DoesNotApplyZeroHitPointRules()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                HitPoints = HitPointRules.Create(
                    maximumHitPoints: 20) with
                {
                    CurrentHitPoints = 0
                }
            };

        CombatantHealthDamageResult result =
            CombatantHealthRules.ResolveDamage(
                state,
                damageAmount: 0,
                isCriticalHit: true);

        Assert.Null(result.ZeroHitPointDamage);
        Assert.Equal(
            0,
            result.State.DeathSavingThrows.FailureCount);
        Assert.False(result.State.IsDead);
    }

    [Fact]
    public void ResolveDamage_WithInstantlyDeadState_Throws()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                HitPoints = HitPointRules.Create(
                    maximumHitPoints: 20) with
                {
                    CurrentHitPoints = 0
                },
                IsInstantlyDead = true
            };

        Assert.Throws<InvalidOperationException>(() =>
            CombatantHealthRules.ResolveDamage(
                state,
                damageAmount: 1,
                isCriticalHit: false));
    }

    [Fact]
    public void ResolveDamage_WithDeathSaveDeadState_Throws()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                HitPoints = HitPointRules.Create(
                    maximumHitPoints: 20) with
                {
                    CurrentHitPoints = 0
                },
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        FailureCount = 3
                    }
            };

        Assert.Throws<InvalidOperationException>(() =>
            CombatantHealthRules.ResolveDamage(
                state,
                damageAmount: 1,
                isCriticalHit: false));
    }

    [Fact]
    public void ResolveDamage_WithPositiveHitPointsAndDeathSaveProgress_Throws()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        SuccessCount = 1
                    }
            };

        Assert.Throws<ArgumentException>(() =>
            CombatantHealthRules.ResolveDamage(
                state,
                damageAmount: 1,
                isCriticalHit: false));
    }

    [Fact]
    public void ResolveDamage_WithInstantDeathAboveZeroHitPoints_Throws()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                IsInstantlyDead = true
            };

        Assert.Throws<ArgumentException>(() =>
            CombatantHealthRules.ResolveDamage(
                state,
                damageAmount: 1,
                isCriticalHit: false));
    }

    [Fact]
    public void ResolveDamage_WithInstantDeathAndStableState_Throws()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                HitPoints = HitPointRules.Create(
                    maximumHitPoints: 20) with
                {
                    CurrentHitPoints = 0
                },
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        IsStable = true
                    },
                IsInstantlyDead = true
            };

        Assert.Throws<ArgumentException>(() =>
            CombatantHealthRules.ResolveDamage(
                state,
                damageAmount: 1,
                isCriticalHit: false));
    }

    [Fact]
    public void ResolveDamage_WithNullState_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CombatantHealthRules.ResolveDamage(
                null!,
                damageAmount: 1,
                isCriticalHit: false));
    }

    [Fact]
    public void ResolveDamage_WithNullHitPointState_Throws()
    {
        CombatantHealthState state = new()
        {
            HitPoints = null!,
            DeathSavingThrows =
                DeathSavingThrowRules.Create(),
            IsInstantlyDead = false
        };

        Assert.Throws<ArgumentNullException>(() =>
            CombatantHealthRules.ResolveDamage(
                state,
                damageAmount: 1,
                isCriticalHit: false));
    }

    [Fact]
    public void ResolveDamage_WithNullDeathSavingThrowState_Throws()
    {
        CombatantHealthState state = new()
        {
            HitPoints = HitPointRules.Create(
                maximumHitPoints: 20),
            DeathSavingThrows = null!,
            IsInstantlyDead = false
        };

        Assert.Throws<ArgumentNullException>(() =>
            CombatantHealthRules.ResolveDamage(
                state,
                damageAmount: 1,
                isCriticalHit: false));
    }

    [Fact]
    public void ResolveDamage_WithNegativeDamage_Throws()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CombatantHealthRules.ResolveDamage(
                state,
                damageAmount: -1,
                isCriticalHit: false));
    }

    [Fact]
    public void ResolveDamage_WithInvalidHitPointState_Throws()
    {
        CombatantHealthState state = new()
        {
            HitPoints = new HitPointState
            {
                MaximumHitPoints = 20,
                CurrentHitPoints = 21,
                TemporaryHitPoints = 0
            },
            DeathSavingThrows =
                DeathSavingThrowRules.Create(),
            IsInstantlyDead = false
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CombatantHealthRules.ResolveDamage(
                state,
                damageAmount: 1,
                isCriticalHit: false));
    }
    [Fact]
    public void ResolveHealing_WhenCreatureAtZero_RegainsHitPointsAndResetsDeathSavingThrows()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                HitPoints = HitPointRules.Create(
                    maximumHitPoints: 20) with
                {
                    CurrentHitPoints = 0
                },
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        SuccessCount = 2,
                        FailureCount = 1
                    }
            };

        CombatantHealthHealingResult result =
            CombatantHealthRules.ResolveHealing(
                state,
                healingAmount: 5);

        Assert.Equal(5, result.HealingAmount);
        Assert.Equal(5, result.HitPointsRestored);
        Assert.True(result.ResetDeathSavingThrows);
        Assert.Equal(
            5,
            result.State.HitPoints.CurrentHitPoints);
        Assert.Equal(
            0,
            result.State.DeathSavingThrows.SuccessCount);
        Assert.Equal(
            0,
            result.State.DeathSavingThrows.FailureCount);
        Assert.False(
            result.State.DeathSavingThrows.IsStable);
        Assert.False(result.State.IsDead);
    }

    [Fact]
    public void ResolveHealing_WhenStableCreatureAtZero_RegainsHitPointsAndBecomesUnstable()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                HitPoints = HitPointRules.Create(
                    maximumHitPoints: 20) with
                {
                    CurrentHitPoints = 0
                },
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        IsStable = true
                    }
            };

        CombatantHealthHealingResult result =
            CombatantHealthRules.ResolveHealing(
                state,
                healingAmount: 1);

        Assert.Equal(
            1,
            result.State.HitPoints.CurrentHitPoints);
        Assert.True(result.ResetDeathSavingThrows);
        Assert.False(
            result.State.DeathSavingThrows.IsStable);
    }

    [Fact]
    public void ResolveHealing_WhenHealingExceedsMissingHitPoints_CapsAtMaximum()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                HitPoints = HitPointRules.Create(
                    maximumHitPoints: 20) with
                {
                    CurrentHitPoints = 18
                }
            };

        CombatantHealthHealingResult result =
            CombatantHealthRules.ResolveHealing(
                state,
                healingAmount: 10);

        Assert.Equal(2, result.HitPointsRestored);
        Assert.Equal(
            20,
            result.State.HitPoints.CurrentHitPoints);
        Assert.False(result.ResetDeathSavingThrows);
    }

    [Fact]
    public void ResolveHealing_WithZeroHealing_ReturnsUnchangedState()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20);

        CombatantHealthHealingResult result =
            CombatantHealthRules.ResolveHealing(
                state,
                healingAmount: 0);

        Assert.Equal(0, result.HitPointsRestored);
        Assert.False(result.ResetDeathSavingThrows);
        Assert.Same(state, result.State);
    }

    [Fact]
    public void ResolveHealing_WithNegativeHealing_Throws()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CombatantHealthRules.ResolveHealing(
                state,
                healingAmount: -1));
    }

    [Fact]
    public void ResolveHealing_WithDeadState_Throws()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                HitPoints = HitPointRules.Create(
                    maximumHitPoints: 20) with
                {
                    CurrentHitPoints = 0
                },
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        FailureCount =
                            DeathSavingThrowRules
                                .FailuresRequired
                    }
            };

        Assert.Throws<InvalidOperationException>(() =>
            CombatantHealthRules.ResolveHealing(
                state,
                healingAmount: 1));
    }
    [Fact]
    public void ResolveDeathSavingThrow_WhenRollSucceeds_RecordsSuccess()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                HitPoints = HitPointRules.Create(
                    maximumHitPoints: 20) with
                {
                    CurrentHitPoints = 0
                }
            };

        CombatantHealthDeathSavingThrowResult result =
            CombatantHealthRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 10,
                secondRoll: null,
                savingThrowBonus: 0);

        Assert.Equal(
            DeathSavingThrowOutcome.Success,
            result.DeathSavingThrow.Outcome);
        Assert.Equal(
            0,
            result.State.HitPoints.CurrentHitPoints);
        Assert.Equal(
            1,
            result.State.DeathSavingThrows.SuccessCount);
        Assert.False(
            result.State.DeathSavingThrows.IsStable);
        Assert.False(result.State.IsDead);
    }

    [Fact]
    public void ResolveDeathSavingThrow_WithThirdSuccess_Stabilizes()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                HitPoints = HitPointRules.Create(
                    maximumHitPoints: 20) with
                {
                    CurrentHitPoints = 0
                },
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        SuccessCount = 2,
                        FailureCount = 1
                    }
            };

        CombatantHealthDeathSavingThrowResult result =
            CombatantHealthRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 10,
                secondRoll: null,
                savingThrowBonus: 0);

        Assert.Equal(
            DeathSavingThrowOutcome.Stabilized,
            result.DeathSavingThrow.Outcome);
        Assert.Equal(
            0,
            result.State.DeathSavingThrows.SuccessCount);
        Assert.Equal(
            0,
            result.State.DeathSavingThrows.FailureCount);
        Assert.True(
            result.State.DeathSavingThrows.IsStable);
        Assert.False(result.State.IsDead);
    }

    [Fact]
    public void ResolveDeathSavingThrow_WithNaturalTwenty_RegainsOneHitPointAndResetsState()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                HitPoints = HitPointRules.Create(
                    maximumHitPoints: 20) with
                {
                    CurrentHitPoints = 0
                },
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        SuccessCount = 2,
                        FailureCount = 2
                    }
            };

        CombatantHealthDeathSavingThrowResult result =
            CombatantHealthRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 20,
                secondRoll: null,
                savingThrowBonus: 0);

        Assert.Equal(
            DeathSavingThrowOutcome.RegainedHitPoint,
            result.DeathSavingThrow.Outcome);
        Assert.Equal(
            1,
            result.State.HitPoints.CurrentHitPoints);
        Assert.Equal(
            0,
            result.State.DeathSavingThrows.SuccessCount);
        Assert.Equal(
            0,
            result.State.DeathSavingThrows.FailureCount);
        Assert.False(
            result.State.DeathSavingThrows.IsStable);
        Assert.False(result.State.IsDead);
    }

    [Fact]
    public void ResolveDeathSavingThrow_WithNaturalOneAndExistingFailure_Dies()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                HitPoints = HitPointRules.Create(
                    maximumHitPoints: 20) with
                {
                    CurrentHitPoints = 0
                },
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        FailureCount = 1
                    }
            };

        CombatantHealthDeathSavingThrowResult result =
            CombatantHealthRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 1,
                secondRoll: null,
                savingThrowBonus: 0);

        Assert.Equal(
            DeathSavingThrowOutcome.Dead,
            result.DeathSavingThrow.Outcome);
        Assert.Equal(
            3,
            result.State.DeathSavingThrows.FailureCount);
        Assert.True(result.State.IsDead);
    }

    [Fact]
    public void ResolveDeathSavingThrow_WhenCreatureHasHitPoints_Throws()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20);

        Assert.Throws<InvalidOperationException>(() =>
            CombatantHealthRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 10,
                secondRoll: null,
                savingThrowBonus: 0));
    }

    [Fact]
    public void ResolveDeathSavingThrow_WhenCreatureIsStable_Throws()
    {
        CombatantHealthState state =
            CombatantHealthRules.Create(
                maximumHitPoints: 20) with
            {
                HitPoints = HitPointRules.Create(
                    maximumHitPoints: 20) with
                {
                    CurrentHitPoints = 0
                },
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        IsStable = true
                    }
            };

        Assert.Throws<InvalidOperationException>(() =>
            CombatantHealthRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 10,
                secondRoll: null,
                savingThrowBonus: 0));
    }
}

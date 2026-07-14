using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

public sealed class CombatantRulesTests
{
    [Fact]
    public void ResolveDamage_WhenPlayerCharacterReachesZero_BecomesDying()
    {
        CombatantState state = CombatantRules.Create(
            combatantId: "combatant.hero",
            maximumHitPoints: 10,
            CombatantZeroHitPointPolicy.DeathSavingThrows);

        CombatantDamageResult result =
            CombatantRules.ResolveDamage(
                state,
                damageAmount: 10,
                isCriticalHit: false);

        Assert.Equal(
            10,
            state.Health.HitPoints.CurrentHitPoints);
        Assert.Equal(
            0,
            result.State.Health.HitPoints.CurrentHitPoints);
        Assert.Equal(
            CombatantLifecycleState.Dying,
            result.State.LifecycleState);
        Assert.True(result.State.IsUnconscious);
        Assert.False(result.State.IsTerminal);
        Assert.NotSame(state, result.State);
    }

    [Fact]
    public void ResolveDamage_WhenOrdinaryEnemyReachesZero_BecomesDefeated()
    {
        CombatantState state = CombatantRules.Create(
            combatantId: "combatant.enemy",
            maximumHitPoints: 10,
            CombatantZeroHitPointPolicy.Defeated);

        CombatantDamageResult result =
            CombatantRules.ResolveDamage(
                state,
                damageAmount: 10,
                isCriticalHit: false);

        Assert.Equal(
            CombatantLifecycleState.Defeated,
            result.State.LifecycleState);
        Assert.True(result.State.IsTerminal);
        Assert.False(result.State.IsUnconscious);
        Assert.Null(
            result.HealthDamage.ZeroHitPointDamage);
        Assert.Equal(
            0,
            result.State.Health
                .DeathSavingThrows.FailureCount);
    }

    [Fact]
    public void ResolveDamage_WhenDyingCombatantTakesDamage_AddsOneFailure()
    {
        CombatantState state = CreateDyingCombatant();

        CombatantDamageResult result =
            CombatantRules.ResolveDamage(
                state,
                damageAmount: 1,
                isCriticalHit: false);

        Assert.Equal(
            1,
            result.State.Health
                .DeathSavingThrows.FailureCount);
        Assert.Equal(
            CombatantLifecycleState.Dying,
            result.State.LifecycleState);
    }

    [Fact]
    public void ResolveDamage_WhenDyingCombatantTakesCriticalDamage_AddsTwoFailures()
    {
        CombatantState state = CreateDyingCombatant();

        CombatantDamageResult result =
            CombatantRules.ResolveDamage(
                state,
                damageAmount: 1,
                isCriticalHit: true);

        Assert.Equal(
            2,
            result.State.Health
                .DeathSavingThrows.FailureCount);
        Assert.Equal(
            CombatantLifecycleState.Dying,
            result.State.LifecycleState);
    }

    [Fact]
    public void ResolveDamage_WhenMassiveDamageApplies_BecomesDead()
    {
        CombatantState state = CombatantRules.Create(
            combatantId: "combatant.hero",
            maximumHitPoints: 10,
            CombatantZeroHitPointPolicy.DeathSavingThrows);

        CombatantDamageResult result =
            CombatantRules.ResolveDamage(
                state,
                damageAmount: 20,
                isCriticalHit: false);

        Assert.True(
            result.State.Health.IsInstantlyDead);
        Assert.Equal(
            CombatantLifecycleState.Dead,
            result.State.LifecycleState);
        Assert.True(result.State.IsTerminal);
    }

    [Fact]
    public void ResolveDamage_WhenCombatantIsTerminal_Throws()
    {
        CombatantState defeated =
            CombatantRules.ResolveDamage(
                CombatantRules.Create(
                    combatantId: "combatant.enemy",
                    maximumHitPoints: 10,
                    CombatantZeroHitPointPolicy.Defeated),
                damageAmount: 10,
                isCriticalHit: false)
            .State;

        Assert.Throws<InvalidOperationException>(() =>
            CombatantRules.ResolveDamage(
                defeated,
                damageAmount: 1,
                isCriticalHit: false));
    }

    private static CombatantState CreateDyingCombatant()
    {
        return CombatantRules.ResolveDamage(
            CombatantRules.Create(
                combatantId: "combatant.hero",
                maximumHitPoints: 10,
                CombatantZeroHitPointPolicy
                    .DeathSavingThrows),
            damageAmount: 10,
            isCriticalHit: false)
        .State;
    }
    [Fact]
    public void ResolveHealing_WhenDyingCombatantRegainsHitPoints_BecomesConscious()
    {
        CombatantState state = CreateDyingCombatant() with
        {
            Health = CreateDyingCombatant().Health with
            {
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        SuccessCount = 1,
                        FailureCount = 2
                    }
            }
        };

        CombatantHealingResult result =
            CombatantRules.ResolveHealing(
                state,
                healingAmount: 4);

        Assert.Equal(
            4,
            result.State.Health.HitPoints.CurrentHitPoints);
        Assert.Equal(
            CombatantLifecycleState.Conscious,
            result.State.LifecycleState);
        Assert.False(result.State.IsUnconscious);
        Assert.Equal(
            0,
            result.State.Health
                .DeathSavingThrows.SuccessCount);
        Assert.Equal(
            0,
            result.State.Health
                .DeathSavingThrows.FailureCount);
    }

    [Fact]
    public void ResolveHealing_WhenStableCombatantRegainsHitPoints_BecomesConscious()
    {
        CombatantState state = CreateDyingCombatant() with
        {
            Health = CreateDyingCombatant().Health with
            {
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        IsStable = true
                    }
            }
        };

        CombatantHealingResult result =
            CombatantRules.ResolveHealing(
                state,
                healingAmount: 1);

        Assert.Equal(
            1,
            result.State.Health.HitPoints.CurrentHitPoints);
        Assert.Equal(
            CombatantLifecycleState.Conscious,
            result.State.LifecycleState);
        Assert.False(
            result.State.Health
                .DeathSavingThrows.IsStable);
    }

    [Fact]
    public void ResolveHealing_WhenHealingExceedsMissingHitPoints_CapsAtMaximum()
    {
        CombatantState state = CombatantRules.Create(
            combatantId: "combatant.hero",
            maximumHitPoints: 10,
            CombatantZeroHitPointPolicy.DeathSavingThrows);

        state = CombatantRules.ResolveDamage(
            state,
            damageAmount: 3,
            isCriticalHit: false)
            .State;

        CombatantHealingResult result =
            CombatantRules.ResolveHealing(
                state,
                healingAmount: 10);

        Assert.Equal(
            10,
            result.State.Health.HitPoints.CurrentHitPoints);
        Assert.Equal(
            3,
            result.HealthHealing.HitPointsRestored);
    }

    [Fact]
    public void ResolveHealing_WithZeroHealing_ReturnsEquivalentState()
    {
        CombatantState state = CombatantRules.Create(
            combatantId: "combatant.hero",
            maximumHitPoints: 10,
            CombatantZeroHitPointPolicy.DeathSavingThrows);

        CombatantHealingResult result =
            CombatantRules.ResolveHealing(
                state,
                healingAmount: 0);

        Assert.Equal(state, result.State);
        Assert.Equal(
            0,
            result.HealthHealing.HitPointsRestored);
    }

    [Fact]
    public void ResolveHealing_WhenCombatantIsTerminal_Throws()
    {
        CombatantState defeated =
            CombatantRules.ResolveDamage(
                CombatantRules.Create(
                    combatantId: "combatant.enemy",
                    maximumHitPoints: 10,
                    CombatantZeroHitPointPolicy.Defeated),
                damageAmount: 10,
                isCriticalHit: false)
            .State;

        Assert.Throws<InvalidOperationException>(() =>
            CombatantRules.ResolveHealing(
                defeated,
                healingAmount: 1));
    }
    [Fact]
    public void ResolveDeathSavingThrow_WhenRollSucceeds_RecordsSuccess()
    {
        CombatantState state = CreateDyingCombatant();

        CombatantDeathSavingThrowResult result =
            CombatantRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 10,
                secondRoll: null,
                savingThrowBonus: 0);

        Assert.Equal(
            DeathSavingThrowOutcome.Success,
            result.HealthDeathSavingThrow
                .DeathSavingThrow.Outcome);
        Assert.Equal(
            1,
            result.State.Health
                .DeathSavingThrows.SuccessCount);
        Assert.Equal(
            CombatantLifecycleState.Dying,
            result.State.LifecycleState);
    }

    [Fact]
    public void ResolveDeathSavingThrow_WithThirdSuccess_BecomesStable()
    {
        CombatantState state = CreateDyingCombatant() with
        {
            Health = CreateDyingCombatant().Health with
            {
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        SuccessCount = 2,
                        FailureCount = 1
                    }
            }
        };

        CombatantDeathSavingThrowResult result =
            CombatantRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 10,
                secondRoll: null,
                savingThrowBonus: 0);

        Assert.Equal(
            DeathSavingThrowOutcome.Stabilized,
            result.HealthDeathSavingThrow
                .DeathSavingThrow.Outcome);
        Assert.Equal(
            CombatantLifecycleState.Stable,
            result.State.LifecycleState);
        Assert.True(result.State.IsUnconscious);
        Assert.False(result.State.IsTerminal);
    }

    [Fact]
    public void ResolveDeathSavingThrow_WithNaturalTwenty_BecomesConscious()
    {
        CombatantState state = CreateDyingCombatant() with
        {
            Health = CreateDyingCombatant().Health with
            {
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        SuccessCount = 2,
                        FailureCount = 2
                    }
            }
        };

        CombatantDeathSavingThrowResult result =
            CombatantRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 20,
                secondRoll: null,
                savingThrowBonus: 0);

        Assert.Equal(
            DeathSavingThrowOutcome.RegainedHitPoint,
            result.HealthDeathSavingThrow
                .DeathSavingThrow.Outcome);
        Assert.Equal(
            1,
            result.State.Health.HitPoints.CurrentHitPoints);
        Assert.Equal(
            CombatantLifecycleState.Conscious,
            result.State.LifecycleState);
        Assert.Equal(
            0,
            result.State.Health
                .DeathSavingThrows.SuccessCount);
        Assert.Equal(
            0,
            result.State.Health
                .DeathSavingThrows.FailureCount);
    }

    [Fact]
    public void ResolveDeathSavingThrow_WithThirdFailure_BecomesDead()
    {
        CombatantState state = CreateDyingCombatant() with
        {
            Health = CreateDyingCombatant().Health with
            {
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        FailureCount = 2
                    }
            }
        };

        CombatantDeathSavingThrowResult result =
            CombatantRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 9,
                secondRoll: null,
                savingThrowBonus: 0);

        Assert.Equal(
            DeathSavingThrowOutcome.Dead,
            result.HealthDeathSavingThrow
                .DeathSavingThrow.Outcome);
        Assert.Equal(
            CombatantLifecycleState.Dead,
            result.State.LifecycleState);
        Assert.True(result.State.IsTerminal);
    }

    [Fact]
    public void ResolveDeathSavingThrow_WhenPolicyIsDefeated_Throws()
    {
        CombatantState state = CombatantRules.Create(
            combatantId: "combatant.enemy",
            maximumHitPoints: 10,
            CombatantZeroHitPointPolicy.Defeated);

        Assert.Throws<InvalidOperationException>(() =>
            CombatantRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 10,
                secondRoll: null,
                savingThrowBonus: 0));
    }

    [Fact]
    public void ResolveDeathSavingThrow_WhenCombatantIsConscious_Throws()
    {
        CombatantState state = CombatantRules.Create(
            combatantId: "combatant.hero",
            maximumHitPoints: 10,
            CombatantZeroHitPointPolicy.DeathSavingThrows);

        Assert.Throws<InvalidOperationException>(() =>
            CombatantRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 10,
                secondRoll: null,
                savingThrowBonus: 0));
    }

    [Fact]
    public void ResolveDeathSavingThrow_WhenCombatantIsStable_Throws()
    {
        CombatantState state = CreateDyingCombatant() with
        {
            Health = CreateDyingCombatant().Health with
            {
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        IsStable = true
                    }
            }
        };

        Assert.Throws<InvalidOperationException>(() =>
            CombatantRules.ResolveDeathSavingThrow(
                state,
                D20RollMode.Normal,
                firstRoll: 10,
                secondRoll: null,
                savingThrowBonus: 0));
    }
    [Fact]
    public void ResolveDamage_WithBlankCombatantId_ThrowsBeforeTransition()
    {
        CombatantState state = CombatantRules.Create(
            combatantId: "combatant.hero",
            maximumHitPoints: 10,
            CombatantZeroHitPointPolicy.DeathSavingThrows)
            with
        {
            CombatantId = " "
        };

        Assert.Throws<ArgumentException>(() =>
            CombatantRules.ResolveDamage(
                state,
                damageAmount: 1,
                isCriticalHit: false));

        Assert.Equal(
            10,
            state.Health.HitPoints.CurrentHitPoints);
    }

    [Fact]
    public void ResolveDamage_WithUnsupportedPolicy_ThrowsBeforeTransition()
    {
        CombatantState state = CombatantRules.Create(
            combatantId: "combatant.hero",
            maximumHitPoints: 10,
            CombatantZeroHitPointPolicy.DeathSavingThrows)
            with
        {
            ZeroHitPointPolicy =
                    (CombatantZeroHitPointPolicy)999
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CombatantRules.ResolveDamage(
                state,
                damageAmount: 1,
                isCriticalHit: false));

        Assert.Equal(
            10,
            state.Health.HitPoints.CurrentHitPoints);
    }

    [Fact]
    public void ResolveDamage_WhenPositiveHitPointStateHasDeathSaveProgress_ThrowsBeforeTransition()
    {
        CombatantState state = CombatantRules.Create(
            combatantId: "combatant.hero",
            maximumHitPoints: 10,
            CombatantZeroHitPointPolicy.DeathSavingThrows)
            with
        {
            Health = CombatantHealthRules.Create(
                    maximumHitPoints: 10) with
            {
                DeathSavingThrows =
                        DeathSavingThrowRules.Create() with
                        {
                            FailureCount = 1
                        }
            }
        };

        Assert.Throws<ArgumentException>(() =>
            CombatantRules.ResolveDamage(
                state,
                damageAmount: 1,
                isCriticalHit: false));

        Assert.Equal(
            10,
            state.Health.HitPoints.CurrentHitPoints);
        Assert.Equal(
            1,
            state.Health.DeathSavingThrows.FailureCount);
    }

    [Fact]
    public void ResolveDamage_WhenDefeatedPolicyHasDeathSaveProgress_ThrowsBeforeTransition()
    {
        CombatantState state = CombatantRules.Create(
            combatantId: "combatant.enemy",
            maximumHitPoints: 10,
            CombatantZeroHitPointPolicy.Defeated)
            with
        {
            Health = CombatantHealthRules.Create(
                    maximumHitPoints: 10) with
            {
                DeathSavingThrows =
                        DeathSavingThrowRules.Create() with
                        {
                            SuccessCount = 1
                        }
            }
        };

        Assert.Throws<ArgumentException>(() =>
            CombatantRules.ResolveDamage(
                state,
                damageAmount: 1,
                isCriticalHit: false));

        Assert.Equal(
            10,
            state.Health.HitPoints.CurrentHitPoints);
        Assert.Equal(
            1,
            state.Health.DeathSavingThrows.SuccessCount);
    }
    [Fact]
    public void ResolveDamage_WhenDefeatedPolicyIsInstantlyDead_ThrowsBeforeTransition()
    {
        CombatantState state = CombatantRules.Create(
            combatantId: "combatant.enemy",
            maximumHitPoints: 10,
            CombatantZeroHitPointPolicy.Defeated)
            with
        {
            Health = CombatantHealthRules.Create(
                    maximumHitPoints: 10) with
            {
                HitPoints = HitPointRules.Create(
                        maximumHitPoints: 10) with
                {
                    CurrentHitPoints = 0
                },
                IsInstantlyDead = true
            }
        };

        Assert.Throws<ArgumentException>(() =>
            CombatantRules.ResolveDamage(
                state,
                damageAmount: 1,
                isCriticalHit: false));

        Assert.True(state.Health.IsInstantlyDead);
    }
}

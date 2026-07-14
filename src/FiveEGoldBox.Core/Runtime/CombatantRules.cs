using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public static class CombatantRules
{
    public static CombatantState Create(
        string combatantId,
        int maximumHitPoints,
        CombatantZeroHitPointPolicy zeroHitPointPolicy)
    {
        if (string.IsNullOrWhiteSpace(combatantId))
        {
            throw new ArgumentException(
                "Combatant ID is required.",
                nameof(combatantId));
        }

        ValidateZeroHitPointPolicy(
            zeroHitPointPolicy,
            nameof(zeroHitPointPolicy));

        return new CombatantState
        {
            CombatantId = combatantId,
            ZeroHitPointPolicy = zeroHitPointPolicy,
            Health = CombatantHealthRules.Create(
                maximumHitPoints)
        };
    }

    public static CombatantDamageResult ResolveDamage(
        CombatantState state,
        int damageAmount,
        bool isCriticalHit)
    {
        ArgumentNullException.ThrowIfNull(state);

        ValidateState(state);

        if (state.IsTerminal)
        {
            throw new InvalidOperationException(
                "A terminal combatant cannot take damage.");
        }

        CombatantHealthDamageResult healthDamage =
            CombatantHealthRules.ResolveDamage(
                state.Health,
                damageAmount,
                isCriticalHit);

        CombatantHealthState resolvedHealth =
            healthDamage.State;

        if (state.ZeroHitPointPolicy
                == CombatantZeroHitPointPolicy.Defeated
            && resolvedHealth.HitPoints.IsAtZeroHitPoints)
        {
            resolvedHealth = resolvedHealth with
            {
                DeathSavingThrows =
                    DeathSavingThrowRules.Create(),
                IsInstantlyDead = false
            };

            healthDamage = healthDamage with
            {
                ZeroHitPointDamage = null,
                State = resolvedHealth
            };
        }

        CombatantState resolvedState = state with
        {
            Health = resolvedHealth
        };

        ValidateState(resolvedState);

        return new CombatantDamageResult
        {
            HealthDamage = healthDamage,
            State = resolvedState
        };
    }

    public static CombatantHealingResult ResolveHealing(
        CombatantState state,
        int healingAmount)
    {
        ArgumentNullException.ThrowIfNull(state);

        ValidateState(state);

        if (state.IsTerminal)
        {
            throw new InvalidOperationException(
                "A terminal combatant cannot regain hit points.");
        }

        CombatantHealthHealingResult healthHealing =
            CombatantHealthRules.ResolveHealing(
                state.Health,
                healingAmount);

        CombatantState resolvedState = state with
        {
            Health = healthHealing.State
        };

        ValidateState(resolvedState);

        return new CombatantHealingResult
        {
            HealthHealing = healthHealing,
            State = resolvedState
        };
    }

    public static CombatantDeathSavingThrowResult
        ResolveDeathSavingThrow(
            CombatantState state,
            D20RollMode rollMode,
            int firstRoll,
            int? secondRoll,
            int savingThrowBonus)
    {
        ArgumentNullException.ThrowIfNull(state);

        ValidateState(state);

        if (state.ZeroHitPointPolicy
            != CombatantZeroHitPointPolicy.DeathSavingThrows)
        {
            throw new InvalidOperationException(
                "This combatant does not use death saving throws.");
        }

        if (state.IsTerminal)
        {
            throw new InvalidOperationException(
                "A terminal combatant cannot make death saving throws.");
        }

        CombatantHealthDeathSavingThrowResult
            healthDeathSavingThrow =
                CombatantHealthRules.ResolveDeathSavingThrow(
                    state.Health,
                    rollMode,
                    firstRoll,
                    secondRoll,
                    savingThrowBonus);

        CombatantState resolvedState = state with
        {
            Health = healthDeathSavingThrow.State
        };

        ValidateState(resolvedState);

        return new CombatantDeathSavingThrowResult
        {
            HealthDeathSavingThrow =
                healthDeathSavingThrow,
            State = resolvedState
        };
    }

    internal static void ValidateState(
        CombatantState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (string.IsNullOrWhiteSpace(state.CombatantId))
        {
            throw new ArgumentException(
                "Combatant ID is required.",
                nameof(state));
        }

        ValidateZeroHitPointPolicy(
            state.ZeroHitPointPolicy,
            nameof(state));

        ArgumentNullException.ThrowIfNull(state.Health);

        CombatantHealthRules.ValidateState(
            state.Health);

        if (state.ZeroHitPointPolicy
        == CombatantZeroHitPointPolicy.Defeated
    && (state.Health.IsInstantlyDead
        || state.Health.DeathSavingThrows.SuccessCount != 0
        || state.Health.DeathSavingThrows.FailureCount != 0
        || state.Health.DeathSavingThrows.IsStable))
        {
            throw new ArgumentException(
                "A combatant defeated at 0 hit points cannot be dead or have death saving throw progress.",
                nameof(state));
        }
    }

    private static void ValidateZeroHitPointPolicy(
        CombatantZeroHitPointPolicy zeroHitPointPolicy,
        string parameterName)
    {
        if (!Enum.IsDefined(zeroHitPointPolicy))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                zeroHitPointPolicy,
                "Unsupported zero-hit-point policy.");
        }
    }
}

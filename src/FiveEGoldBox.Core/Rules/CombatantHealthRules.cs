namespace FiveEGoldBox.Core.Rules;

public static class CombatantHealthRules
{
    public static CombatantHealthState Create(
        int maximumHitPoints)
    {
        return new CombatantHealthState
        {
            HitPoints = HitPointRules.Create(
                maximumHitPoints),
            DeathSavingThrows =
                DeathSavingThrowRules.Create(),
            IsInstantlyDead = false
        };
    }

    public static CombatantHealthDamageResult ResolveDamage(
        CombatantHealthState state,
        int damageAmount,
        bool isCriticalHit)
    {
        ArgumentNullException.ThrowIfNull(state);

        ValidateState(state);

        if (state.IsDead)
        {
            throw new InvalidOperationException(
                "A dead creature cannot take damage.");
        }

        HitPointDamageResult hitPointDamage =
            HitPointRules.ResolveDamage(
                state.HitPoints,
                damageAmount);

        int damageAtZeroHitPoints =
            hitPointDamage.StartedAtZeroHitPoints
                ? damageAmount
                : hitPointDamage
                    .DamageRemainingAfterReachingZeroHitPoints;

        bool tookDamageWhileAtZero =
            hitPointDamage.StartedAtZeroHitPoints
            && damageAmount > 0;

        bool sufferedMassiveDamage =
            hitPointDamage.ReducedToZeroHitPoints
            && damageAtZeroHitPoints
                >= hitPointDamage.State.MaximumHitPoints;

        ZeroHitPointDamageResult? zeroHitPointDamage =
            null;

        if (tookDamageWhileAtZero
            || sufferedMassiveDamage)
        {
            zeroHitPointDamage =
                ZeroHitPointDamageRules.ResolveDamage(
                    state.DeathSavingThrows,
                    hitPointDamage.State.MaximumHitPoints,
                    damageAtZeroHitPoints,
                    isCriticalHit);
        }

        DeathSavingThrowState
            resolvedDeathSavingThrows =
                zeroHitPointDamage?.State
                ?? state.DeathSavingThrows;

        bool isInstantlyDead =
            zeroHitPointDamage?.Outcome
                == ZeroHitPointDamageOutcome.InstantDeath;

        CombatantHealthState resolvedState = new()
        {
            HitPoints = hitPointDamage.State,
            DeathSavingThrows =
                resolvedDeathSavingThrows,
            IsInstantlyDead = isInstantlyDead
        };

        return new CombatantHealthDamageResult
        {
            IsCriticalHit = isCriticalHit,
            HitPointDamage = hitPointDamage,
            ZeroHitPointDamage = zeroHitPointDamage,
            State = resolvedState
        };
    }

    private static void ValidateState(
        CombatantHealthState state)
    {
        ArgumentNullException.ThrowIfNull(
            state.HitPoints);

        ArgumentNullException.ThrowIfNull(
            state.DeathSavingThrows);

        HitPointRules.ValidateState(
            state.HitPoints);

        DeathSavingThrowRules.ValidateState(
            state.DeathSavingThrows);

        if (!state.HitPoints.IsAtZeroHitPoints)
        {
            if (state.IsInstantlyDead)
            {
                throw new ArgumentException(
                    "An instantly dead creature must have 0 hit points.",
                    nameof(state));
            }

            if (state.DeathSavingThrows.SuccessCount != 0
                || state.DeathSavingThrows.FailureCount != 0
                || state.DeathSavingThrows.IsStable)
            {
                throw new ArgumentException(
                    "A creature with hit points must have no death saving throw progress.",
                    nameof(state));
            }
        }

        if (state.IsInstantlyDead
            && state.DeathSavingThrows.IsStable)
        {
            throw new ArgumentException(
                "An instantly dead creature cannot be stable.",
                nameof(state));
        }

        if (state.IsInstantlyDead
            && state.DeathSavingThrows.IsDead)
        {
            throw new ArgumentException(
                "A creature cannot have both instant-death and failed-death-save terminal states.",
                nameof(state));
        }
    }
}

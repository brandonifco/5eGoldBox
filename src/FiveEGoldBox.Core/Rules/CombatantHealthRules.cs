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
    public static CombatantHealthHealingResult ResolveHealing(
        CombatantHealthState state,
        int healingAmount)
    {
        ArgumentNullException.ThrowIfNull(state);

        ValidateState(state);

        if (state.IsDead)
        {
            throw new InvalidOperationException(
                "A dead creature cannot regain hit points.");
        }

        HitPointState resolvedHitPoints =
            HitPointRules.ApplyHealing(
                state.HitPoints,
                healingAmount);

        int hitPointsRestored =
            resolvedHitPoints.CurrentHitPoints
            - state.HitPoints.CurrentHitPoints;

        if (hitPointsRestored == 0)
        {
            return new CombatantHealthHealingResult
            {
                HealingAmount = healingAmount,
                HitPointsRestored = 0,
                ResetDeathSavingThrows = false,
                State = state
            };
        }

        bool resetDeathSavingThrows =
            state.HitPoints.IsAtZeroHitPoints
            && !resolvedHitPoints.IsAtZeroHitPoints;

        CombatantHealthState resolvedState = new()
        {
            HitPoints = resolvedHitPoints,
            DeathSavingThrows = resetDeathSavingThrows
                ? DeathSavingThrowRules.Create()
                : state.DeathSavingThrows,
            IsInstantlyDead = false
        };

        return new CombatantHealthHealingResult
        {
            HealingAmount = healingAmount,
            HitPointsRestored = hitPointsRestored,
            ResetDeathSavingThrows =
                resetDeathSavingThrows,
            State = resolvedState
        };
    }

    public static CombatantHealthDeathSavingThrowResult
        ResolveDeathSavingThrow(
            CombatantHealthState state,
            D20RollMode rollMode,
            int firstRoll,
            int? secondRoll,
            int savingThrowBonus)
    {
        ArgumentNullException.ThrowIfNull(state);

        ValidateState(state);

        if (state.IsDead)
        {
            throw new InvalidOperationException(
                "A dead creature cannot make death saving throws.");
        }

        if (!state.HitPoints.IsAtZeroHitPoints)
        {
            throw new InvalidOperationException(
                "A creature with hit points cannot make death saving throws.");
        }

        DeathSavingThrowResult deathSavingThrow =
            DeathSavingThrowRules.ResolveDeathSavingThrow(
                state.DeathSavingThrows,
                rollMode,
                firstRoll,
                secondRoll,
                savingThrowBonus);

        HitPointState resolvedHitPoints =
            deathSavingThrow.Outcome
                == DeathSavingThrowOutcome.RegainedHitPoint
            ? HitPointRules.ApplyHealing(
                state.HitPoints,
                healingAmount: 1)
            : state.HitPoints;

        CombatantHealthState resolvedState = new()
        {
            HitPoints = resolvedHitPoints,
            DeathSavingThrows =
                deathSavingThrow.State,
            IsInstantlyDead = false
        };

        return new CombatantHealthDeathSavingThrowResult
        {
            DeathSavingThrow = deathSavingThrow,
            State = resolvedState
        };
    }

    internal static void ValidateState(
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

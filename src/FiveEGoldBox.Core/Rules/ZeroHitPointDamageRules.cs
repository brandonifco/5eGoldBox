namespace FiveEGoldBox.Core.Rules;

public static class ZeroHitPointDamageRules
{
    public static ZeroHitPointDamageResult ResolveDamage(
        DeathSavingThrowState state,
        int maximumHitPoints,
        int damageAmount,
        bool isCriticalHit)
    {
        ArgumentNullException.ThrowIfNull(state);

        DeathSavingThrowRules.ValidateState(state);

        if (maximumHitPoints <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumHitPoints),
                maximumHitPoints,
                "Maximum hit points must be greater than 0.");
        }

        if (damageAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(damageAmount),
                damageAmount,
                "Damage amount must not be negative.");
        }

        if (state.IsDead)
        {
            throw new InvalidOperationException(
                "A dead creature cannot take damage at 0 hit points.");
        }

        if (damageAmount == 0)
        {
            return new ZeroHitPointDamageResult
            {
                MaximumHitPoints = maximumHitPoints,
                DamageAmount = damageAmount,
                IsCriticalHit = isCriticalHit,
                DeathSavingThrowFailuresCaused = 0,
                Outcome = ZeroHitPointDamageOutcome.NoEffect,
                State = state
            };
        }

        if (damageAmount >= maximumHitPoints)
        {
            return new ZeroHitPointDamageResult
            {
                MaximumHitPoints = maximumHitPoints,
                DamageAmount = damageAmount,
                IsCriticalHit = isCriticalHit,
                DeathSavingThrowFailuresCaused = 0,
                Outcome = ZeroHitPointDamageOutcome.InstantDeath,
                State = state with
                {
                    IsStable = false
                }
            };
        }

        int failuresCaused = isCriticalHit
            ? 2
            : 1;

        DeathSavingThrowState activeState = state with
        {
            IsStable = false
        };

        int resolvedFailureCount = Math.Min(
            DeathSavingThrowRules.FailuresRequired,
            activeState.FailureCount + failuresCaused);

        int failuresApplied =
            resolvedFailureCount - activeState.FailureCount;

        DeathSavingThrowState resolvedState = activeState with
        {
            FailureCount = resolvedFailureCount
        };

        ZeroHitPointDamageOutcome outcome = resolvedState.IsDead
            ? ZeroHitPointDamageOutcome.Dead
            : ZeroHitPointDamageOutcome.DeathSavingThrowFailure;

        return new ZeroHitPointDamageResult
        {
            MaximumHitPoints = maximumHitPoints,
            DamageAmount = damageAmount,
            IsCriticalHit = isCriticalHit,
            DeathSavingThrowFailuresCaused = failuresApplied,
            Outcome = outcome,
            State = resolvedState
        };
    }
}

namespace FiveEGoldBox.Core.Rules;

public static class HitPointRules
{
    public static HitPointState Create(
        int maximumHitPoints)
    {
        if (maximumHitPoints <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumHitPoints),
                maximumHitPoints,
                "Maximum hit points must be greater than 0.");
        }

        return new HitPointState
        {
            MaximumHitPoints = maximumHitPoints,
            CurrentHitPoints = maximumHitPoints,
            TemporaryHitPoints = 0
        };
    }

    public static HitPointState ApplyDamage(
        HitPointState state,
        int damageAmount)
    {
        ArgumentNullException.ThrowIfNull(state);

        ValidateState(state);

        if (damageAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(damageAmount),
                damageAmount,
                "Damage amount must not be negative.");
        }

        if (damageAmount == 0)
        {
            return state;
        }

        int damageAbsorbedByTemporaryHitPoints = Math.Min(
            state.TemporaryHitPoints,
            damageAmount);

        int remainingDamage = damageAmount - damageAbsorbedByTemporaryHitPoints;

        int newTemporaryHitPoints = state.TemporaryHitPoints - damageAbsorbedByTemporaryHitPoints;

        int newCurrentHitPoints = Math.Max(
            0,
            state.CurrentHitPoints - remainingDamage);

        return state with
        {
            CurrentHitPoints = newCurrentHitPoints,
            TemporaryHitPoints = newTemporaryHitPoints
        };
    }

    public static HitPointState ApplyHealing(
        HitPointState state,
        int healingAmount)
    {
        ArgumentNullException.ThrowIfNull(state);

        ValidateState(state);

        if (healingAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(healingAmount),
                healingAmount,
                "Healing amount must not be negative.");
        }

        if (healingAmount == 0)
        {
            return state;
        }

        return state with
        {
            CurrentHitPoints = Math.Min(
                state.MaximumHitPoints,
                state.CurrentHitPoints + healingAmount)
        };
    }

    public static HitPointState ApplyTemporaryHitPoints(
        HitPointState state,
        int temporaryHitPoints)
    {
        ArgumentNullException.ThrowIfNull(state);

        ValidateState(state);

        if (temporaryHitPoints < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(temporaryHitPoints),
                temporaryHitPoints,
                "Temporary hit points must not be negative.");
        }

        if (temporaryHitPoints <= state.TemporaryHitPoints)
        {
            return state;
        }

        return state with
        {
            TemporaryHitPoints = temporaryHitPoints
        };
    }

    private static void ValidateState(
        HitPointState state)
    {
        if (state.MaximumHitPoints <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state.MaximumHitPoints,
                "Maximum hit points must be greater than 0.");
        }

        if (state.CurrentHitPoints < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state.CurrentHitPoints,
                "Current hit points must not be negative.");
        }

        if (state.CurrentHitPoints > state.MaximumHitPoints)
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state.CurrentHitPoints,
                "Current hit points must not exceed maximum hit points.");
        }

        if (state.TemporaryHitPoints < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state.TemporaryHitPoints,
                "Temporary hit points must not be negative.");
        }
    }
}
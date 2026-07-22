namespace FiveEGoldBox.Core.Rules;

public static class DeathSavingThrowRules
{
    public const int DifficultyClass = 10;

    public const int SuccessesRequired = 3;

    public const int FailuresRequired = 3;

    public static DeathSavingThrowState Create()
    {
        return new DeathSavingThrowState
        {
            SuccessCount = 0,
            FailureCount = 0,
            IsStable = false
        };
    }

    public static DeathSavingThrowResult ResolveDeathSavingThrow(
        DeathSavingThrowState state,
        D20RollMode rollMode,
        int firstRoll,
        int? secondRoll,
        int savingThrowBonus)
    {
        ArgumentNullException.ThrowIfNull(state);

        ValidateState(state);

        if (state.IsStable)
        {
            throw new InvalidOperationException(
                "A stable creature does not make death saving throws.");
        }

        if (state.IsDead)
        {
            throw new InvalidOperationException(
                "A dead creature cannot make death saving throws.");
        }

        int naturalRoll = D20Rules.ResolveNaturalRoll(
            rollMode,
            firstRoll,
            secondRoll);

        int total = D20Rules.ResolveTotal(
            naturalRoll,
            savingThrowBonus);

        DeathSavingThrowOutcome outcome;
        DeathSavingThrowState resolvedState;

        if (naturalRoll == 20)
        {
            outcome = DeathSavingThrowOutcome.RegainedHitPoint;
            resolvedState = Create();
        }
        else if (naturalRoll == 1)
        {
            resolvedState = ApplyFailures(
                state,
                failureCount: 2);

            outcome = resolvedState.IsDead
                ? DeathSavingThrowOutcome.Dead
                : DeathSavingThrowOutcome.Failure;
        }
        else if (total >= DifficultyClass)
        {
            resolvedState = ApplySuccess(state);

            outcome = resolvedState.IsStable
                ? DeathSavingThrowOutcome.Stabilized
                : DeathSavingThrowOutcome.Success;
        }
        else
        {
            resolvedState = ApplyFailures(
                state,
                failureCount: 1);

            outcome = resolvedState.IsDead
                ? DeathSavingThrowOutcome.Dead
                : DeathSavingThrowOutcome.Failure;
        }

        return new DeathSavingThrowResult
        {
            RollMode = rollMode,
            FirstRoll = firstRoll,
            SecondRoll = secondRoll,
            NaturalRoll = naturalRoll,
            SavingThrowBonus = savingThrowBonus,
            Total = total,
            DifficultyClass = DifficultyClass,
            Outcome = outcome,
            State = resolvedState
        };
    }

    private static DeathSavingThrowState ApplySuccess(
        DeathSavingThrowState state)
    {
        int successCount = state.SuccessCount + 1;

        if (successCount >= SuccessesRequired)
        {
            return new DeathSavingThrowState
            {
                SuccessCount = 0,
                FailureCount = 0,
                IsStable = true
            };
        }

        return state with
        {
            SuccessCount = successCount
        };
    }

    private static DeathSavingThrowState ApplyFailures(
        DeathSavingThrowState state,
        int failureCount)
    {
        int resolvedFailureCount = Math.Min(
            FailuresRequired,
            state.FailureCount + failureCount);

        return state with
        {
            FailureCount = resolvedFailureCount
        };
    }

    internal static void ValidateState(
        DeathSavingThrowState state)
    {
        if (state.SuccessCount is < 0 or >= SuccessesRequired)
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state.SuccessCount,
                $"Death saving throw successes must be between 0 and {SuccessesRequired - 1}.");
        }

        if (state.FailureCount is < 0 or > FailuresRequired)
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state.FailureCount,
                $"Death saving throw failures must be between 0 and {FailuresRequired}.");
        }

        if (state.IsStable && state.IsDead)
        {
            throw new ArgumentException(
                "A creature cannot be both stable and dead.",
                nameof(state));
        }

        if (state.IsStable
            && (state.SuccessCount != 0 || state.FailureCount != 0))
        {
            throw new ArgumentException(
                "A stable creature must have no recorded death saving throw successes or failures.",
                nameof(state));
        }
    }
}

namespace FiveEGoldBox.Core.Rules;

public sealed record DeathSavingThrowState
{
    public required int SuccessCount { get; init; }

    public required int FailureCount { get; init; }

    public required bool IsStable { get; init; }

    public bool IsDead =>
        FailureCount >= DeathSavingThrowRules.FailuresRequired;
}
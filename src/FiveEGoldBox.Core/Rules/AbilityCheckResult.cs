namespace FiveEGoldBox.Core.Rules;

public sealed record AbilityCheckResult
{
    public required Ability Ability { get; init; }

    public required D20TestResult Test { get; init; }
}

namespace FiveEGoldBox.Core.Rules;

internal sealed record AbilityContestResult
{
    public required AbilityContestantResult FirstContestant { get; init; }

    public required AbilityContestantResult SecondContestant { get; init; }

    public required D20ContestOutcome Outcome { get; init; }
}

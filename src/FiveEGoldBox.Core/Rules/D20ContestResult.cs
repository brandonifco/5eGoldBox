namespace FiveEGoldBox.Core.Rules;

public sealed record D20ContestResult
{
    public required D20ContestantResult FirstContestant { get; init; }

    public required D20ContestantResult SecondContestant { get; init; }

    public required D20ContestOutcome Outcome { get; init; }
}

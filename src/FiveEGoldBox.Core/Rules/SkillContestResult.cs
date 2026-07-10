namespace FiveEGoldBox.Core.Rules;

public sealed record SkillContestResult
{
    public required SkillContestantResult FirstContestant { get; init; }

    public required SkillContestantResult SecondContestant { get; init; }

    public required D20ContestOutcome Outcome { get; init; }
}
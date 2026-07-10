namespace FiveEGoldBox.Core.Rules;

public sealed record AbilityContestantResult
{
    public required Ability Ability { get; init; }

    public required D20ContestantResult Contestant { get; init; }
}
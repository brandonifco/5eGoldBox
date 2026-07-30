namespace FiveEGoldBox.Core.Rules;

internal sealed record SkillContestantResult
{
    public required string SkillId { get; init; }

    public required Ability Ability { get; init; }

    public required D20ContestantResult Contestant { get; init; }
}

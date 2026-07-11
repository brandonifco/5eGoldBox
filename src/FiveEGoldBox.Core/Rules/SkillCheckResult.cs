namespace FiveEGoldBox.Core.Rules;

public sealed record SkillCheckResult
{
    public required string SkillId { get; init; }

    public required Ability Ability { get; init; }

    public required D20TestResult Test { get; init; }
}

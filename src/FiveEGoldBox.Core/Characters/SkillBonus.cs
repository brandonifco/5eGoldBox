using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Characters;

public sealed record SkillBonus
{
    public required string SkillId { get; init; }

    public required string SkillName { get; init; }

    public required Ability Ability { get; init; }

    public required int AbilityModifier { get; init; }

    public required bool IsProficient { get; init; }

    public required int ProficiencyBonus { get; init; }

    public required int TotalBonus { get; init; }

    public bool HasDisadvantage { get; init; }
}

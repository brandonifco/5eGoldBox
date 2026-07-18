using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterSavingThrowResult
{
    public required string TargetCombatantId { get; init; }

    public required Ability Ability { get; init; }

    public required SavingThrowBonus BaseSavingThrowBonus { get; init; }

    public required EncounterSavingThrowCoverPolicy CoverPolicy { get; init; }

    public required EncounterSavingThrowCoverDisposition
        CoverDisposition
    { get; init; }

    public EncounterLineOfSightResult? LineOfSight { get; init; }

    public EncounterCoverEvaluation? Cover { get; init; }

    public int? AppliedCoverBonus { get; init; }

    public int? CombinedSavingThrowBonus { get; init; }

    public SavingThrowResult? SavingThrow { get; init; }
}

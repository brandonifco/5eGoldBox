using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

internal sealed record EncounterWeaponAttackPrerequisiteEvaluation
{
    public required bool IsLegal { get; init; }

    public required EncounterActionUnavailabilityReason
        UnavailabilityReason { get; init; }

    public required D20RollMode? AttackRollMode { get; init; }

    public required int? DistanceFeet { get; init; }

    public required EncounterLineOfSightResult?
        LineOfSight { get; init; }

    public EncounterCoverEvaluation Cover { get; init; } =
        new()
        {
            CoverLevel = EncounterCoverLevel.None,
            ArmorClassBonus = 0,
            DexteritySavingThrowBonus = 0,
            CoverPosition = null
        };
}

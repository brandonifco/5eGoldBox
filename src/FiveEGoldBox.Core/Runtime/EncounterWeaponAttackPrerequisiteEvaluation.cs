using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterWeaponAttackPrerequisiteEvaluation
{
    public required bool IsLegal { get; init; }

    public required EncounterActionUnavailabilityReason
        UnavailabilityReason { get; init; }

    public required D20RollMode? AttackRollMode { get; init; }

    public required int? DistanceFeet { get; init; }

    public required EncounterLineOfSightResult?
        LineOfSight { get; init; }
}

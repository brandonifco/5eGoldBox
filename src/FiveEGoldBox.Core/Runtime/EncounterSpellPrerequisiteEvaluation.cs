using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterSpellPrerequisiteEvaluation
{
    public required bool IsLegal { get; init; }

    public required EncounterActionUnavailabilityReason
        UnavailabilityReason { get; init; }

    /// Set only for a spell resolved by an attack roll. A spell resisted by a
    /// saving throw, or one that simply lands, has no roll mode because the
    /// caster rolls nothing.
    public required D20RollMode? AttackRollMode { get; init; }

    public required int? DistanceFeet { get; init; }

    public required EncounterLineOfSightResult? LineOfSight { get; init; }

    public EncounterCoverEvaluation? Cover { get; init; }
}

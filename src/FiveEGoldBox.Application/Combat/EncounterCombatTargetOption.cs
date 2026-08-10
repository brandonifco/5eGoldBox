using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal sealed record EncounterCombatTargetOption
{
    public required string TargetCombatantId { get; init; }

    public required bool IsAvailable { get; init; }

    public required EncounterActionUnavailabilityReason UnavailabilityReason { get; init; }

    public D20RollMode? AttackRollMode { get; init; }

    public int? DistanceFeet { get; init; }

    /// See CombatTargetOption.Cover — carried here too so the write path's
    /// own decision surface says the same thing the read path's does.
    public EncounterCoverEvaluation? Cover { get; init; }

    /// Set only for a spell target resolved by a saving throw.
    public Ability? SaveAbility { get; init; }

    public int? SaveDc { get; init; }
}

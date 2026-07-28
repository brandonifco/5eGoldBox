using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal sealed record WatchtowerCombatTargetOption
{
    public required string TargetCombatantId { get; init; }

    public required bool IsAvailable { get; init; }

    public required EncounterActionUnavailabilityReason UnavailabilityReason { get; init; }

    public D20RollMode? AttackRollMode { get; init; }

    public int? DistanceFeet { get; init; }

    /// Set only for a spell target resolved by a saving throw.
    public Ability? SaveAbility { get; init; }

    public int? SaveDc { get; init; }
}

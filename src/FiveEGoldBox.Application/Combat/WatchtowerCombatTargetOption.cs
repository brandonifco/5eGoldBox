using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

public sealed record WatchtowerCombatTargetOption
{
    public required string TargetCombatantId { get; init; }

    public required bool IsAvailable { get; init; }

    public required EncounterActionUnavailabilityReason UnavailabilityReason { get; init; }

    public D20RollMode? AttackRollMode { get; init; }

    public int? DistanceFeet { get; init; }
}

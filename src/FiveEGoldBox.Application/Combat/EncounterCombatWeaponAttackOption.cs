using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal sealed record EncounterCombatWeaponAttackOption
{
    public required string WeaponId { get; init; }

    public required bool IsAvailable { get; init; }

    public required EncounterActionUnavailabilityReason UnavailabilityReason { get; init; }

    public required IReadOnlyList<EncounterCombatTargetOption> Targets { get; init; }
}

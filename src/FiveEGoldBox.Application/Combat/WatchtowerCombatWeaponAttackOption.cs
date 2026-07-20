using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

public sealed record WatchtowerCombatWeaponAttackOption
{
    public required string WeaponId { get; init; }

    public required bool IsAvailable { get; init; }

    public required EncounterActionUnavailabilityReason UnavailabilityReason { get; init; }

    public required IReadOnlyList<WatchtowerCombatTargetOption> Targets { get; init; }
}

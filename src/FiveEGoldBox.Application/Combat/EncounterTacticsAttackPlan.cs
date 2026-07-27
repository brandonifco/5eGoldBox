namespace FiveEGoldBox.Application.Combat;

internal sealed record EncounterTacticsAttackPlan
{
    internal required string TargetCombatantId { get; init; }

    internal required string WeaponId { get; init; }
}

namespace FiveEGoldBox.Application.Combat;

public sealed record CombatWeaponAttackIntent
{
    public required long ExpectedEncounterRevision { get; init; }

    public required string ActorCombatantId { get; init; }

    public required string WeaponId { get; init; }

    public required string TargetCombatantId { get; init; }
}

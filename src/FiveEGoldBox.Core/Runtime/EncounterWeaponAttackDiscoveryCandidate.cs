namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterWeaponAttackDiscoveryCandidate
{
    public required string ActionOptionId { get; init; }

    public required string ActorCombatantId { get; init; }

    public required string TargetCombatantId { get; init; }

    public required string WeaponId { get; init; }
}

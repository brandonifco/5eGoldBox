using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterWeaponAttackResult
{
    public required string ActorCombatantId { get; init; }

    public required string TargetCombatantId { get; init; }

    public required string WeaponId { get; init; }

    public required int DistanceFeet { get; init; }

    public required EncounterLineOfSightResult
        LineOfSight
    { get; init; }

    public required AttackResolutionResult Attack { get; init; }

    public CombatantDamageResult? TargetDamage { get; init; }

    public required EncounterState State { get; init; }
}

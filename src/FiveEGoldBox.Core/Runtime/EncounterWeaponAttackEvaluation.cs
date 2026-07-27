using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

internal sealed record EncounterWeaponAttackEvaluation
{
    public required long EncounterRevision { get; init; }

    public required string ActorCombatantId { get; init; }

    public required string TargetCombatantId { get; init; }

    public required string WeaponId { get; init; }

    public required EncounterWeaponAttackPrerequisiteEvaluation
        Prerequisites
    { get; init; }

    public required AttackRollResult AttackRoll { get; init; }

    public required DamageDice? RequiredDamageDice { get; init; }

    /// What the attacker adds to the damage, and the dice the caller owes for
    /// it. Empty on a miss, because a miss deals no damage for anything to
    /// contribute to — which is why this is decided here, after the attack
    /// roll, rather than with the prerequisites.
    public RollContributionSet DamageContributions { get; init; } =
        RollContributionSet.None(
            RollContributionTarget.DamageRoll);
}

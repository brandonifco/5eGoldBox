using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterWeaponAttackEvaluation
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
}

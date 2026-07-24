using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal static class CombatAttackStaging
{
    internal static CombatAttackAvailability EvaluateAvailability(
        EncounterState encounter,
        string actorCombatantId,
        string targetCombatantId,
        string weaponId)
    {
        EncounterWeaponAttackPrerequisiteEvaluation evaluation =
            EncounterWeaponAttackPrerequisiteRules.Evaluate(
                encounter,
                actorCombatantId,
                targetCombatantId,
                weaponId);

        return new CombatAttackAvailability(
            evaluation.IsLegal,
            evaluation.UnavailabilityReason,
            evaluation.AttackRollMode,
            evaluation.DistanceFeet);
    }

    internal static CombatAttackEvaluation Evaluate(
        EncounterState encounter,
        long expectedRevision,
        string actorCombatantId,
        string targetCombatantId,
        string weaponId,
        int firstAttackRoll,
        int? secondAttackRoll)
    {
        EncounterWeaponAttackEvaluation evaluation =
            EncounterWeaponAttackRules.Evaluate(
                encounter,
                new EncounterWeaponAttackEvaluationCommand
                {
                    ExpectedRevision = expectedRevision,
                    ActorCombatantId = actorCombatantId,
                    TargetCombatantId = targetCombatantId,
                    WeaponId = weaponId,
                    FirstAttackRoll = firstAttackRoll,
                    SecondAttackRoll = secondAttackRoll
                });

        return new CombatAttackEvaluation(
            evaluation.RequiredDamageDice);
    }
}

internal sealed record CombatAttackAvailability(
    bool IsLegal,
    EncounterActionUnavailabilityReason UnavailabilityReason,
    D20RollMode? AttackRollMode,
    int? DistanceFeet);

internal sealed record CombatAttackEvaluation(
    DamageDice? RequiredDamageDice);

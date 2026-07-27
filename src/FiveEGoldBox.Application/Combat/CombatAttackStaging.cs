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
            evaluation.DistanceFeet,
            evaluation.AttackRollContributions);
    }

    internal static CombatAttackEvaluation Evaluate(
        EncounterState encounter,
        long expectedRevision,
        string actorCombatantId,
        string targetCombatantId,
        string weaponId,
        int firstAttackRoll,
        int? secondAttackRoll,
        IReadOnlyList<int> contributionRolls)
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
                    SecondAttackRoll = secondAttackRoll,
                    ContributionRolls = contributionRolls
                });

        return new CombatAttackEvaluation(
            evaluation.RequiredDamageDice,
            evaluation.DamageContributions);
    }
}

internal sealed record CombatAttackAvailability(
    bool IsLegal,
    EncounterActionUnavailabilityReason UnavailabilityReason,
    D20RollMode? AttackRollMode,
    int? DistanceFeet,
    RollContributionSet AttackRollContributions);

internal sealed record CombatAttackEvaluation(
    DamageDice? RequiredDamageDice,
    RollContributionSet DamageContributions);

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
        string weaponId,
        EncounterWeaponAttackTiming timing =
            EncounterWeaponAttackTiming.Action)
    {
        EncounterWeaponAttackPrerequisiteEvaluation evaluation =
            EncounterWeaponAttackPrerequisiteRules.Evaluate(
                encounter,
                actorCombatantId,
                targetCombatantId,
                weaponId,
                timing);

        return new CombatAttackAvailability(
            evaluation.IsLegal,
            evaluation.UnavailabilityReason,
            evaluation.AttackRollMode,
            evaluation.DistanceFeet,
            evaluation.AttackRollContributions,
            evaluation.Cover);
    }

    internal static CombatAttackEvaluation Evaluate(
        EncounterState encounter,
        long expectedRevision,
        string actorCombatantId,
        string targetCombatantId,
        string weaponId,
        int firstAttackRoll,
        int? secondAttackRoll,
        IReadOnlyList<int> contributionRolls,
        EncounterWeaponAttackTiming timing =
            EncounterWeaponAttackTiming.Action)
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
                    Timing = timing,
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
    RollContributionSet AttackRollContributions,
    EncounterCoverEvaluation Cover);

internal sealed record CombatAttackEvaluation(
    DamageDice? RequiredDamageDice,
    RollContributionSet DamageContributions);

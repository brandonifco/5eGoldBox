using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal static class CombatSpellAttackStaging
{
    internal static CombatSpellAttackAvailability EvaluateAvailability(
        EncounterState encounter,
        string actorCombatantId,
        string targetCombatantId,
        string spellId)
    {
        EncounterSpellPrerequisiteEvaluation evaluation =
            EncounterSpellPrerequisiteRules.Evaluate(
                encounter,
                actorCombatantId,
                targetCombatantId,
                spellId);

        return new CombatSpellAttackAvailability(
            evaluation.IsLegal,
            evaluation.UnavailabilityReason,
            evaluation.AttackRollMode,
            evaluation.DistanceFeet);
    }
}

internal sealed record CombatSpellAttackAvailability(
    bool IsLegal,
    EncounterActionUnavailabilityReason UnavailabilityReason,
    D20RollMode? AttackRollMode,
    int? DistanceFeet);

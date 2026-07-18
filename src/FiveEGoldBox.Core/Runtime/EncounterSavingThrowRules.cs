using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public static class EncounterSavingThrowRules
{
    public static EncounterSavingThrowResult Resolve(
        EncounterState state,
        string targetCombatantId,
        Ability ability,
        D20RollMode rollMode,
        int firstRoll,
        int? secondRoll,
        int difficultyClass,
        EncounterSavingThrowCoverPolicy coverPolicy,
        GridPosition? originPosition)
    {
        ArgumentNullException.ThrowIfNull(state);

        EncounterRules.ValidateState(state);

        if (string.IsNullOrWhiteSpace(
            targetCombatantId))
        {
            throw new ArgumentException(
                "Target combatant ID is required.",
                nameof(targetCombatantId));
        }

        ValidateAbility(ability);
        ValidateRollMode(rollMode);
        ValidateCoverPolicy(coverPolicy);

        EncounterParticipantState? target =
            FindParticipant(
                state,
                targetCombatantId);

        if (target is null)
        {
            throw new ArgumentException(
                $"Target '{targetCombatantId}' is not an encounter participant.",
                nameof(targetCombatantId));
        }

        SavingThrowBonus baseSavingThrowBonus =
            FindSavingThrowBonus(
                target,
                ability);

        if (ability != Ability.Dexterity)
        {
            return ResolveCompletedSavingThrow(
                targetCombatantId,
                ability,
                rollMode,
                firstRoll,
                secondRoll,
                difficultyClass,
                coverPolicy,
                EncounterSavingThrowCoverDisposition
                    .NotApplicableToAbility,
                baseSavingThrowBonus,
                lineOfSight: null,
                cover: null);
        }

        if (coverPolicy
            == EncounterSavingThrowCoverPolicy.Ignored)
        {
            EncounterSavingThrowCoverDisposition
                disposition =
                    originPosition is null
                    ? EncounterSavingThrowCoverDisposition
                        .NoMeaningfulOrigin
                    : EncounterSavingThrowCoverDisposition
                        .IgnoredByEffect;

            return ResolveCompletedSavingThrow(
                targetCombatantId,
                ability,
                rollMode,
                firstRoll,
                secondRoll,
                difficultyClass,
                coverPolicy,
                disposition,
                baseSavingThrowBonus,
                lineOfSight: null,
                cover: null);
        }

        if (originPosition is null)
        {
            throw new ArgumentException(
                "A cover-permitted Dexterity saving throw requires an origin position.",
                nameof(originPosition));
        }

        if (originPosition.Value == target.Position)
        {
            throw new ArgumentException(
                "A cover-permitted Dexterity saving throw requires an origin position different from the target position.",
                nameof(originPosition));
        }

        EncounterLineOfSightResult lineOfSight =
            EncounterLineOfSightRules.Evaluate(
                state.Battlefield,
                originPosition.Value,
                target.Position);

        if (!lineOfSight.HasLineOfSight)
        {
            return new EncounterSavingThrowResult
            {
                TargetCombatantId = targetCombatantId,
                Ability = ability,
                BaseSavingThrowBonus =
                    baseSavingThrowBonus,
                CoverPolicy = coverPolicy,
                CoverDisposition =
                    EncounterSavingThrowCoverDisposition
                        .DirectPathBlocked,
                LineOfSight = lineOfSight,
                Cover = null,
                AppliedCoverBonus = null,
                CombinedSavingThrowBonus = null,
                SavingThrow = null
            };
        }

        EncounterCoverEvaluation cover =
            EncounterCoverRules.Evaluate(
                state.Battlefield,
                lineOfSight);

        EncounterSavingThrowCoverDisposition
            coverDisposition =
                ResolveCoverDisposition(
                    cover.CoverLevel);

        return ResolveCompletedSavingThrow(
            targetCombatantId,
            ability,
            rollMode,
            firstRoll,
            secondRoll,
            difficultyClass,
            coverPolicy,
            coverDisposition,
            baseSavingThrowBonus,
            lineOfSight,
            cover);
    }

    private static EncounterParticipantState? FindParticipant(
        EncounterState state,
        string combatantId)
    {
        return state.Participants.FirstOrDefault(
            participant =>
                string.Equals(
                    participant.Combatant.CombatantId,
                    combatantId,
                    StringComparison.Ordinal));
    }

    private static SavingThrowBonus FindSavingThrowBonus(
        EncounterParticipantState target,
        Ability ability)
    {
        SavingThrowBonus? savingThrowBonus =
            target.CombatProfile.SavingThrowBonuses
                .SingleOrDefault(candidate =>
                    candidate.Ability == ability);

        if (savingThrowBonus is null)
        {
            throw new InvalidOperationException(
                $"Target '{target.Combatant.CombatantId}' does not have an encounter saving-throw bonus for '{ability}'.");
        }

        return savingThrowBonus;
    }

    private static EncounterSavingThrowResult
        ResolveCompletedSavingThrow(
            string targetCombatantId,
            Ability ability,
            D20RollMode rollMode,
            int firstRoll,
            int? secondRoll,
            int difficultyClass,
            EncounterSavingThrowCoverPolicy coverPolicy,
            EncounterSavingThrowCoverDisposition
                coverDisposition,
            SavingThrowBonus baseSavingThrowBonus,
            EncounterLineOfSightResult? lineOfSight,
            EncounterCoverEvaluation? cover)
    {
        int appliedCoverBonus =
            cover?.DexteritySavingThrowBonus
            ?? 0;

        int combinedSavingThrowBonus =
            checked(
                baseSavingThrowBonus.TotalBonus
                + appliedCoverBonus);

        SavingThrowResult savingThrow =
            SavingThrowRules.ResolveSavingThrow(
                ability,
                rollMode,
                firstRoll,
                secondRoll,
                combinedSavingThrowBonus,
                difficultyClass);

        return new EncounterSavingThrowResult
        {
            TargetCombatantId = targetCombatantId,
            Ability = ability,
            BaseSavingThrowBonus =
                baseSavingThrowBonus,
            CoverPolicy = coverPolicy,
            CoverDisposition = coverDisposition,
            LineOfSight = lineOfSight,
            Cover = cover,
            AppliedCoverBonus = appliedCoverBonus,
            CombinedSavingThrowBonus =
                combinedSavingThrowBonus,
            SavingThrow = savingThrow
        };
    }

    private static EncounterSavingThrowCoverDisposition
        ResolveCoverDisposition(
            EncounterCoverLevel coverLevel)
    {
        return coverLevel switch
        {
            EncounterCoverLevel.None =>
                EncounterSavingThrowCoverDisposition
                    .EvaluatedNoCover,

            EncounterCoverLevel.Half =>
                EncounterSavingThrowCoverDisposition
                    .HalfCoverApplied,

            EncounterCoverLevel.ThreeQuarters =>
                EncounterSavingThrowCoverDisposition
                    .ThreeQuartersCoverApplied,

            _ => throw new ArgumentOutOfRangeException(
                nameof(coverLevel),
                coverLevel,
                "Unsupported encounter cover level.")
        };
    }

    private static void ValidateAbility(
        Ability ability)
    {
        if (!Enum.IsDefined(ability))
        {
            throw new ArgumentOutOfRangeException(
                nameof(ability),
                ability,
                "Unsupported saving-throw ability.");
        }
    }

    private static void ValidateRollMode(
        D20RollMode rollMode)
    {
        if (!Enum.IsDefined(rollMode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rollMode),
                rollMode,
                "Unsupported saving-throw roll mode.");
        }
    }

    private static void ValidateCoverPolicy(
        EncounterSavingThrowCoverPolicy coverPolicy)
    {
        if (!Enum.IsDefined(coverPolicy))
        {
            throw new ArgumentOutOfRangeException(
                nameof(coverPolicy),
                coverPolicy,
                "Unsupported saving-throw cover policy.");
        }
    }
}

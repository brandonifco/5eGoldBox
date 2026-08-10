using FiveEGoldBox.Application.Randomness;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal static class EncounterCombatSpellAttackStaging
{
    internal static EncounterCombatSpellAttackAvailability EvaluateAvailability(
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

        return new EncounterCombatSpellAttackAvailability(
            evaluation.IsLegal,
            evaluation.UnavailabilityReason,
            evaluation.AttackRollMode,
            evaluation.DistanceFeet,
            evaluation.AttackRollContributions,
            evaluation.SavingThrowContributions);
    }

    internal static EncounterCombatSpellAttackExecution Resolve(
        EncounterState encounter,
        int seed,
        int cursor,
        string actorCombatantId,
        string targetCombatantId,
        string spellId,
        IReadOnlyList<string>? additionalTargetCombatantIds = null)
    {
        EncounterCombatSpellAttackAvailability availability =
            EvaluateAvailability(
                encounter,
                actorCombatantId,
                targetCombatantId,
                spellId);

        if (!availability.IsLegal)
        {
            throw new InvalidOperationException(
                $"The spell attack is unavailable for reason '{availability.UnavailabilityReason}'.");
        }

        List<EncounterCombatDieRoll> dice = [];
        int nextCursor = cursor;

        // Whichever roll the spell's own resolution needs — an attack roll
        // against the target's armour class, or the target's own saving
        // throw — is drawn first, the same way a weapon attack's roll comes
        // before its damage: what it decides (a hit, a failed save) is what
        // decides whether an effect happens at all.
        int? firstAttackRoll = null;
        int? secondAttackRoll = null;
        List<int> attackContributionValues = [];
        int? savingThrowRoll = null;
        List<int> savingThrowContributionValues = [];

        if (availability.AttackRollMode is not null)
        {
            ApplicationRandomRoll first =
                ApplicationRandomSequence.GenerateDie(
                    seed,
                    nextCursor,
                    DieType.D20);

            nextCursor = first.UpdatedValuesConsumed;
            firstAttackRoll = first.Value;
            dice.Add(CreateDie(
                first,
                CombatDiePurpose.AttackRoll));

            if (availability.AttackRollMode
                != D20RollMode.Normal)
            {
                ApplicationRandomRoll second =
                    ApplicationRandomSequence.GenerateDie(
                        seed,
                        nextCursor,
                        DieType.D20);

                nextCursor = second.UpdatedValuesConsumed;
                secondAttackRoll = second.Value;
                dice.Add(CreateDie(
                    second,
                    CombatDiePurpose.AttackRoll));
            }

            foreach (DieType die
                in availability.AttackRollContributions
                    .RequiredDice)
            {
                ApplicationRandomRoll contribution =
                    ApplicationRandomSequence.GenerateDie(
                        seed,
                        nextCursor,
                        die);

                nextCursor = contribution.UpdatedValuesConsumed;
                attackContributionValues.Add(contribution.Value);
                dice.Add(CreateDie(
                    contribution,
                    CombatDiePurpose.AttackRoll));
            }
        }
        else if (RequiresSavingThrow(
            encounter,
            actorCombatantId,
            spellId))
        {
            ApplicationRandomRoll save =
                ApplicationRandomSequence.GenerateDie(
                    seed,
                    nextCursor,
                    DieType.D20);

            nextCursor = save.UpdatedValuesConsumed;
            savingThrowRoll = save.Value;
            dice.Add(CreateDie(
                save,
                CombatDiePurpose.SavingThrow));

            foreach (DieType die
                in availability.SavingThrowContributions
                    .RequiredDice)
            {
                ApplicationRandomRoll contribution =
                    ApplicationRandomSequence.GenerateDie(
                        seed,
                        nextCursor,
                        die);

                nextCursor = contribution.UpdatedValuesConsumed;
                savingThrowContributionValues.Add(
                    contribution.Value);
                dice.Add(CreateDie(
                    contribution,
                    CombatDiePurpose.SavingThrow));
            }
        }

        EncounterSpellCastEvaluation evaluation =
            EncounterSpellRules.Evaluate(
                encounter,
                new EncounterSpellCastEvaluationCommand
                {
                    ExpectedRevision = encounter.Revision,
                    ActorCombatantId = actorCombatantId,
                    TargetCombatantId = targetCombatantId,
                    SpellId = spellId,
                    FirstAttackRoll = firstAttackRoll,
                    SecondAttackRoll = secondAttackRoll,
                    AttackContributionRolls = Array.AsReadOnly(
                        attackContributionValues.ToArray()),
                    SavingThrowRoll = savingThrowRoll,
                    SavingThrowContributionRolls = Array.AsReadOnly(
                        savingThrowContributionValues.ToArray())
                });

        List<int> effectValues = [];

        foreach (DieType die in evaluation.RequiredEffectDice)
        {
            ApplicationRandomRoll effect =
                ApplicationRandomSequence.GenerateDie(
                    seed,
                    nextCursor,
                    die);

            nextCursor = effect.UpdatedValuesConsumed;
            effectValues.Add(effect.Value);
            dice.Add(CreateDie(
                effect,
                CombatDiePurpose.EffectRoll));
        }

        // A spell that would deal damage can end whatever the target was
        // concentrating on, the same way a weapon attack can — asked for
        // here, before Resolve, for the same reason every other roll is.
        int? concentrationSavingThrowRoll = null;
        List<int> concentrationContributionValues = [];

        if (evaluation.WouldDealDamage)
        {
            EncounterParticipantState target =
                EncounterCombatDecisionFactory.FindParticipant(
                    encounter,
                    targetCombatantId);

            if (target.ConcentratingOnEffectId is not null)
            {
                ApplicationRandomRoll concentration =
                    ApplicationRandomSequence.GenerateDie(
                        seed,
                        nextCursor,
                        DieType.D20);

                nextCursor =
                    concentration.UpdatedValuesConsumed;
                concentrationSavingThrowRoll =
                    concentration.Value;
                dice.Add(CreateDie(
                    concentration,
                    CombatDiePurpose
                        .ConcentrationSavingThrow));

                foreach (DieType die
                    in availability.SavingThrowContributions
                        .RequiredDice)
                {
                    ApplicationRandomRoll contribution =
                        ApplicationRandomSequence.GenerateDie(
                            seed,
                            nextCursor,
                            die);

                    nextCursor =
                        contribution.UpdatedValuesConsumed;
                    concentrationContributionValues.Add(
                        contribution.Value);
                    dice.Add(CreateDie(
                        contribution,
                        CombatDiePurpose
                            .ConcentrationSavingThrow));
                }
            }
        }

        EncounterSpellCastResult result =
            EncounterSpellRules.Resolve(
                encounter,
                new EncounterSpellCastCommand
                {
                    ExpectedRevision = encounter.Revision,
                    ActorCombatantId = actorCombatantId,
                    TargetCombatantId = targetCombatantId,
                    AdditionalTargetCombatantIds =
                        additionalTargetCombatantIds
                        ?? Array.Empty<string>(),
                    SpellId = spellId,
                    FirstAttackRoll = firstAttackRoll,
                    SecondAttackRoll = secondAttackRoll,
                    AttackContributionRolls = Array.AsReadOnly(
                        attackContributionValues.ToArray()),
                    SavingThrowRoll = savingThrowRoll,
                    SavingThrowContributionRolls = Array.AsReadOnly(
                        savingThrowContributionValues.ToArray()),
                    EffectRolls = Array.AsReadOnly(
                        effectValues.ToArray()),
                    ConcentrationSavingThrowRoll =
                        concentrationSavingThrowRoll,
                    ConcentrationSavingThrowContributionRolls =
                        Array.AsReadOnly(
                            concentrationContributionValues
                                .ToArray())
                });

        return new EncounterCombatSpellAttackExecution(
            result,
            nextCursor,
            Array.AsReadOnly(dice.ToArray()));
    }

    /// Whether this spell is resolved by a saving throw at all — decided
    /// once, before any dice are drawn, from content rather than from
    /// whichever roll happened to come back set.
    private static bool RequiresSavingThrow(
        EncounterState encounter,
        string actorCombatantId,
        string spellId)
    {
        EncounterParticipantState actor =
            EncounterCombatDecisionFactory.FindParticipant(
                encounter,
                actorCombatantId);

        SpellAttack spell = actor.CombatProfile.SpellAttacks
            .Single(candidate => string.Equals(
                candidate.SpellId,
                spellId,
                StringComparison.Ordinal));

        return spell.Resolution == SpellResolutionKind.SavingThrow;
    }

    private static EncounterCombatDieRoll CreateDie(
        ApplicationRandomRoll roll,
        CombatDiePurpose purpose)
    {
        return new EncounterCombatDieRoll
        {
            Ordinal = roll.Ordinal,
            Sides = roll.Sides,
            Value = roll.Value,
            Purpose = purpose
        };
    }
}

internal sealed record EncounterCombatSpellAttackAvailability(
    bool IsLegal,
    EncounterActionUnavailabilityReason UnavailabilityReason,
    D20RollMode? AttackRollMode,
    int? DistanceFeet,
    RollContributionSet AttackRollContributions,
    RollContributionSet SavingThrowContributions);

internal sealed record EncounterCombatSpellAttackExecution(
    EncounterSpellCastResult Result,
    int CursorAfter,
    IReadOnlyList<EncounterCombatDieRoll> Dice);

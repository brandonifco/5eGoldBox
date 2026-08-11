using FiveEGoldBox.Application.Randomness;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal static class EncounterCombatAttackStaging
{
    internal static EncounterCombatAttackAvailability EvaluateAvailability(
        EncounterState encounter,
        string actorCombatantId,
        string targetCombatantId,
        string weaponId,
        EncounterWeaponAttackTiming timing =
            EncounterWeaponAttackTiming.Action)
    {
        CombatAttackAvailability evaluation =
            CombatAttackStaging.EvaluateAvailability(
                encounter,
                actorCombatantId,
                targetCombatantId,
                weaponId,
                timing);

        return new EncounterCombatAttackAvailability(
            evaluation.IsLegal,
            evaluation.UnavailabilityReason,
            evaluation.AttackRollMode,
            evaluation.DistanceFeet,
            evaluation.AttackRollContributions,
            evaluation.Cover);
    }

    internal static EncounterCombatAttackExecution Resolve(
        EncounterState encounter,
        int seed,
        int cursor,
        string actorCombatantId,
        string targetCombatantId,
        string weaponId,
        EncounterWeaponAttackTiming timing =
            EncounterWeaponAttackTiming.Action)
    {
        EncounterCombatAttackAvailability availability =
            EvaluateAvailability(
                encounter,
                actorCombatantId,
                targetCombatantId,
                weaponId,
                timing);

        if (!availability.IsLegal
            || availability.AttackRollMode is null)
        {
            throw new InvalidOperationException(
                $"The weapon attack is unavailable for reason '{availability.UnavailabilityReason}'.");
        }

        List<EncounterCombatDieRoll> dice = [];
        int nextCursor = cursor;

        ApplicationRandomRoll first =
            ApplicationRandomSequence.GenerateDie(
                seed,
                nextCursor,
                DieType.D20);

        nextCursor = first.UpdatedValuesConsumed;
        dice.Add(CreateDie(
            first,
            CombatDiePurpose.AttackRoll));

        int? secondValue = null;

        if (availability.AttackRollMode
            != D20RollMode.Normal)
        {
            ApplicationRandomRoll second =
                ApplicationRandomSequence.GenerateDie(
                    seed,
                    nextCursor,
                    DieType.D20);

            nextCursor = second.UpdatedValuesConsumed;
            secondValue = second.Value;
            dice.Add(CreateDie(
                second,
                CombatDiePurpose.AttackRoll));
        }

        // Whatever the attacker's effects add to the roll is rolled here,
        // after the d20s and before the damage, because the contribution can
        // turn a miss into a hit and so decides whether damage is rolled at
        // all.
        List<int> contributionValues = [];

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
            contributionValues.Add(contribution.Value);
            dice.Add(CreateDie(
                contribution,
                CombatDiePurpose.AttackRoll));
        }

        IReadOnlyList<int> contributionRolls =
            Array.AsReadOnly(
                contributionValues.ToArray());

        CombatAttackEvaluation evaluation =
            CombatAttackStaging.Evaluate(
                encounter,
                encounter.Revision,
                actorCombatantId,
                targetCombatantId,
                weaponId,
                first.Value,
                secondValue,
                contributionRolls,
                timing);

        List<int> damageValues = [];
        DamageDice? requiredDamage =
            evaluation.RequiredDamageDice;

        if (requiredDamage is not null)
        {
            for (int index = 0;
                index < requiredDamage.Count;
                index++)
            {
                ApplicationRandomRoll damage =
                    ApplicationRandomSequence.GenerateDie(
                        seed,
                        nextCursor,
                        requiredDamage.Die);

                nextCursor = damage.UpdatedValuesConsumed;
                damageValues.Add(damage.Value);
                dice.Add(CreateDie(
                    damage,
                    CombatDiePurpose.DamageRoll));
            }
        }

        // Damage the attacker adds by being what it is — a rogue's Sneak
        // Attack — is drawn after the weapon's own dice, since nothing
        // downstream depends on it.
        List<int> damageContributionValues = [];

        foreach (DieType die
            in evaluation.DamageContributions.RequiredDice)
        {
            ApplicationRandomRoll contribution =
                ApplicationRandomSequence.GenerateDie(
                    seed,
                    nextCursor,
                    die);

            nextCursor = contribution.UpdatedValuesConsumed;
            damageContributionValues.Add(contribution.Value);
            dice.Add(CreateDie(
                contribution,
                CombatDiePurpose.DamageRoll));
        }

        // A hit that lands damage can end whatever the target was
        // concentrating on. Asked for here, before Resolve, for the same
        // reason every other roll in this method is.
        int? concentrationSavingThrowRoll = null;
        List<int> concentrationContributionValues = [];

        if (requiredDamage is not null)
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

                nextCursor = concentration.UpdatedValuesConsumed;
                concentrationSavingThrowRoll = concentration.Value;
                dice.Add(CreateDie(
                    concentration,
                    CombatDiePurpose
                        .ConcentrationSavingThrow));

                RollContributionSet concentrationContributions =
                    RollContributionRules.Resolve(
                        target,
                        RollContributionTarget.SavingThrow);

                foreach (DieType die
                    in concentrationContributions.RequiredDice)
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

        EncounterWeaponAttackResult result =
            EncounterWeaponAttackRules.Resolve(
                encounter,
                new EncounterWeaponAttackCommand
                {
                    ExpectedRevision = encounter.Revision,
                    ActorCombatantId = actorCombatantId,
                    TargetCombatantId = targetCombatantId,
                    WeaponId = weaponId,
                    Timing = timing,
                    FirstAttackRoll = first.Value,
                    SecondAttackRoll = secondValue,
                    ContributionRolls = contributionRolls,
                    DamageRolls = Array.AsReadOnly(
                        damageValues.ToArray()),
                    DamageContributionRolls = Array.AsReadOnly(
                        damageContributionValues.ToArray()),
                    ConcentrationSavingThrowRoll =
                        concentrationSavingThrowRoll,
                    ConcentrationSavingThrowContributionRolls =
                        Array.AsReadOnly(
                            concentrationContributionValues
                                .ToArray())
                });

        return new EncounterCombatAttackExecution(
            result,
            nextCursor,
            Array.AsReadOnly(dice.ToArray()));
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

internal sealed record EncounterCombatAttackAvailability(
    bool IsLegal,
    EncounterActionUnavailabilityReason UnavailabilityReason,
    D20RollMode? AttackRollMode,
    int? DistanceFeet,
    RollContributionSet AttackRollContributions,
    EncounterCoverEvaluation Cover);

internal sealed record EncounterCombatAttackExecution(
    EncounterWeaponAttackResult Result,
    int CursorAfter,
    IReadOnlyList<EncounterCombatDieRoll> Dice);

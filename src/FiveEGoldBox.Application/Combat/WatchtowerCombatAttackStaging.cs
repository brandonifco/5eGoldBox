using FiveEGoldBox.Application.Randomness;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal static class WatchtowerCombatAttackStaging
{
    internal static WatchtowerCombatAttackAvailability EvaluateAvailability(
        EncounterState encounter,
        string actorCombatantId,
        string targetCombatantId,
        string weaponId)
    {
        CombatAttackAvailability evaluation =
            CombatAttackStaging.EvaluateAvailability(
                encounter,
                actorCombatantId,
                targetCombatantId,
                weaponId);

        return new WatchtowerCombatAttackAvailability(
            evaluation.IsLegal,
            evaluation.UnavailabilityReason,
            evaluation.AttackRollMode,
            evaluation.DistanceFeet,
            evaluation.AttackRollContributions);
    }

    internal static WatchtowerCombatAttackExecution Resolve(
        EncounterState encounter,
        int seed,
        int cursor,
        string actorCombatantId,
        string targetCombatantId,
        string weaponId)
    {
        WatchtowerCombatAttackAvailability availability =
            EvaluateAvailability(
                encounter,
                actorCombatantId,
                targetCombatantId,
                weaponId);

        if (!availability.IsLegal
            || availability.AttackRollMode is null)
        {
            throw new InvalidOperationException(
                $"The weapon attack is unavailable for reason '{availability.UnavailabilityReason}'.");
        }

        List<WatchtowerCombatDieRoll> dice = [];
        int nextCursor = cursor;

        ApplicationRandomRoll first =
            ApplicationRandomSequence.GenerateDie(
                seed,
                nextCursor,
                DieType.D20);

        nextCursor = first.UpdatedValuesConsumed;
        dice.Add(CreateDie(
            first,
            WatchtowerCombatDiePurpose.AttackRoll));

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
                WatchtowerCombatDiePurpose.AttackRoll));
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
                WatchtowerCombatDiePurpose.AttackRoll));
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
                contributionRolls);

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
                    WatchtowerCombatDiePurpose.DamageRoll));
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
                WatchtowerCombatDiePurpose.DamageRoll));
        }

        // A hit that lands damage can end whatever the target was
        // concentrating on. Asked for here, before Resolve, for the same
        // reason every other roll in this method is.
        int? concentrationSavingThrowRoll = null;
        List<int> concentrationContributionValues = [];

        if (requiredDamage is not null)
        {
            EncounterParticipantState target =
                WatchtowerCombatDecisionFactory.FindParticipant(
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
                    WatchtowerCombatDiePurpose
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
                        WatchtowerCombatDiePurpose
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

        return new WatchtowerCombatAttackExecution(
            result,
            nextCursor,
            Array.AsReadOnly(dice.ToArray()));
    }

    private static WatchtowerCombatDieRoll CreateDie(
        ApplicationRandomRoll roll,
        WatchtowerCombatDiePurpose purpose)
    {
        return new WatchtowerCombatDieRoll
        {
            Ordinal = roll.Ordinal,
            Sides = roll.Sides,
            Value = roll.Value,
            Purpose = purpose
        };
    }
}

internal sealed record WatchtowerCombatAttackAvailability(
    bool IsLegal,
    EncounterActionUnavailabilityReason UnavailabilityReason,
    D20RollMode? AttackRollMode,
    int? DistanceFeet,
    RollContributionSet AttackRollContributions);

internal sealed record WatchtowerCombatAttackExecution(
    EncounterWeaponAttackResult Result,
    int CursorAfter,
    IReadOnlyList<WatchtowerCombatDieRoll> Dice);

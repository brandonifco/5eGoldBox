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
            evaluation.DistanceFeet);
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
                sides: 20);

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
                    sides: 20);

            nextCursor = second.UpdatedValuesConsumed;
            secondValue = second.Value;
            dice.Add(CreateDie(
                second,
                WatchtowerCombatDiePurpose.AttackRoll));
        }

        CombatAttackEvaluation evaluation =
            CombatAttackStaging.Evaluate(
                encounter,
                encounter.Revision,
                actorCombatantId,
                targetCombatantId,
                weaponId,
                first.Value,
                secondValue);

        List<int> damageValues = [];
        DamageDice? requiredDamage =
            evaluation.RequiredDamageDice;

        if (requiredDamage is not null)
        {
            int sides = (int)requiredDamage.Die;

            for (int index = 0;
                index < requiredDamage.Count;
                index++)
            {
                ApplicationRandomRoll damage =
                    ApplicationRandomSequence.GenerateDie(
                        seed,
                        nextCursor,
                        sides);

                nextCursor = damage.UpdatedValuesConsumed;
                damageValues.Add(damage.Value);
                dice.Add(CreateDie(
                    damage,
                    WatchtowerCombatDiePurpose.DamageRoll));
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
                    DamageRolls = Array.AsReadOnly(
                        damageValues.ToArray())
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
    int? DistanceFeet);

internal sealed record WatchtowerCombatAttackExecution(
    EncounterWeaponAttackResult Result,
    int CursorAfter,
    IReadOnlyList<WatchtowerCombatDieRoll> Dice);

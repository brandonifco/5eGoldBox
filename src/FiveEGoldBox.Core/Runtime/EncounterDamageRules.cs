using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public static class EncounterDamageRules
{
    public static EncounterDamageResult Resolve(
        EncounterState state,
        EncounterDamageCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        EncounterRules.ValidateState(state);
        ValidateCommand(command);

        if (state.LifecycleState
            != EncounterLifecycleState.Active)
        {
            throw new InvalidOperationException(
                "A completed encounter cannot resolve damage.");
        }

        if (command.ExpectedRevision != state.Revision)
        {
            throw new InvalidOperationException(
                $"Expected encounter revision '{command.ExpectedRevision}', but the current revision is '{state.Revision}'.");
        }

        int targetIndex =
            FindParticipantIndex(
                state,
                command.TargetCombatantId);

        if (targetIndex < 0)
        {
            throw new ArgumentException(
                $"Target '{command.TargetCombatantId}' is not an encounter participant.",
                nameof(command));
        }

        EncounterParticipantState target =
            state.Participants[targetIndex];

        if (target.Combatant.IsTerminal)
        {
            throw new InvalidOperationException(
                "A terminal combatant cannot receive encounter damage.");
        }

        long resolvedRevision =
            checked(state.Revision + 1);

        CombatantLifecycleState previousLifecycleState =
            target.Combatant.LifecycleState;

        CombatantDamageResult combatantDamage =
            CombatantRules.ResolveDamage(
                target.Combatant,
                command.DamageAmount,
                command.IsCriticalHit);

        bool clearedPendingDeathSavingThrow =
            string.Equals(
                state.PendingDeathSavingThrowCombatantId,
                command.TargetCombatantId,
                StringComparison.Ordinal)
            && combatantDamage.State.LifecycleState
                != CombatantLifecycleState.Dying;

        EncounterParticipantState[] participants =
            state.Participants.ToArray();

        participants[targetIndex] =
            target with
            {
                Combatant = combatantDamage.State
            };

        EncounterConcentrationCheckResult? concentrationCheck =
            null;

        if (target.ConcentratingOnEffectId is not null
            && command.DamageAmount > 0)
        {
            string effectId =
                target.ConcentratingOnEffectId;

            if (combatantDamage.State.LifecycleState
                != CombatantLifecycleState.Conscious)
            {
                // 5e never asks an incapacitated creature to roll for it —
                // concentration just ends.
                participants = ActiveEffectRules.Drop(
                    participants,
                    command.TargetCombatantId,
                    effectId);

                concentrationCheck =
                    new EncounterConcentrationCheckResult
                    {
                        CombatantId =
                            command.TargetCombatantId,
                        EffectId = effectId,
                        BrokenByIncapacitation = true,
                        SavingThrow = null,
                        EffectDropped = true
                    };
            }
            else
            {
                if (command.ConcentrationSavingThrowRoll
                    is null)
                {
                    throw new ArgumentException(
                        $"Target '{command.TargetCombatantId}' is concentrating on '{effectId}' and requires a concentration saving throw roll.",
                        nameof(command));
                }

                int difficultyClass = Math.Max(
                    10,
                    command.DamageAmount / 2);

                EncounterSavingThrowResult savingThrow =
                    EncounterSavingThrowRules.Resolve(
                        state,
                        command.TargetCombatantId,
                        Ability.Constitution,
                        D20RollMode.Normal,
                        command.ConcentrationSavingThrowRoll
                            .Value,
                        secondRoll: null,
                        difficultyClass,
                        EncounterSavingThrowCoverPolicy
                            .Ignored,
                        originPosition: null,
                        command
                            .ConcentrationSavingThrowContributionRolls);

                bool succeeded =
                    savingThrow.SavingThrow!.Test.Outcome
                        == D20TestOutcome.Success;

                if (!succeeded)
                {
                    participants = ActiveEffectRules.Drop(
                        participants,
                        command.TargetCombatantId,
                        effectId);
                }

                concentrationCheck =
                    new EncounterConcentrationCheckResult
                    {
                        CombatantId =
                            command.TargetCombatantId,
                        EffectId = effectId,
                        BrokenByIncapacitation = false,
                        SavingThrow = savingThrow,
                        EffectDropped = !succeeded
                    };
            }
        }

        EncounterState resolvedState =
            EncounterCompletionRules.Resolve(
                state with
                {
                    Revision = resolvedRevision,
                    Participants =
                        Array.AsReadOnly(participants),
                    PendingDeathSavingThrowCombatantId =
                        clearedPendingDeathSavingThrow
                        ? null
                        : state.PendingDeathSavingThrowCombatantId
                });

        EncounterRules.ValidateState(resolvedState);

        return new EncounterDamageResult
        {
            TargetCombatantId =
                command.TargetCombatantId,
            PreviousLifecycleState =
                previousLifecycleState,
            LifecycleState =
                combatantDamage.State.LifecycleState,
            ClearedPendingDeathSavingThrow =
                clearedPendingDeathSavingThrow,
            CombatantDamage =
                combatantDamage,
            ConcentrationCheck = concentrationCheck,
            State = resolvedState
        };
    }

    private static void ValidateCommand(
        EncounterDamageCommand command)
    {
        if (command.ExpectedRevision < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                command.ExpectedRevision,
                "Expected encounter revision must be at least 1.");
        }

        if (string.IsNullOrWhiteSpace(
            command.TargetCombatantId))
        {
            throw new ArgumentException(
                "Target combatant ID is required.",
                nameof(command));
        }

        if (command.DamageAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                command.DamageAmount,
                "Damage amount cannot be negative.");
        }
    }

    private static int FindParticipantIndex(
        EncounterState state,
        string combatantId)
    {
        for (int index = 0;
            index < state.Participants.Count;
            index++)
        {
            if (string.Equals(
                state.Participants[index]
                    .Combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }
}

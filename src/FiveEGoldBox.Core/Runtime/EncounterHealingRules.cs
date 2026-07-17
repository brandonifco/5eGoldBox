namespace FiveEGoldBox.Core.Runtime;

public static class EncounterHealingRules
{
    public static EncounterHealingResult Resolve(
        EncounterState state,
        EncounterHealingCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        EncounterRules.ValidateState(state);
        ValidateCommand(command);

        if (state.LifecycleState
            != EncounterLifecycleState.Active)
        {
            throw new InvalidOperationException(
                "A completed encounter cannot resolve healing.");
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
                "A terminal combatant cannot receive encounter healing.");
        }

        long resolvedRevision =
            checked(state.Revision + 1);

        CombatantLifecycleState previousLifecycleState =
            target.Combatant.LifecycleState;

        CombatantHealingResult combatantHealing =
            CombatantRules.ResolveHealing(
                target.Combatant,
                command.HealingAmount);

        bool clearedPendingDeathSavingThrow =
            string.Equals(
                state.PendingDeathSavingThrowCombatantId,
                command.TargetCombatantId,
                StringComparison.Ordinal)
            && combatantHealing.State.LifecycleState
                != CombatantLifecycleState.Dying;

        EncounterParticipantState[] participants =
            state.Participants.ToArray();

        participants[targetIndex] =
            target with
            {
                Combatant = combatantHealing.State
            };

        EncounterState resolvedState = state with
        {
            Revision = resolvedRevision,
            Participants =
                Array.AsReadOnly(participants),
            PendingDeathSavingThrowCombatantId =
                clearedPendingDeathSavingThrow
                ? null
                : state.PendingDeathSavingThrowCombatantId
        };

        EncounterRules.ValidateState(resolvedState);

        return new EncounterHealingResult
        {
            TargetCombatantId =
                command.TargetCombatantId,
            PreviousLifecycleState =
                previousLifecycleState,
            LifecycleState =
                combatantHealing.State.LifecycleState,
            ClearedPendingDeathSavingThrow =
                clearedPendingDeathSavingThrow,
            CombatantHealing =
                combatantHealing,
            State = resolvedState
        };
    }

    private static void ValidateCommand(
        EncounterHealingCommand command)
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

        if (command.HealingAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                command.HealingAmount,
                "Healing amount cannot be negative.");
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

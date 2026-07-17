using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public static class EncounterDeathSavingThrowRules
{
    public static EncounterDeathSavingThrowResult Resolve(
        EncounterState state,
        EncounterDeathSavingThrowCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        EncounterRules.ValidateState(state);
        ValidateCommand(command);

        if (state.LifecycleState
            != EncounterLifecycleState.Active)
        {
            throw new InvalidOperationException(
                "A completed encounter cannot resolve death saving throws.");
        }

        if (command.ExpectedRevision != state.Revision)
        {
            throw new InvalidOperationException(
                $"Expected encounter revision '{command.ExpectedRevision}', but the current revision is '{state.Revision}'.");
        }

        int participantIndex =
            FindParticipantIndex(
                state,
                command.ActorCombatantId);

        if (participantIndex < 0)
        {
            throw new ArgumentException(
                $"Actor '{command.ActorCombatantId}' is not an encounter participant.",
                nameof(command));
        }

        if (!string.Equals(
            command.ActorCombatantId,
            state.ActiveCombatantId,
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Only the active combatant can make an encounter death saving throw.");
        }

        EncounterParticipantState participant =
            state.Participants[participantIndex];

        if (participant.Combatant.LifecycleState
            != CombatantLifecycleState.Dying)
        {
            throw new InvalidOperationException(
                "Only a dying combatant can make a death saving throw.");
        }

        if (!string.Equals(
            state.PendingDeathSavingThrowCombatantId,
            command.ActorCombatantId,
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The active combatant does not have a pending death saving throw.");
        }

        long resolvedRevision =
            checked(state.Revision + 1);

        CombatantLifecycleState previousLifecycleState =
            participant.Combatant.LifecycleState;

        CombatantDeathSavingThrowResult
            combatantDeathSavingThrow =
                CombatantRules.ResolveDeathSavingThrow(
                    participant.Combatant,
                    command.RollMode,
                    command.FirstRoll,
                    command.SecondRoll,
                    command.SavingThrowBonus);

        EncounterParticipantState[] participants =
            state.Participants.ToArray();

        participants[participantIndex] =
            participant with
            {
                Combatant =
                    combatantDeathSavingThrow.State
            };

        EncounterState resolvedState = state with
        {
            Revision = resolvedRevision,
            Participants =
                Array.AsReadOnly(participants),
            PendingDeathSavingThrowCombatantId =
                null
        };

        EncounterRules.ValidateState(resolvedState);

        return new EncounterDeathSavingThrowResult
        {
            ActorCombatantId =
                command.ActorCombatantId,
            PreviousLifecycleState =
                previousLifecycleState,
            LifecycleState =
                combatantDeathSavingThrow
                    .State.LifecycleState,
            CombatantDeathSavingThrow =
                combatantDeathSavingThrow,
            State = resolvedState
        };
    }

    private static void ValidateCommand(
        EncounterDeathSavingThrowCommand command)
    {
        if (command.ExpectedRevision < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                command.ExpectedRevision,
                "Expected encounter revision must be at least 1.");
        }

        if (string.IsNullOrWhiteSpace(
            command.ActorCombatantId))
        {
            throw new ArgumentException(
                "Actor combatant ID is required.",
                nameof(command));
        }

        if (!Enum.IsDefined(command.RollMode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                command.RollMode,
                "Unsupported death-saving-throw roll mode.");
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

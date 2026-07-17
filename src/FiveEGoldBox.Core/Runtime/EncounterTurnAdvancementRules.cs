using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public static class EncounterTurnAdvancementRules
{
    public static EncounterTurnAdvancementResult Resolve(
        EncounterState state,
        EncounterTurnAdvancementCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        EncounterRules.ValidateState(state);
        ValidateCommand(command);

        if (state.LifecycleState
            != EncounterLifecycleState.Active)
        {
            throw new InvalidOperationException(
                "A completed encounter cannot advance turns.");
        }

        if (command.ExpectedRevision != state.Revision)
        {
            throw new InvalidOperationException(
                $"Expected encounter revision '{command.ExpectedRevision}', but the current revision is '{state.Revision}'.");
        }

        int currentParticipantIndex =
            FindParticipantIndex(
                state,
                command.ActorCombatantId);

        if (currentParticipantIndex < 0)
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
                "Only the active combatant can end the current turn.");
        }

        long resolvedRevision =
            checked(state.Revision + 1);

        CombatTurnState resolvedTurnState =
            state.TurnState;

        List<string> skippedCombatantIds = [];

        EncounterParticipantState?
            nextParticipant = null;

        int nextParticipantIndex = -1;

        for (int offset = 1;
            offset < state.Participants.Count;
            offset++)
        {
            resolvedTurnState =
                AdvanceTurnChecked(
                    resolvedTurnState);

            string candidateCombatantId =
                CombatTurnRules.GetActiveCombatant(
                    resolvedTurnState)
                .CombatantId;

            int candidateParticipantIndex =
                FindParticipantIndex(
                    state,
                    candidateCombatantId);

            if (candidateParticipantIndex < 0)
            {
                throw new ArgumentException(
                    $"Initiative combatant '{candidateCombatantId}' is not an encounter participant.",
                    nameof(state));
            }

            EncounterParticipantState candidate =
                state.Participants[
                    candidateParticipantIndex];

            if (candidate.Combatant.IsTerminal)
            {
                skippedCombatantIds.Add(
                    candidateCombatantId);

                continue;
            }

            nextParticipant = candidate;
            nextParticipantIndex =
                candidateParticipantIndex;

            break;
        }

        if (nextParticipant is null)
        {
            throw new InvalidOperationException(
                "No other nonterminal combatant is available to take the next turn. Resolve the encounter outcome before advancing.");
        }

        CombatTurnResources refreshedResources =
            CombatTurnResourceRules.StartTurn(
                nextParticipant.TurnResources
                    .MovementSpeedFeet);

        EncounterParticipantState[] participants =
            state.Participants.ToArray();

        participants[nextParticipantIndex] =
            nextParticipant with
            {
                TurnResources = refreshedResources
            };

        IReadOnlyList<string>
            protectedSkippedCombatantIds =
                Array.AsReadOnly(
                    skippedCombatantIds.ToArray());

        EncounterState resolvedState = state with
        {
            Revision = resolvedRevision,
            TurnState = resolvedTurnState,
            Participants =
                Array.AsReadOnly(participants)
        };

        EncounterRules.ValidateState(resolvedState);

        return new EncounterTurnAdvancementResult
        {
            EndedTurnCombatantId =
                command.ActorCombatantId,
            ActiveCombatantId =
                nextParticipant.Combatant
                    .CombatantId,
            PreviousRoundNumber =
                state.TurnState.RoundNumber,
            RoundNumber =
                resolvedTurnState.RoundNumber,
            PreviousActivePosition =
                state.TurnState.ActivePosition,
            ActivePosition =
                resolvedTurnState.ActivePosition,
            SkippedCombatantIds =
                protectedSkippedCombatantIds,
            State = resolvedState
        };
    }

    private static void ValidateCommand(
        EncounterTurnAdvancementCommand command)
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
    }

    private static CombatTurnState AdvanceTurnChecked(
        CombatTurnState state)
    {
        if (state.ActivePosition
                == state.InitiativeOrder.Count
            && state.RoundNumber == int.MaxValue)
        {
            throw new OverflowException(
                "Encounter round number cannot be incremented.");
        }

        return CombatTurnRules.AdvanceTurn(state);
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

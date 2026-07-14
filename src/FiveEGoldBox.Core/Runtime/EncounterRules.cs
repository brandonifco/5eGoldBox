using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public static class EncounterRules
{
    public static EncounterState Start(
        string encounterId,
        IReadOnlyList<EncounterParticipantSetup> participants,
        IReadOnlyList<InitiativeOrderEntry> initiativeOrder)
    {
        if (string.IsNullOrWhiteSpace(encounterId))
        {
            throw new ArgumentException(
                "Encounter ID is required.",
                nameof(encounterId));
        }

        ArgumentNullException.ThrowIfNull(participants);
        ArgumentNullException.ThrowIfNull(initiativeOrder);

        EncounterParticipantState[] participantStates =
            participants
                .Select(CreateParticipantState)
                .ToArray();

        ValidateParticipants(
            participantStates,
            allowTerminalCombatants: false);
        ValidateInitiativeOrder(
            participantStates,
            initiativeOrder);

        IReadOnlyList<EncounterParticipantState>
            protectedParticipants =
                Array.AsReadOnly(participantStates);

        IReadOnlyList<InitiativeOrderEntry>
            canonicalInitiativeOrder =
                Array.AsReadOnly(
                    initiativeOrder
                        .OrderBy(entry => entry.Position)
                        .ToArray());

        CombatTurnState turnState =
            CombatTurnRules.StartCombat(
                canonicalInitiativeOrder);

        return new EncounterState
        {
            EncounterId = encounterId,
            Revision = 1,
            Participants = protectedParticipants,
            TurnState = turnState,
            LifecycleState =
                EncounterLifecycleState.Active
        };
    }

    public static EncounterState DeclareOutcome(
        EncounterState state,
        EncounterLifecycleState outcome)
    {
        ArgumentNullException.ThrowIfNull(state);

        ValidateState(state);

        if (state.LifecycleState
            != EncounterLifecycleState.Active)
        {
            throw new InvalidOperationException(
                "A completed encounter cannot declare another outcome.");
        }

        if (outcome is not EncounterLifecycleState.Victory
            and not EncounterLifecycleState.Defeat)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "Encounter outcome must be victory or defeat.");
        }

        return state with
        {
            Revision = checked(state.Revision + 1),
            LifecycleState = outcome
        };
    }

    internal static void ValidateState(
        EncounterState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (string.IsNullOrWhiteSpace(state.EncounterId))
        {
            throw new ArgumentException(
                "Encounter ID is required.",
                nameof(state));
        }

        if (state.Revision < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state.Revision,
                "Encounter revision must be at least 1.");
        }

        if (!Enum.IsDefined(state.LifecycleState))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state.LifecycleState,
                "Unsupported encounter lifecycle state.");
        }

        ArgumentNullException.ThrowIfNull(
            state.Participants);
        ArgumentNullException.ThrowIfNull(
            state.TurnState);

        ValidateParticipants(
            state.Participants,
            allowTerminalCombatants: true);
        ValidateInitiativeOrder(
            state.Participants,
            state.TurnState.InitiativeOrder);
        for (int index = 0;
            index < state.TurnState.InitiativeOrder.Count;
            index++)
        {
            if (state.TurnState.InitiativeOrder[index].Position
                != index + 1)
            {
                throw new ArgumentException(
                    "Stored initiative order must be ordered by position.",
                    nameof(state));
            }
        }
        string activeCombatantId =
            CombatTurnRules.GetActiveCombatant(
                state.TurnState)
            .CombatantId;

        if (!state.Participants.Any(
            participant =>
                participant.Combatant.CombatantId
                == activeCombatantId))
        {
            throw new ArgumentException(
                "The active combatant must be an encounter participant.",
                nameof(state));
        }
    }

    private static EncounterParticipantState
        CreateParticipantState(
            EncounterParticipantSetup participant)
    {
        ArgumentNullException.ThrowIfNull(participant);
        ArgumentNullException.ThrowIfNull(
            participant.Combatant);

        return new EncounterParticipantState
        {
            Combatant = participant.Combatant,
            SideId = participant.SideId,
            TurnResources =
                CombatTurnResourceRules.StartTurn(
                    participant.MovementSpeedFeet)
        };
    }

    private static void ValidateParticipants(
        IReadOnlyList<EncounterParticipantState> participants, bool allowTerminalCombatants)
    {
        if (participants.Count == 0)
        {
            throw new ArgumentException(
                "An encounter must contain participants.",
                nameof(participants));
        }

        HashSet<string> combatantIds =
            new(StringComparer.Ordinal);

        HashSet<string> sideIds =
            new(StringComparer.Ordinal);

        foreach (EncounterParticipantState participant
            in participants)
        {
            ArgumentNullException.ThrowIfNull(participant);
            ArgumentNullException.ThrowIfNull(
                participant.Combatant);
            ArgumentNullException.ThrowIfNull(
                participant.TurnResources);

            CombatTurnResourceRules.ValidateResources(
                participant.TurnResources);
            CombatantRules.ValidateState(
                participant.Combatant);

            if (!allowTerminalCombatants && participant.Combatant.IsTerminal)
            {
                throw new ArgumentException(
                    $"Terminal combatant '{participant.Combatant.CombatantId}' cannot enter a new encounter.",
                    nameof(participants));
            }

            if (!combatantIds.Add(
                participant.Combatant.CombatantId))
            {
                throw new ArgumentException(
                    $"Duplicate combatant ID '{participant.Combatant.CombatantId}' is not allowed.",
                    nameof(participants));
            }

            if (string.IsNullOrWhiteSpace(
                participant.SideId))
            {
                throw new ArgumentException(
                    "Participant side ID is required.",
                    nameof(participants));
            }

            sideIds.Add(participant.SideId);
        }

        if (sideIds.Count < 2)
        {
            throw new ArgumentException(
                "An encounter must contain at least two sides.",
                nameof(participants));
        }
    }

    private static void ValidateInitiativeOrder(
        IReadOnlyList<EncounterParticipantState> participants,
        IReadOnlyList<InitiativeOrderEntry> initiativeOrder)
    {
        if (initiativeOrder.Count != participants.Count)
        {
            throw new ArgumentException(
                "Initiative order must contain exactly one entry for each encounter participant.",
                nameof(initiativeOrder));
        }

        HashSet<string> participantIds =
            participants
                .Select(participant =>
                    participant.Combatant.CombatantId)
                .ToHashSet(StringComparer.Ordinal);

        HashSet<string> initiativeIds =
            new(StringComparer.Ordinal);

        HashSet<int> positions = new();

        foreach (InitiativeOrderEntry entry
            in initiativeOrder)
        {
            ArgumentNullException.ThrowIfNull(entry);

            if (string.IsNullOrWhiteSpace(
                entry.CombatantId))
            {
                throw new ArgumentException(
                    "Initiative combatant ID is required.",
                    nameof(initiativeOrder));
            }

            ArgumentNullException.ThrowIfNull(
                entry.Initiative);

            if (!initiativeIds.Add(
                entry.CombatantId))
            {
                throw new ArgumentException(
                    $"Duplicate initiative combatant ID '{entry.CombatantId}' is not allowed.",
                    nameof(initiativeOrder));
            }

            if (!participantIds.Contains(
                entry.CombatantId))
            {
                throw new ArgumentException(
                    $"Initiative combatant '{entry.CombatantId}' is not an encounter participant.",
                    nameof(initiativeOrder));
            }

            if (!positions.Add(entry.Position))
            {
                throw new ArgumentException(
                    $"Duplicate initiative position '{entry.Position}' is not allowed.",
                    nameof(initiativeOrder));
            }
        }

        for (int expectedPosition = 1;
            expectedPosition <= initiativeOrder.Count;
            expectedPosition++)
        {
            if (!positions.Contains(expectedPosition))
            {
                throw new ArgumentException(
                    "Initiative positions must be contiguous and start at 1.",
                    nameof(initiativeOrder));
            }
        }

        if (!participantIds.SetEquals(
            initiativeIds))
        {
            throw new ArgumentException(
                "Initiative order must match the encounter participant set.",
                nameof(initiativeOrder));
        }
    }
}

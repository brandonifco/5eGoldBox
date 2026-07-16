using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public static class EncounterRules
{
    public static EncounterState Start(
        string encounterId,
        EncounterBattlefieldState battlefield,
        IReadOnlyList<EncounterParticipantSetup> participants,
        IReadOnlyList<InitiativeOrderEntry> initiativeOrder)
    {
        if (string.IsNullOrWhiteSpace(encounterId))
        {
            throw new ArgumentException(
                "Encounter ID is required.",
                nameof(encounterId));
        }

        ArgumentNullException.ThrowIfNull(battlefield);
        ArgumentNullException.ThrowIfNull(participants);
        ArgumentNullException.ThrowIfNull(initiativeOrder);

        EncounterBattlefieldState protectedBattlefield =
            ProtectBattlefield(battlefield);

        EncounterParticipantState[] participantStates =
            participants
                .Select(CreateParticipantState)
                .ToArray();

        ValidateParticipants(
            participantStates,
            protectedBattlefield,
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
            Battlefield = protectedBattlefield,
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
            state.Battlefield);
        ArgumentNullException.ThrowIfNull(
            state.Participants);
        ArgumentNullException.ThrowIfNull(
            state.TurnState);

        ValidateBattlefield(state.Battlefield);
        ValidateParticipants(
            state.Participants,
            state.Battlefield,
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

    private static EncounterBattlefieldState
        ProtectBattlefield(
            EncounterBattlefieldState battlefield)
    {
        ValidateBattlefield(battlefield);

        return battlefield with
        {
            BlockedPositions =
                Array.AsReadOnly(
                    battlefield.BlockedPositions.ToArray()),
            DifficultTerrainPositions =
                Array.AsReadOnly(
                    battlefield
                        .DifficultTerrainPositions
                        .ToArray())
        };
    }
    private static EncounterCombatProfile
        ProtectCombatProfile(
            EncounterCombatProfile combatProfile)
    {
        ArgumentNullException.ThrowIfNull(combatProfile);

        if (combatProfile.ArmorClass <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(combatProfile),
                combatProfile.ArmorClass,
                "Armor class must be greater than 0.");
        }

        ArgumentNullException.ThrowIfNull(
            combatProfile.WeaponAttacks);
        ArgumentNullException.ThrowIfNull(
            combatProfile.DamageResponses);

        return combatProfile with
        {
            WeaponAttacks =
                Array.AsReadOnly(
                    combatProfile.WeaponAttacks.ToArray()),
            DamageResponses =
                Array.AsReadOnly(
                    combatProfile.DamageResponses.ToArray())
        };
    }
    private static void ValidateCombatProfile(
        EncounterCombatProfile combatProfile)
    {
        ArgumentNullException.ThrowIfNull(combatProfile);

        if (combatProfile.ArmorClass <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(combatProfile),
                combatProfile.ArmorClass,
                "Armor class must be greater than 0.");
        }

        ArgumentNullException.ThrowIfNull(
            combatProfile.WeaponAttacks);
        ArgumentNullException.ThrowIfNull(
            combatProfile.DamageResponses);
    }

    private static EncounterParticipantState
        CreateParticipantState(
            EncounterParticipantSetup participant)
    {
        ArgumentNullException.ThrowIfNull(participant);
        ArgumentNullException.ThrowIfNull(
            participant.Combatant);
        ArgumentNullException.ThrowIfNull(
            participant.CombatProfile);

        return new EncounterParticipantState
        {
            Combatant = participant.Combatant,
            CombatProfile =
                ProtectCombatProfile(
                    participant.CombatProfile),
            SideId = participant.SideId,
            TurnResources =
                CombatTurnResourceRules.StartTurn(
                    participant.MovementSpeedFeet),
            Position = participant.StartingPosition
        };
    }

    private static void ValidateBattlefield(
        EncounterBattlefieldState battlefield)
    {
        if (string.IsNullOrWhiteSpace(
            battlefield.BattlefieldId))
        {
            throw new ArgumentException(
                "Battlefield ID is required.",
                nameof(battlefield));
        }

        if (battlefield.Width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(battlefield),
                battlefield.Width,
                "Battlefield width must be greater than 0.");
        }

        if (battlefield.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(battlefield),
                battlefield.Height,
                "Battlefield height must be greater than 0.");
        }

        ArgumentNullException.ThrowIfNull(
            battlefield.BlockedPositions);
        ArgumentNullException.ThrowIfNull(
            battlefield.DifficultTerrainPositions);

        HashSet<GridPosition> blockedPositions = new();

        foreach (GridPosition position
            in battlefield.BlockedPositions)
        {
            ValidatePositionWithinBattlefield(
                battlefield,
                position,
                nameof(battlefield));

            if (!blockedPositions.Add(position))
            {
                throw new ArgumentException(
                    $"Duplicate blocked position '{position}' is not allowed.",
                    nameof(battlefield));
            }
        }

        HashSet<GridPosition> difficultTerrainPositions =
            new();

        foreach (GridPosition position
            in battlefield.DifficultTerrainPositions)
        {
            ValidatePositionWithinBattlefield(
                battlefield,
                position,
                nameof(battlefield));

            if (!difficultTerrainPositions.Add(position))
            {
                throw new ArgumentException(
                    $"Duplicate difficult-terrain position '{position}' is not allowed.",
                    nameof(battlefield));
            }

            if (blockedPositions.Contains(position))
            {
                throw new ArgumentException(
                    $"Position '{position}' cannot be both blocked and difficult terrain.",
                    nameof(battlefield));
            }
        }
    }

    private static void ValidateParticipants(
        IReadOnlyList<EncounterParticipantState> participants,
        EncounterBattlefieldState battlefield,
        bool allowTerminalCombatants)
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

        HashSet<GridPosition> occupiedPositions = new();

        HashSet<GridPosition> blockedPositions =
            battlefield.BlockedPositions.ToHashSet();

        foreach (EncounterParticipantState participant
            in participants)
        {
            ArgumentNullException.ThrowIfNull(
                participant.Combatant);
            ArgumentNullException.ThrowIfNull(
                participant.CombatProfile);
            ArgumentNullException.ThrowIfNull(
                participant.TurnResources);

            CombatTurnResourceRules.ValidateResources(
                participant.TurnResources);
            CombatantRules.ValidateState(
                participant.Combatant);
            ValidateCombatProfile(
                participant.CombatProfile);

            if (!allowTerminalCombatants
                && participant.Combatant.IsTerminal)
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

            ValidatePositionWithinBattlefield(
                battlefield,
                participant.Position,
                nameof(participants));

            if (blockedPositions.Contains(
                participant.Position))
            {
                throw new ArgumentException(
                    $"Combatant '{participant.Combatant.CombatantId}' cannot occupy blocked position '{participant.Position}'.",
                    nameof(participants));
            }

            if (!occupiedPositions.Add(
                participant.Position))
            {
                throw new ArgumentException(
                    $"Multiple combatants cannot occupy position '{participant.Position}'.",
                    nameof(participants));
            }
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

    private static void ValidatePositionWithinBattlefield(
        EncounterBattlefieldState battlefield,
        GridPosition position,
        string parameterName)
    {
        if (position.X < 0
            || position.X >= battlefield.Width
            || position.Y < 0
            || position.Y >= battlefield.Height)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                position,
                $"Position '{position}' must be within the battlefield.");
        }
    }
}

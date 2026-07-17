using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public static class EncounterMovementRules
{
    private const int NormalTerrainMovementFeet = 5;
    private const int DifficultTerrainMovementFeet = 10;

    public static EncounterMovementResult Resolve(
        EncounterState state,
        EncounterMovementCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        EncounterRules.ValidateState(state);
        ValidateCommand(command);

        if (state.LifecycleState
            != EncounterLifecycleState.Active)
        {
            throw new InvalidOperationException(
                "A completed encounter cannot resolve movement.");
        }

        if (command.ExpectedRevision != state.Revision)
        {
            throw new InvalidOperationException(
                $"Expected encounter revision '{command.ExpectedRevision}', but the current revision is '{state.Revision}'.");
        }

        int actorIndex = FindParticipantIndex(
            state,
            command.ActorCombatantId);

        if (actorIndex < 0)
        {
            throw new ArgumentException(
                $"Actor '{command.ActorCombatantId}' is not an encounter participant.",
                nameof(command));
        }

        EncounterParticipantState actor =
            state.Participants[actorIndex];

        ValidateActor(state, actor);

        GridPosition[] path =
            command.Path.ToArray();

        HashSet<GridPosition> blockedPositions =
            state.Battlefield.BlockedPositions
                .ToHashSet();

        HashSet<GridPosition> difficultTerrainPositions =
            state.Battlefield
                .DifficultTerrainPositions
                .ToHashSet();

        HashSet<GridPosition> occupiedPositions =
            state.Participants
                .Where(
                    (_, index) =>
                        index != actorIndex)
                .Select(
                    participant =>
                        participant.Position)
                .ToHashSet();

        GridPosition startingPosition =
            actor.Position;

        GridPosition previousPosition =
            startingPosition;

        int movementSpentFeet = 0;

        foreach (GridPosition position in path)
        {
            ValidatePositionWithinBattlefield(
                state.Battlefield,
                position);

            ValidateAdjacentStep(
                previousPosition,
                position);

            if (blockedPositions.Contains(position))
            {
                throw new InvalidOperationException(
                    $"Movement path enters blocked position '{position}'.");
            }

            if (occupiedPositions.Contains(position))
            {
                throw new InvalidOperationException(
                    $"Movement path enters occupied position '{position}'.");
            }

            int stepCost =
                difficultTerrainPositions.Contains(position)
                    ? DifficultTerrainMovementFeet
                    : NormalTerrainMovementFeet;

            movementSpentFeet = checked(
                movementSpentFeet + stepCost);

            previousPosition = position;
        }

        CombatTurnResources resolvedResources =
            CombatTurnResourceRules.SpendMovement(
                actor.TurnResources,
                movementSpentFeet);

        long resolvedRevision =
            checked(state.Revision + 1);

        EncounterParticipantState[] participants =
            state.Participants.ToArray();

        participants[actorIndex] = actor with
        {
            Position = previousPosition,
            TurnResources = resolvedResources
        };

        IReadOnlyList<GridPosition> protectedPath =
            Array.AsReadOnly(path);

        EncounterState resolvedState = state with
        {
            Revision = resolvedRevision,
            Participants =
                Array.AsReadOnly(participants)
        };

        EncounterRules.ValidateState(resolvedState);

        return new EncounterMovementResult
        {
            ActorCombatantId =
                command.ActorCombatantId,
            StartingPosition = startingPosition,
            EndingPosition = previousPosition,
            Path = protectedPath,
            MovementSpentFeet = movementSpentFeet,
            State = resolvedState
        };
    }

    private static void ValidateCommand(
        EncounterMovementCommand command)
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

        ArgumentNullException.ThrowIfNull(
            command.Path);

        if (command.Path.Count == 0)
        {
            throw new ArgumentException(
                "Movement path must contain at least one position.",
                nameof(command));
        }
    }

    private static void ValidateActor(
        EncounterState state,
        EncounterParticipantState actor)
    {
        if (actor.Combatant.CombatantId
            != state.ActiveCombatantId)
        {
            throw new InvalidOperationException(
                "Only the active combatant can move.");
        }

        if (actor.Combatant.LifecycleState
            != CombatantLifecycleState.Conscious)
        {
            throw new InvalidOperationException(
                "The moving combatant must be conscious.");
        }

        if (actor.TurnResources
            .MovementRemainingFeet <= 0)
        {
            throw new InvalidOperationException(
                "The moving combatant has no movement remaining.");
        }
    }

    private static void ValidatePositionWithinBattlefield(
        EncounterBattlefieldState battlefield,
        GridPosition position)
    {
        if (position.X < 0
            || position.X >= battlefield.Width
            || position.Y < 0
            || position.Y >= battlefield.Height)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                position,
                $"Position '{position}' must be within the battlefield.");
        }
    }

    private static void ValidateAdjacentStep(
        GridPosition previousPosition,
        GridPosition nextPosition)
    {
        int horizontalDistance =
            Math.Abs(
                nextPosition.X
                - previousPosition.X);

        int verticalDistance =
            Math.Abs(
                nextPosition.Y
                - previousPosition.Y);

        bool isAdjacent =
            horizontalDistance <= 1
            && verticalDistance <= 1
            && (horizontalDistance != 0
                || verticalDistance != 0);

        if (!isAdjacent)
        {
            throw new ArgumentException(
                $"Movement step from '{previousPosition}' to '{nextPosition}' must enter an adjacent square.",
                nameof(nextPosition));
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

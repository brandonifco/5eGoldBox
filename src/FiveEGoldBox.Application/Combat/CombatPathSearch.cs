using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal static class CombatPathSearch
{
    private const int FeetPerStep = 5;

    private static readonly GridPosition[] NeighborOffsets =
    [
        new(-1, -1),
        new(0, -1),
        new(1, -1),
        new(-1, 0),
        new(1, 0),
        new(-1, 1),
        new(0, 1),
        new(1, 1)
    ];

    internal static IReadOnlyList<EncounterMovementResult>
        EnumerateReachableMovements(
            EncounterState state,
            string actorCombatantId)
    {
        ArgumentNullException.ThrowIfNull(state);

        EncounterParticipantState actor =
            FindParticipant(state, actorCombatantId);

        int maximumSteps =
            actor.TurnResources.MovementRemainingFeet
            / FeetPerStep;

        if (maximumSteps <= 0)
        {
            return Array.Empty<EncounterMovementResult>();
        }

        List<EncounterMovementResult> results = [];

        foreach (IReadOnlyList<GridPosition> path
            in EnumerateShortestPaths(
                state,
                actorCombatantId,
                actor.Position,
                maximumSteps))
        {
            results.Add(
                EncounterMovementRules.Resolve(
                    state,
                    new EncounterMovementCommand
                    {
                        ExpectedRevision = state.Revision,
                        ActorCombatantId = actorCombatantId,
                        Path = path
                    }));
        }

        results.Sort(CompareMovementResults);

        return Array.AsReadOnly(results.ToArray());
    }

    private static EncounterParticipantState FindParticipant(
        EncounterState state,
        string combatantId)
    {
        return state.Participants.Single(participant =>
            string.Equals(
                participant.Combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal));
    }

    private static IReadOnlyList<IReadOnlyList<GridPosition>>
        EnumerateShortestPaths(
            EncounterState state,
            string actorCombatantId,
            GridPosition start,
            int maximumSteps)
    {
        EncounterBattlefieldState battlefield = state.Battlefield;
        HashSet<GridPosition> unavailable =
            battlefield.BlockedPositions.ToHashSet();

        foreach (EncounterParticipantState participant
            in state.Participants)
        {
            if (!string.Equals(
                participant.Combatant.CombatantId,
                actorCombatantId,
                StringComparison.Ordinal))
            {
                unavailable.Add(participant.Position);
            }
        }

        Queue<(GridPosition Position, GridPosition[] Path)> queue = [];
        HashSet<GridPosition> visited = [start];
        List<IReadOnlyList<GridPosition>> paths = [];

        queue.Enqueue((start, Array.Empty<GridPosition>()));

        while (queue.Count > 0)
        {
            (GridPosition position, GridPosition[] path) = queue.Dequeue();

            if (path.Length >= maximumSteps)
            {
                continue;
            }

            foreach (GridPosition next
                in EnumerateNeighbors(position))
            {
                if (!IsInside(battlefield, next)
                    || unavailable.Contains(next)
                    || !visited.Add(next))
                {
                    continue;
                }

                GridPosition[] nextPath =
                    [.. path, next];

                paths.Add(Array.AsReadOnly(nextPath));
                queue.Enqueue((next, nextPath));
            }
        }

        return Array.AsReadOnly(paths.ToArray());
    }

    private static IEnumerable<GridPosition> EnumerateNeighbors(
        GridPosition position)
    {
        return NeighborOffsets
            .Select(offset => new GridPosition(
                position.X + offset.X,
                position.Y + offset.Y))
            .OrderBy(candidate => candidate.Y)
            .ThenBy(candidate => candidate.X);
    }

    private static bool IsInside(
        EncounterBattlefieldState battlefield,
        GridPosition position)
    {
        return position.X >= 0
            && position.X < battlefield.Width
            && position.Y >= 0
            && position.Y < battlefield.Height;
    }

    private static int CompareMovementResults(
        EncounterMovementResult left,
        EncounterMovementResult right)
    {
        int comparison = left.EndingPosition.Y.CompareTo(
            right.EndingPosition.Y);

        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.EndingPosition.X.CompareTo(
            right.EndingPosition.X);

        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.MovementSpentFeet.CompareTo(
            right.MovementSpentFeet);

        if (comparison != 0)
        {
            return comparison;
        }

        return ComparePaths(left.Path, right.Path);
    }

    private static int ComparePaths(
        IReadOnlyList<GridPosition> left,
        IReadOnlyList<GridPosition> right)
    {
        int sharedCount = Math.Min(left.Count, right.Count);

        for (int index = 0; index < sharedCount; index++)
        {
            int comparison = left[index].Y.CompareTo(right[index].Y);

            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left[index].X.CompareTo(right[index].X);

            if (comparison != 0)
            {
                return comparison;
            }
        }

        return left.Count.CompareTo(right.Count);
    }
}

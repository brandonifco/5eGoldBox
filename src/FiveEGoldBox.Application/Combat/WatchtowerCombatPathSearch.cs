using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal static class WatchtowerCombatPathSearch
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

    internal static EncounterMovementResult? FindMovement(
        EncounterState state,
        string actorCombatantId,
        string targetCombatantId,
        string weaponId)
    {
        ArgumentNullException.ThrowIfNull(state);

        EncounterParticipantState actor =
            WatchtowerCombatDecisionFactory.FindParticipant(
                state,
                actorCombatantId);
        EncounterParticipantState target =
            WatchtowerCombatDecisionFactory.FindParticipant(
                state,
                targetCombatantId);
        WeaponAttack weapon =
            WatchtowerCombatDecisionFactory.GetFixedWeapon(actor);

        if (!string.Equals(
            weapon.WeaponId,
            weaponId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Weapon '{weaponId}' is not the authored weapon for actor '{actorCombatantId}'.",
                nameof(weaponId));
        }

        int maximumSteps =
            actor.TurnResources.MovementRemainingFeet
            / FeetPerStep;

        if (maximumSteps <= 0)
        {
            return null;
        }

        List<EncounterMovementResult> attackEnabled = [];
        List<EncounterMovementResult> progress = [];
        int startingTargetDistance = DistanceFeet(
            actor.Position,
            target.Position);

        foreach (IReadOnlyList<GridPosition> path
            in EnumerateShortestPaths(
                state,
                actorCombatantId,
                actor.Position,
                maximumSteps))
        {
            EncounterMovementResult movement =
                EncounterMovementRules.Resolve(
                    state,
                    new EncounterMovementCommand
                    {
                        ExpectedRevision = state.Revision,
                        ActorCombatantId = actorCombatantId,
                        Path = path
                    });

            EncounterWeaponAttackPrerequisiteEvaluation prerequisites =
                EncounterWeaponAttackPrerequisiteRules.Evaluate(
                    movement.State,
                    actorCombatantId,
                    targetCombatantId,
                    weaponId);

            if (prerequisites.IsLegal)
            {
                attackEnabled.Add(movement);
            }
            else if (DistanceFeet(
                movement.EndingPosition,
                target.Position) < startingTargetDistance)
            {
                progress.Add(movement);
            }
        }

        IEnumerable<EncounterMovementResult> candidates =
            attackEnabled.Count > 0
                ? attackEnabled
                    .OrderBy(result => result.MovementSpentFeet)
                    .ThenBy(result => DistanceFeet(
                        result.EndingPosition,
                        target.Position))
                    .ThenBy(result => result.EndingPosition.Y)
                    .ThenBy(result => result.EndingPosition.X)
                : progress
                    .OrderBy(result => DistanceFeet(
                        result.EndingPosition,
                        target.Position))
                    .ThenBy(result => result.EndingPosition.Y)
                    .ThenBy(result => result.EndingPosition.X);

        return candidates.FirstOrDefault();
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

    private static int DistanceFeet(
        GridPosition first,
        GridPosition second)
    {
        return checked(
            Math.Max(
                Math.Abs(first.X - second.X),
                Math.Abs(first.Y - second.Y))
            * FeetPerStep);
    }
}

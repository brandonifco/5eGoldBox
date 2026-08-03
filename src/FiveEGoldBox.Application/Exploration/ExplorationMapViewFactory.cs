using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Exploration;

/// Flattens an ExplorationMapDefinition (internal content) plus a live
/// ExplorationState into the public, client-safe ExplorationMapView —
/// the same role CombatViewFactory plays for CombatView.
internal static class ExplorationMapViewFactory
{
    internal static ExplorationMapView Create(
        ExplorationMapDefinition map,
        ExplorationState state)
    {
        ExplorationFloorDefinition floor = map.Floors
            .First(candidate => string.Equals(
                candidate.Floor,
                state.Floor,
                StringComparison.Ordinal));

        IReadOnlyList<DoorDefinition> visibleClosedDoors = floor.Doors
            .Where(door => IsRevealed(door, state) && !IsOpen(door, state))
            .ToArray();
        IReadOnlyList<GridPosition> treasurePositions = floor.Treasures
            .Where(treasure => !IsCollected(treasure, state))
            .Select(treasure => treasure.Position)
            .ToArray();

        return new ExplorationMapView(
            map.MapId,
            state.Floor,
            map.Width,
            map.Height,
            floor.TraversablePositions,
            floor.Stairs
                .Select(stair => stair.Position)
                .ToArray(),
            visibleClosedDoors
                .Select(door => new ExplorationDoorEdge(
                    door.Position,
                    door.OtherPosition,
                    door.IsLocked))
                .ToArray(),
            treasurePositions,
            state.Position,
            state.Facing);
    }

    /// Bucket 1 of the door-visibility rule -- an unrevealed secret door
    /// is fully invisible, so nothing downstream should ever see it. Every
    /// other door (secret-but-revealed, or never secret at all) is known.
    private static bool IsRevealed(
        DoorDefinition door,
        ExplorationState state)
    {
        return !door.IsSecret
            || state.RevealedSecretDoorIds.Contains(
                door.DoorId,
                StringComparer.Ordinal);
    }

    /// Bucket 2 -- a door the party has opened behaves exactly like
    /// ordinary floor on both sides, so it drops out of the "closed door"
    /// list entirely rather than needing special-cased traversability.
    private static bool IsOpen(
        DoorDefinition door,
        ExplorationState state)
    {
        return state.OpenDoorIds.Contains(
            door.DoorId,
            StringComparer.Ordinal);
    }

    private static bool IsCollected(
        TreasureDefinition treasure,
        ExplorationState state)
    {
        return state.CollectedTreasureIds.Contains(
            treasure.TreasureId,
            StringComparer.Ordinal);
    }
}

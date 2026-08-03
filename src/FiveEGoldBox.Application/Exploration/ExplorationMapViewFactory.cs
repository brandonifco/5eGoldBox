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
            floor.Doors
                .Select(door => new ExplorationDoorEdge(
                    door.Position,
                    door.OtherPosition,
                    door.IsLocked,
                    IsOpen(door),
                    IsRevealed(door, state)))
                .ToArray(),
            treasurePositions,
            state.Position,
            state.Facing);
    }

    /// Whether the party knows about this door at all -- false only for
    /// an unrevealed secret. Flagged on the returned ExplorationDoorEdge
    /// rather than used to filter it out, since a renderer still needs to
    /// know an unrevealed door's edge exists in order to draw it as a
    /// plain wall instead of open passage.
    private static bool IsRevealed(
        DoorDefinition door,
        ExplorationState state)
    {
        return !door.IsSecret
            || state.RevealedSecretDoorIds.Contains(
                door.DoorId,
                StringComparer.Ordinal);
    }

    /// An unlocked, revealed door is just an opening -- there's no
    /// separate "closed but unlocked" state to track once opening a door
    /// no longer needs its own action. Only a locked door renders as
    /// still-closed, until some future unlock mechanic (bash/pick/knock)
    /// resolves it.
    private static bool IsOpen(
        DoorDefinition door)
    {
        return !door.IsLocked;
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

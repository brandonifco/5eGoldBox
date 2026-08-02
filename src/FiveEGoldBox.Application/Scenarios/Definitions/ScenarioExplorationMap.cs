using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// Reads an authored map: what can be walked on, where the stairs go, and
/// whether a given exploration state is somewhere the map allows.
///
/// Replaces the per-scenario map class. The geometry is content, so it lives in
/// the definition and this only interprets it.
internal static class ScenarioExplorationMap
{
    /// The map for wherever the party currently is, or null if that location
    /// is a hub rather than somewhere to explore.
    internal static ExplorationMapDefinition? FindCurrent(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return ScenarioDefinitionRegistry
            .Resolve(session)
            .Locations
            .FirstOrDefault(location => string.Equals(
                location.LocationId,
                session.CurrentLocationId,
                StringComparison.Ordinal))
            ?.ExplorationMap;
    }

    internal static ExplorationState CreateStartingState(
        ExplorationMapDefinition map)
    {
        ArgumentNullException.ThrowIfNull(map);

        return new ExplorationState
        {
            MapId = map.MapId,
            Floor = map.StartingFloor,
            Position = map.StartingPosition,
            Facing = map.StartingFacing
        };
    }

    internal static void Validate(
        ExplorationMapDefinition map,
        ExplorationState state)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(state);

        if (string.IsNullOrWhiteSpace(state.MapId))
        {
            throw new ArgumentException(
                "Exploration map ID is required.",
                nameof(state));
        }

        if (!string.Equals(
            state.MapId,
            map.MapId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The exploration map is unsupported.",
                nameof(state));
        }

        if (string.IsNullOrWhiteSpace(state.Floor))
        {
            throw new ArgumentException(
                "Exploration floor ID is required.",
                nameof(state));
        }

        if (!Enum.IsDefined(state.Facing))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state.Facing,
                "Unsupported exploration facing.");
        }

        if (!IsTraversable(map, state.Floor, state.Position, state.OpenDoorIds))
        {
            throw new ArgumentException(
                "The exploration position is not a traversable tile on the current floor.",
                nameof(state));
        }

        RequireKnownDoorIds(
            map,
            state.OpenDoorIds,
            nameof(state.OpenDoorIds));
        RequireKnownDoorIds(
            map,
            state.RevealedSecretDoorIds,
            nameof(state.RevealedSecretDoorIds));
        RequireKnownTreasureIds(
            map,
            state.CollectedTreasureIds,
            nameof(state.CollectedTreasureIds));
    }

    /// A position is traversable if it is ordinary open ground, or an
    /// unlocked door there has been opened. A locked door is independently
    /// re-checked here rather than trusted from the caller -- the same
    /// defense-in-depth this method already applies to every other
    /// invariant -- so corrupted state naming a locked door's ID can never
    /// make it walkable.
    internal static bool IsTraversable(
        ExplorationMapDefinition map,
        string floor,
        GridPosition position,
        IReadOnlyList<string> openDoorIds)
    {
        ExplorationFloorDefinition? floorDefinition = FindFloor(map, floor);

        if (floorDefinition is null)
        {
            return false;
        }

        if (floorDefinition.TraversablePositions.Contains(position))
        {
            return true;
        }

        DoorDefinition? door = floorDefinition.Doors
            .FirstOrDefault(candidate => candidate.Position == position);

        return door is not null
            && !door.IsLocked
            && openDoorIds.Contains(door.DoorId, StringComparer.Ordinal);
    }

    internal static DoorDefinition? FindDoor(
        ExplorationMapDefinition map,
        string floor,
        GridPosition position)
    {
        return FindFloor(map, floor)
            ?.Doors
            .FirstOrDefault(candidate => candidate.Position == position);
    }

    internal static TreasureDefinition? FindTreasure(
        ExplorationMapDefinition map,
        string floor,
        GridPosition position)
    {
        return FindFloor(map, floor)
            ?.Treasures
            .FirstOrDefault(candidate => candidate.Position == position);
    }

    internal static bool TryGetStairDestination(
        ExplorationMapDefinition map,
        string floor,
        GridPosition position,
        out string destinationFloor,
        out GridPosition destinationPosition)
    {
        StairDefinition? stair = FindFloor(map, floor)
            ?.Stairs
            .FirstOrDefault(candidate => candidate.Position == position);

        if (stair is null)
        {
            destinationFloor = string.Empty;
            destinationPosition = default;
            return false;
        }

        destinationFloor = stair.DestinationFloor;
        destinationPosition = stair.DestinationPosition;
        return true;
    }

    private static ExplorationFloorDefinition? FindFloor(
        ExplorationMapDefinition map,
        string floor)
    {
        return map.Floors.FirstOrDefault(
            candidate => string.Equals(
                candidate.Floor,
                floor,
                StringComparison.Ordinal));
    }

    /// Guards against corrupted/hand-edited save data referencing a door ID
    /// that does not exist anywhere on this map.
    private static void RequireKnownDoorIds(
        ExplorationMapDefinition map,
        IReadOnlyList<string> doorIds,
        string parameterName)
    {
        if (doorIds.Count == 0)
        {
            return;
        }

        HashSet<string> known = new(
            map.Floors.SelectMany(floor => floor.Doors)
                .Select(door => door.DoorId),
            StringComparer.Ordinal);

        if (doorIds.Any(id => !known.Contains(id)))
        {
            throw new ArgumentException(
                "The exploration state names a door ID that does not exist on the current map.",
                parameterName);
        }
    }

    /// Same guard as RequireKnownDoorIds, for collected treasure IDs.
    private static void RequireKnownTreasureIds(
        ExplorationMapDefinition map,
        IReadOnlyList<string> treasureIds,
        string parameterName)
    {
        if (treasureIds.Count == 0)
        {
            return;
        }

        HashSet<string> known = new(
            map.Floors.SelectMany(floor => floor.Treasures)
                .Select(treasure => treasure.TreasureId),
            StringComparer.Ordinal);

        if (treasureIds.Any(id => !known.Contains(id)))
        {
            throw new ArgumentException(
                "The exploration state names a treasure ID that does not exist on the current map.",
                parameterName);
        }
    }
}

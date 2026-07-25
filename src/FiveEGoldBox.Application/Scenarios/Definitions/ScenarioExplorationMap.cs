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

        if (!Enum.IsDefined(state.Floor))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state.Floor,
                "Unsupported exploration floor.");
        }

        if (!Enum.IsDefined(state.Facing))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state.Facing,
                "Unsupported exploration facing.");
        }

        if (!IsTraversable(map, state.Floor, state.Position))
        {
            throw new ArgumentException(
                "The exploration position is not a traversable tile on the current floor.",
                nameof(state));
        }
    }

    internal static bool IsTraversable(
        ExplorationMapDefinition map,
        ExplorationFloor floor,
        GridPosition position)
    {
        return FindFloor(map, floor)
            ?.TraversablePositions.Contains(position)
            ?? false;
    }

    internal static bool TryGetStairDestination(
        ExplorationMapDefinition map,
        ExplorationFloor floor,
        GridPosition position,
        out ExplorationFloor destinationFloor,
        out GridPosition destinationPosition)
    {
        StairDefinition? stair = FindFloor(map, floor)
            ?.Stairs
            .FirstOrDefault(candidate => candidate.Position == position);

        if (stair is null)
        {
            destinationFloor = default;
            destinationPosition = default;
            return false;
        }

        destinationFloor = stair.DestinationFloor;
        destinationPosition = stair.DestinationPosition;
        return true;
    }

    private static ExplorationFloorDefinition? FindFloor(
        ExplorationMapDefinition map,
        ExplorationFloor floor)
    {
        return map.Floors.FirstOrDefault(
            candidate => candidate.Floor == floor);
    }
}

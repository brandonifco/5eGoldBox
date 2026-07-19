using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Exploration;

internal static class WatchtowerExplorationMap
{
    internal const string MapId =
        "map.ruined-watchtower";

    internal const ExplorationFloor StartingFloor =
        ExplorationFloor.GroundFloor;

    internal const ExplorationFacing StartingFacing =
        ExplorationFacing.East;

    internal static readonly GridPosition
        StartingPosition = new(0, 0);

    private static readonly GridPosition
        GroundFloorStairPosition = new(2, 0);

    private static readonly GridPosition
        UpperFloorStairPosition = new(2, 0);

    private static readonly HashSet<GridPosition>
        GroundFloorTraversableTiles =
        [
            new GridPosition(0, 0),
            new GridPosition(1, 0),
            new GridPosition(2, 0),
            new GridPosition(0, 1),
            new GridPosition(2, 1),
            new GridPosition(0, 2),
            new GridPosition(1, 2),
            new GridPosition(2, 2)
        ];

    private static readonly HashSet<GridPosition>
        UpperFloorTraversableTiles =
        [
            new GridPosition(0, 0),
            new GridPosition(1, 0),
            new GridPosition(2, 0),
            new GridPosition(0, 1),
            new GridPosition(1, 1),
            new GridPosition(2, 1)
        ];

    internal static ExplorationState CreateStartingState()
    {
        return new ExplorationState
        {
            MapId = MapId,
            Floor = StartingFloor,
            Position = StartingPosition,
            Facing = StartingFacing
        };
    }

    internal static void Validate(
        ExplorationState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (string.IsNullOrWhiteSpace(state.MapId))
        {
            throw new ArgumentException(
                "Exploration map ID is required.",
                nameof(state));
        }

        if (!string.Equals(
            state.MapId,
            MapId,
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

        if (!IsTraversable(
            state.Floor,
            state.Position))
        {
            throw new ArgumentException(
                "The exploration position is not a traversable tile on the current floor.",
                nameof(state));
        }
    }

    internal static bool IsTraversable(
        ExplorationFloor floor,
        GridPosition position)
    {
        return floor switch
        {
            ExplorationFloor.GroundFloor =>
                GroundFloorTraversableTiles.Contains(
                    position),
            ExplorationFloor.UpperFloor =>
                UpperFloorTraversableTiles.Contains(
                    position),
            _ => false
        };
    }

    internal static bool TryGetStairDestination(
        ExplorationFloor floor,
        GridPosition position,
        out ExplorationFloor destinationFloor,
        out GridPosition destinationPosition)
    {
        if (floor == ExplorationFloor.GroundFloor
            && position == GroundFloorStairPosition)
        {
            destinationFloor =
                ExplorationFloor.UpperFloor;
            destinationPosition =
                UpperFloorStairPosition;
            return true;
        }

        if (floor == ExplorationFloor.UpperFloor
            && position == UpperFloorStairPosition)
        {
            destinationFloor =
                ExplorationFloor.GroundFloor;
            destinationPosition =
                GroundFloorStairPosition;
            return true;
        }

        destinationFloor = default;
        destinationPosition = default;
        return false;
    }
}

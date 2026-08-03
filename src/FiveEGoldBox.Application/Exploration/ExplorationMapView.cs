using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Exploration;

/// A door on the current floor, known to the party (not an unrevealed
/// secret) and not yet opened -- PositionA/PositionB are the two tiles it
/// connects, in no particular order, since neither tile is privileged over
/// the other.
public sealed record ExplorationDoorEdge
{
    internal ExplorationDoorEdge(
        GridPosition positionA,
        GridPosition positionB,
        bool isLocked)
    {
        PositionA = positionA;
        PositionB = positionB;
        IsLocked = isLocked;
    }

    public GridPosition PositionA { get; }

    public GridPosition PositionB { get; }

    /// Rendered distinctly from an ordinary closed door -- a door that is
    /// both secret and locked only appears here once its secrecy has been
    /// revealed.
    public bool IsLocked { get; }
}

/// A read-only projection of the current floor's grid geometry plus the
/// party's real position/facing — the same "internal definitions stay
/// internal, only a flattened client-safe record crosses the boundary"
/// shape FiveEGoldBox.Application.Combat.CombatView already establishes
/// for the tactical grid.
public sealed record ExplorationMapView
{
    internal ExplorationMapView(
        string mapId,
        string floor,
        int width,
        int height,
        IReadOnlyList<GridPosition> traversablePositions,
        IReadOnlyList<GridPosition> stairPositions,
        IReadOnlyList<ExplorationDoorEdge> doors,
        IReadOnlyList<GridPosition> treasurePositions,
        GridPosition partyPosition,
        ExplorationFacing partyFacing)
    {
        if (string.IsNullOrWhiteSpace(mapId))
        {
            throw new ArgumentException(
                "Map ID is required.",
                nameof(mapId));
        }

        if (string.IsNullOrWhiteSpace(floor))
        {
            throw new ArgumentException(
                "Floor ID is required.",
                nameof(floor));
        }

        ArgumentNullException.ThrowIfNull(traversablePositions);
        ArgumentNullException.ThrowIfNull(stairPositions);
        ArgumentNullException.ThrowIfNull(doors);
        ArgumentNullException.ThrowIfNull(treasurePositions);

        MapId = mapId;
        Floor = floor;
        Width = width;
        Height = height;
        TraversablePositions = Array.AsReadOnly(
            traversablePositions.ToArray());
        StairPositions = Array.AsReadOnly(stairPositions.ToArray());
        Doors = Array.AsReadOnly(doors.ToArray());
        TreasurePositions = Array.AsReadOnly(
            treasurePositions.ToArray());
        PartyPosition = partyPosition;
        PartyFacing = partyFacing;
    }

    public string MapId { get; }

    public string Floor { get; }

    public int Width { get; }

    public int Height { get; }

    /// Squares the party may occupy on this floor — everything else is
    /// solid. Current floor only, not every floor the map defines. A door's
    /// own edge is not part of this list either way -- a door no longer
    /// occupies a tile of its own, see ExplorationDoorEdge.
    public IReadOnlyList<GridPosition> TraversablePositions { get; }

    /// Every staircase's own square on this floor. Current floor only.
    public IReadOnlyList<GridPosition> StairPositions { get; }

    /// Doors known to the party (not an unrevealed secret) that are not
    /// currently open -- this covers both an ordinary not-yet-opened door
    /// and a secret door that has been found but not yet opened, since the
    /// two are visually identical once known.
    public IReadOnlyList<ExplorationDoorEdge> Doors { get; }

    /// Treasure not yet collected. Current floor only.
    public IReadOnlyList<GridPosition> TreasurePositions { get; }

    public GridPosition PartyPosition { get; }

    public ExplorationFacing PartyFacing { get; }
}

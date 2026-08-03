using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Exploration;

/// Every door on the current floor -- open or closed, revealed or not --
/// PositionA/PositionB are the two tiles it connects, in no particular
/// order, since neither tile is privileged over the other. A renderer
/// needs the full state, not just "here's a door to draw": an unrevealed
/// secret door still blocks movement and must render as a plain wall
/// (not as open passage, and not with anything that would give away its
/// presence), and an opened door still occupies its edge and should keep
/// reading as a doorway rather than vanishing into indistinguishable
/// open floor once passable.
public sealed record ExplorationDoorEdge
{
    internal ExplorationDoorEdge(
        GridPosition positionA,
        GridPosition positionB,
        bool isLocked,
        bool isOpen,
        bool isRevealed)
    {
        PositionA = positionA;
        PositionB = positionB;
        IsLocked = isLocked;
        IsOpen = isOpen;
        IsRevealed = isRevealed;
    }

    public GridPosition PositionA { get; }

    public GridPosition PositionB { get; }

    public bool IsLocked { get; }

    /// Once open, the door no longer blocks movement across this edge,
    /// but the edge itself should still read as a doorway rather than
    /// disappearing.
    public bool IsOpen { get; }

    /// False only for a secret door not yet found -- a renderer must
    /// treat an unrevealed door exactly like a plain wall (same texture,
    /// no door marker), since rendering it any other way would give away
    /// that something is there to find.
    public bool IsRevealed { get; }
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

    /// Every door on this floor, in every state -- see
    /// ExplorationDoorEdge's own comment for why open and unrevealed
    /// doors are included rather than filtered out here.
    public IReadOnlyList<ExplorationDoorEdge> Doors { get; }

    /// Treasure not yet collected. Current floor only.
    public IReadOnlyList<GridPosition> TreasurePositions { get; }

    public GridPosition PartyPosition { get; }

    public ExplorationFacing PartyFacing { get; }
}

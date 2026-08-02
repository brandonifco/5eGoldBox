using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Exploration;

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
        IReadOnlyList<GridPosition> closedDoorPositions,
        IReadOnlyList<GridPosition> lockedDoorPositions,
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
        ArgumentNullException.ThrowIfNull(closedDoorPositions);
        ArgumentNullException.ThrowIfNull(lockedDoorPositions);
        ArgumentNullException.ThrowIfNull(treasurePositions);

        MapId = mapId;
        Floor = floor;
        Width = width;
        Height = height;
        TraversablePositions = Array.AsReadOnly(
            traversablePositions.ToArray());
        StairPositions = Array.AsReadOnly(stairPositions.ToArray());
        ClosedDoorPositions = Array.AsReadOnly(
            closedDoorPositions.ToArray());
        LockedDoorPositions = Array.AsReadOnly(
            lockedDoorPositions.ToArray());
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
    /// solid. Current floor only, not every floor the map defines.
    public IReadOnlyList<GridPosition> TraversablePositions { get; }

    /// Every staircase's own square on this floor. Current floor only.
    public IReadOnlyList<GridPosition> StairPositions { get; }

    /// Doors known to the party (not an unrevealed secret) that are
    /// unlocked and not currently open -- this covers both an ordinary
    /// not-yet-opened door and a secret door that has been found but not
    /// yet opened, since the two are visually identical once known.
    public IReadOnlyList<GridPosition> ClosedDoorPositions { get; }

    /// Doors known to the party that are locked -- rendered distinctly
    /// from an ordinary closed door. A door that is both secret and
    /// locked only appears here once its secrecy has been revealed.
    public IReadOnlyList<GridPosition> LockedDoorPositions { get; }

    /// Treasure not yet collected. Current floor only.
    public IReadOnlyList<GridPosition> TreasurePositions { get; }

    public GridPosition PartyPosition { get; }

    public ExplorationFacing PartyFacing { get; }
}

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

        MapId = mapId;
        Floor = floor;
        Width = width;
        Height = height;
        TraversablePositions = Array.AsReadOnly(
            traversablePositions.ToArray());
        StairPositions = Array.AsReadOnly(stairPositions.ToArray());
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

    public GridPosition PartyPosition { get; }

    public ExplorationFacing PartyFacing { get; }
}

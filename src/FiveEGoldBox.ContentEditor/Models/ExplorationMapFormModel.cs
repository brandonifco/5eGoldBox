using FiveEGoldBox.Application.Content.V1;

namespace FiveEGoldBox.ContentEditor.Models;

/// Editable projection of an ExplorationMapDefinitionV1, backing the visual
/// grid editor. Internal rather than public for the same reason every other
/// scenario form model is: its DTOs are internal to FiveEGoldBox.Application,
/// and a public member with an internal type is an accessibility-consistency
/// error.
///
/// TraversablePositions is deliberately an ordered List, not a HashSet.
/// Committed content's cell order is hand-authored and not purely row-major
/// (Watchtower's ground floor ends with two cells clearly appended after the
/// fact), so round-tripping through an unordered set would rewrite every
/// untouched floor's cell order on the first save. Toggling instead mutates
/// this list in place -- removing a cell leaves the rest in their original
/// order, and adding one appends, which is what hand-editing would have done.
internal sealed class ExplorationMapFormModel
{
    public string MapId { get; set; } = "";

    public int Width { get; set; }

    public int Height { get; set; }

    public string StartingFloor { get; set; } = "";

    public int StartingX { get; set; }

    public int StartingY { get; set; }

    public ExplorationFacingV1 StartingFacing { get; set; }

    public List<ExplorationFloorFormModel> Floors { get; set; } = [];

    public static ExplorationMapFormModel FromDefinition(
        ExplorationMapDefinitionV1 map)
    {
        return new ExplorationMapFormModel
        {
            MapId = map.MapId,
            Width = map.Width,
            Height = map.Height,
            StartingFloor = map.StartingFloor,
            StartingX = map.StartingPosition.X,
            StartingY = map.StartingPosition.Y,
            StartingFacing = map.StartingFacing,
            Floors = map.Floors
                .Select(ExplorationFloorFormModel.FromDefinition)
                .ToList()
        };
    }

    public ExplorationMapDefinitionV1 ToDefinition()
    {
        return new ExplorationMapDefinitionV1
        {
            MapId = MapId,
            Width = Width,
            Height = Height,
            StartingFloor = StartingFloor,
            StartingPosition = new GridPositionV1 { X = StartingX, Y = StartingY },
            StartingFacing = StartingFacing,
            Floors = Floors.Select(floor => floor.ToDefinition()).ToList()
        };
    }
}

/// One floor's editable state. Stairs/Doors/Treasures/Npcs are carried
/// through untouched by the grid editor -- it only toggles traversable cells
/// today -- but they still round-trip through this model so that saving a
/// floor never silently drops the features authored on it.
internal sealed class ExplorationFloorFormModel
{
    public string Floor { get; set; } = "";

    public List<GridPositionV1> TraversablePositions { get; set; } = [];

    public List<StairDefinitionV1> Stairs { get; set; } = [];

    public List<DoorDefinitionV1> Doors { get; set; } = [];

    public List<TreasureDefinitionV1> Treasures { get; set; } = [];

    public List<NpcDefinitionV1> Npcs { get; set; } = [];

    public static ExplorationFloorFormModel FromDefinition(
        ExplorationFloorDefinitionV1 floor)
    {
        return new ExplorationFloorFormModel
        {
            Floor = floor.Floor,
            TraversablePositions = floor.TraversablePositions.ToList(),
            Stairs = floor.Stairs.ToList(),
            Doors = floor.Doors.ToList(),
            Treasures = floor.Treasures.ToList(),
            Npcs = floor.Npcs.ToList()
        };
    }

    public ExplorationFloorDefinitionV1 ToDefinition()
    {
        return new ExplorationFloorDefinitionV1
        {
            Floor = Floor,
            TraversablePositions = TraversablePositions,
            Stairs = Stairs,
            Doors = Doors,
            Treasures = Treasures,
            Npcs = Npcs
        };
    }

    public bool IsTraversable(
        int x,
        int y)
    {
        return TraversablePositions.Any(position => position.X == x && position.Y == y);
    }

    /// Removing leaves every other cell in its original order; adding appends,
    /// matching how the committed files were themselves hand-extended.
    public void ToggleTraversable(
        int x,
        int y)
    {
        GridPositionV1? existing = TraversablePositions
            .FirstOrDefault(position => position.X == x && position.Y == y);

        if (existing is not null)
        {
            TraversablePositions.Remove(existing);
            return;
        }

        TraversablePositions.Add(new GridPositionV1 { X = x, Y = y });
    }
}

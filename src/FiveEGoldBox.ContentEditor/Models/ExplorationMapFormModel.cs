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

    /// Suggests an id for a newly placed feature, following the committed
    /// content's own "kind.scenario-slug.name" convention (door.watchtower
    /// .armory-door). Uniqueness is checked across every floor, not just the
    /// one being edited, because the validator's own id-collision rules are
    /// map-wide. This is only a starting point -- the author is expected to
    /// rename it to something meaningful, so it deliberately doesn't try to
    /// be clever about what the feature is for.
    public string SuggestFeatureId(
        string kind)
    {
        string slug = MapId.StartsWith("map.", StringComparison.Ordinal)
            ? MapId["map.".Length..]
            : MapId;

        HashSet<string> taken = new(StringComparer.Ordinal);

        foreach (var floor in Floors)
        {
            foreach (var door in floor.Doors)
            {
                taken.Add(door.DoorId);
            }

            foreach (var treasure in floor.Treasures)
            {
                taken.Add(treasure.TreasureId);
            }

            foreach (var npc in floor.Npcs)
            {
                taken.Add(npc.NpcId);
            }
        }

        for (int index = 1; ; index++)
        {
            string candidate = $"{kind}.{slug}.new-{index}";

            if (taken.Add(candidate))
            {
                return candidate;
            }
        }
    }
}

/// One floor's editable state. Stairs/Doors/Treasures/Npcs are carried
/// through this model so that saving a floor never silently drops the
/// features authored on it, and -- since Phase 4c -- are editable in place.
internal sealed class ExplorationFloorFormModel
{
    public string Floor { get; set; } = "";

    public List<GridPositionV1> TraversablePositions { get; set; } = [];

    public List<StairFormModel> Stairs { get; set; } = [];

    public List<DoorFormModel> Doors { get; set; } = [];

    public List<TreasureFormModel> Treasures { get; set; } = [];

    public List<NpcFormModel> Npcs { get; set; } = [];

    public static ExplorationFloorFormModel FromDefinition(
        ExplorationFloorDefinitionV1 floor)
    {
        return new ExplorationFloorFormModel
        {
            Floor = floor.Floor,
            TraversablePositions = floor.TraversablePositions.ToList(),
            Stairs = floor.Stairs.Select(StairFormModel.FromDefinition).ToList(),
            Doors = floor.Doors.Select(DoorFormModel.FromDefinition).ToList(),
            Treasures = floor.Treasures.Select(TreasureFormModel.FromDefinition).ToList(),
            Npcs = floor.Npcs.Select(NpcFormModel.FromDefinition).ToList()
        };
    }

    public ExplorationFloorDefinitionV1 ToDefinition()
    {
        return new ExplorationFloorDefinitionV1
        {
            Floor = Floor,
            TraversablePositions = TraversablePositions,
            Stairs = Stairs.Select(stair => stair.ToDefinition()).ToList(),
            Doors = Doors.Select(door => door.ToDefinition()).ToList(),
            Treasures = Treasures.Select(treasure => treasure.ToDefinition()).ToList(),
            Npcs = Npcs.Select(npc => npc.ToDefinition()).ToList()
        };
    }

    public StairFormModel? FindStair(int x, int y) =>
        Stairs.FirstOrDefault(stair => stair.X == x && stair.Y == y);

    public DoorFormModel? FindDoor(int x, int y, ExplorationFacingV1 side) =>
        Doors.FirstOrDefault(door => door.X == x && door.Y == y && door.Side == side);

    public TreasureFormModel? FindTreasure(int x, int y) =>
        Treasures.FirstOrDefault(treasure => treasure.X == x && treasure.Y == y);

    public NpcFormModel? FindNpc(int x, int y) =>
        Npcs.FirstOrDefault(npc => npc.X == x && npc.Y == y);

    public bool HasAnyFeature(int x, int y) =>
        FindStair(x, y) is not null
        || FindTreasure(x, y) is not null
        || FindNpc(x, y) is not null
        || Doors.Any(door => door.X == x && door.Y == y);

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

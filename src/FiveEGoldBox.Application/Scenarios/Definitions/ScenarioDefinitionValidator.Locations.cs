using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Runtime;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

internal static partial class ScenarioDefinitionValidator
{
    private static void AddLocationIssues(
        ScenarioDefinition definition,
        ValidatedRuleset? ruleset,
        List<ValidationIssue> issues)
    {
        if (definition.Locations.Count == 0)
        {
            issues.Add(Error(
                "scenario.locations.empty",
                "A scenario must declare at least one location."));
        }

        AddDuplicateIdIssues(
            issues,
            definition.Locations.Select(location => location.LocationId),
            "scenario.locations.duplicate_id",
            "location ID");

        foreach (ScenarioLocationDefinition location in definition.Locations)
        {
            AddIfBlank(
                issues,
                location.LocationId,
                "scenario.locations.id_required",
                "Location IDs must not be blank.");

            if (location.ExplorationMap is not null)
            {
                AddMapIssues(location, location.ExplorationMap, ruleset, issues);
            }
            else if (location.ExplorableProgressIds.Count > 0)
            {
                issues.Add(Error(
                    "scenario.locations.explorable_without_map",
                    $"Location '{location.LocationId}' declares explorable progress but has no map."));
            }

            AddUnknownProgressIssues(
                issues,
                location.ExplorableProgressIds,
                ToSet(definition.Progress.ProgressIds),
                "scenario.locations.explorable_progress_unknown",
                $"Location '{location.LocationId}'");
        }

        AddDuplicateIdIssues(
            issues,
            definition.Locations
                .Where(location => location.ExplorationMap is not null)
                .Select(location => location.ExplorationMap!.MapId),
            "scenario.maps.duplicate_id",
            "map ID");
    }

    private static void AddMapIssues(
        ScenarioLocationDefinition location,
        ExplorationMapDefinition map,
        ValidatedRuleset? ruleset,
        List<ValidationIssue> issues)
    {
        string where = $"map '{map.MapId}' in location '{location.LocationId}'";

        if (map.Width <= 0 || map.Height <= 0)
        {
            issues.Add(Error(
                "scenario.map.dimensions",
                $"The {where} must have positive width and height."));
            return;
        }

        if (map.Floors.Count == 0)
        {
            issues.Add(Error(
                "scenario.map.no_floors",
                $"The {where} must declare at least one floor."));
            return;
        }

        AddDuplicateIdIssues(
            issues,
            map.Floors.Select(floor => floor.Floor),
            "scenario.map.duplicate_floor",
            $"floor in {where}");

        Dictionary<string, HashSet<GridPosition>> traversable =
            new(StringComparer.Ordinal);

        foreach (ExplorationFloorDefinition floor in map.Floors)
        {
            HashSet<GridPosition> positions = [];

            foreach (GridPosition position in floor.TraversablePositions)
            {
                if (!IsWithin(map, position))
                {
                    issues.Add(Error(
                        "scenario.map.position_out_of_bounds",
                        $"Position ({position.X}, {position.Y}) on floor {floor.Floor} lies outside the {where}."));
                    continue;
                }

                if (!positions.Add(position))
                {
                    issues.Add(Error(
                        "scenario.map.duplicate_position",
                        $"Position ({position.X}, {position.Y}) is declared twice on floor {floor.Floor} of the {where}."));
                }
            }

            if (positions.Count == 0)
            {
                issues.Add(Error(
                    "scenario.map.floor_impassable",
                    $"Floor {floor.Floor} of the {where} has no traversable square."));
            }

            traversable[floor.Floor] = positions;
        }

        AddStairIssues(map, traversable, where, issues);
        AddDoorIssues(map, traversable, where, issues);
        AddTreasureIssues(map, traversable, where, ruleset, issues);
        AddStartingStateIssues(map, traversable, where, issues);
    }

    /// Stairs must start somewhere the party can stand and arrive somewhere it
    /// can stand, on a floor that exists. A stair into solid rock is the kind of
    /// mistake that only shows up when a player walks into it.
    private static void AddStairIssues(
        ExplorationMapDefinition map,
        Dictionary<string, HashSet<GridPosition>> traversable,
        string where,
        List<ValidationIssue> issues)
    {
        foreach (ExplorationFloorDefinition floor in map.Floors)
        {
            foreach (StairDefinition stair in floor.Stairs)
            {
                if (!traversable.TryGetValue(
                        floor.Floor,
                        out HashSet<GridPosition>? origin)
                    || !origin.Contains(stair.Position))
                {
                    issues.Add(Error(
                        "scenario.map.stair_origin_impassable",
                        $"A stair on floor {floor.Floor} of the {where} stands on an untraversable square."));
                }

                if (!traversable.TryGetValue(
                        stair.DestinationFloor,
                        out HashSet<GridPosition>? destination))
                {
                    issues.Add(Error(
                        "scenario.map.stair_floor_unknown",
                        $"A stair on floor {floor.Floor} of the {where} leads to undeclared floor {stair.DestinationFloor}."));
                    continue;
                }

                if (!destination.Contains(stair.DestinationPosition))
                {
                    issues.Add(Error(
                        "scenario.map.stair_destination_impassable",
                        $"A stair on floor {floor.Floor} of the {where} arrives on an untraversable square."));
                }
            }
        }
    }

    /// A door blocks its own tile until opened, so it must not sit on ground
    /// already traversable without it (that door would do nothing), and it
    /// must not fight another door or a stair for the same square.
    private static void AddDoorIssues(
        ExplorationMapDefinition map,
        Dictionary<string, HashSet<GridPosition>> traversable,
        string where,
        List<ValidationIssue> issues)
    {
        AddDuplicateIdIssues(
            issues,
            map.Floors.SelectMany(floor => floor.Doors)
                .Select(door => door.DoorId),
            "scenario.map.duplicate_door_id",
            "door ID");

        foreach (ExplorationFloorDefinition floor in map.Floors)
        {
            HashSet<GridPosition> occupied =
                new(floor.Stairs.Select(stair => stair.Position));

            foreach (DoorDefinition door in floor.Doors)
            {
                AddIfBlank(
                    issues,
                    door.DoorId,
                    "scenario.map.door_id_required",
                    "Door IDs must not be blank.");

                if (!IsWithin(map, door.Position))
                {
                    issues.Add(Error(
                        "scenario.map.door_position_out_of_bounds",
                        $"A door on floor {floor.Floor} of the {where} lies outside the map bounds."));
                    continue;
                }

                if (traversable.TryGetValue(
                        floor.Floor,
                        out HashSet<GridPosition>? floorTraversable)
                    && floorTraversable.Contains(door.Position))
                {
                    issues.Add(Error(
                        "scenario.map.door_position_already_traversable",
                        $"A door on floor {floor.Floor} of the {where} sits on a square that is already traversable without it."));
                }

                if (!occupied.Add(door.Position))
                {
                    issues.Add(Error(
                        "scenario.map.door_position_collision",
                        $"A door on floor {floor.Floor} of the {where} shares its square with another door or a stair."));
                }
            }
        }
    }

    /// Unlike a door, treasure does not block its own tile -- it has to sit
    /// somewhere the party can already reach, and it needs a declared reward
    /// even though v1 only flips a collected flag rather than granting it.
    private static void AddTreasureIssues(
        ExplorationMapDefinition map,
        Dictionary<string, HashSet<GridPosition>> traversable,
        string where,
        ValidatedRuleset? ruleset,
        List<ValidationIssue> issues)
    {
        AddDuplicateIdIssues(
            issues,
            map.Floors.SelectMany(floor => floor.Treasures)
                .Select(treasure => treasure.TreasureId),
            "scenario.map.duplicate_treasure_id",
            "treasure ID");

        foreach (ExplorationFloorDefinition floor in map.Floors)
        {
            HashSet<GridPosition> occupied = new(
                floor.Stairs.Select(stair => stair.Position)
                    .Concat(floor.Doors.Select(door => door.Position)));

            foreach (TreasureDefinition treasure in floor.Treasures)
            {
                AddIfBlank(
                    issues,
                    treasure.TreasureId,
                    "scenario.map.treasure_id_required",
                    "Treasure IDs must not be blank.");

                if (!IsWithin(map, treasure.Position))
                {
                    issues.Add(Error(
                        "scenario.map.treasure_position_out_of_bounds",
                        $"A treasure on floor {floor.Floor} of the {where} lies outside the map bounds."));
                    continue;
                }

                if (!traversable.TryGetValue(
                        floor.Floor,
                        out HashSet<GridPosition>? floorTraversable)
                    || !floorTraversable.Contains(treasure.Position))
                {
                    issues.Add(Error(
                        "scenario.map.treasure_position_unreachable",
                        $"A treasure on floor {floor.Floor} of the {where} sits on a square the party cannot reach."));
                }

                if (occupied.Contains(treasure.Position))
                {
                    issues.Add(Error(
                        "scenario.map.treasure_position_collision",
                        $"A treasure on floor {floor.Floor} of the {where} shares its square with a door or a stair."));
                }

                if (string.IsNullOrWhiteSpace(treasure.ItemId)
                    && treasure.GoldPieces is null)
                {
                    issues.Add(Error(
                        "scenario.map.treasure_no_reward",
                        $"A treasure on floor {floor.Floor} of the {where} declares neither an item nor gold."));
                }

                if (treasure.GoldPieces is int goldPieces
                    && goldPieces < 0)
                {
                    issues.Add(Error(
                        "scenario.map.treasure_gold_negative",
                        $"A treasure on floor {floor.Floor} of the {where} declares negative gold."));
                }

                if (ruleset is not null
                    && !string.IsNullOrWhiteSpace(treasure.ItemId)
                    && !ruleset.Definition.EquipmentItems.Any(item => string.Equals(
                        item.Id,
                        treasure.ItemId,
                        StringComparison.Ordinal)))
                {
                    issues.Add(Error(
                        "scenario.map.treasure_item_id_unresolved",
                        $"A treasure on floor {floor.Floor} of the {where} references item '{treasure.ItemId}', which the scenario's ruleset does not define."));
                }
            }
        }
    }

    private static void AddStartingStateIssues(
        ExplorationMapDefinition map,
        Dictionary<string, HashSet<GridPosition>> traversable,
        string where,
        List<ValidationIssue> issues)
    {
        if (!traversable.TryGetValue(
            map.StartingFloor,
            out HashSet<GridPosition>? startingFloor))
        {
            issues.Add(Error(
                "scenario.map.starting_floor_unknown",
                $"The {where} starts on undeclared floor {map.StartingFloor}."));
            return;
        }

        if (!startingFloor.Contains(map.StartingPosition))
        {
            issues.Add(Error(
                "scenario.map.starting_position_impassable",
                $"The {where} starts the party on an untraversable square."));
        }
    }

    private static bool IsWithin(
        ExplorationMapDefinition map,
        GridPosition position)
    {
        return position.X >= 0
            && position.Y >= 0
            && position.X < map.Width
            && position.Y < map.Height;
    }
}

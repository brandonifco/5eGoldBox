using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Console;

/// Converts a Tiled map editor JSON export (.tmj) into this engine's own
/// ExplorationMap/Trigger JSON shape (data/schemas/scenario-pack.schema.json).
///
/// Authoring convention: a tile layer with a custom IsFloor=true property is
/// one floor, named by the layer's own name; a non-zero (GID-masked) cell is
/// traversable. Exactly one object of type/class "Start" (properties Floor,
/// Facing) fixes where the party begins. Any number of "Stair" objects
/// (properties Floor, DestinationFloor, DestinationX, DestinationY) and
/// "Trigger" objects (properties Floor, TriggerId, ResultingProgressId
/// required; DisplayName, RequiredProgressIds, EncounterId optional) round
/// out the rest. Doors/secret doors/treasure aren't
/// convertible yet -- those engine concepts don't exist (see
/// docs/2026-07-30-adventure-authoring-tool-plan.md, Phase B).
internal static class TiledMapConverter
{
    /// Clears Tiled's 3 high flip-flag bits (horizontal/vertical/diagonal)
    /// from a raw GID, leaving the underlying tile ID. An empty cell is
    /// GID 0 either way, so this only matters for painted-but-flipped tiles.
    private const long GidFlipMask = 0x1FFFFFFF;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = false
    };

    internal static TiledConversionResult Convert(
        string tiledJson,
        string mapId,
        string locationId)
    {
        ArgumentNullException.ThrowIfNull(tiledJson);
        ArgumentNullException.ThrowIfNull(mapId);
        ArgumentNullException.ThrowIfNull(locationId);

        TiledDocument document;

        try
        {
            document = JsonSerializer.Deserialize<TiledDocument>(tiledJson, SerializerOptions)
                ?? throw new JsonException("The Tiled document is empty.");
        }
        catch (JsonException exception)
        {
            return TiledConversionResult.Failure(Issue(
                "content.tiled_import.parse_failed",
                $"Could not parse the Tiled map: {exception.Message}"));
        }

        List<TiledLayer> floorLayers = document.Layers
            .Where(layer =>
                string.Equals(layer.Type, "tilelayer", StringComparison.Ordinal)
                && GetBoolProperty(layer.Properties, "IsFloor") == true)
            .ToList();

        if (floorLayers.Count == 0)
        {
            return TiledConversionResult.Failure(Issue(
                "content.tiled_import.no_floor_layers",
                "The Tiled map has no tile layer with an IsFloor=true property."));
        }

        HashSet<string> floorNames = floorLayers
            .Select(layer => layer.Name)
            .ToHashSet(StringComparer.Ordinal);

        List<TiledObject> objects = document.Layers
            .Where(layer => string.Equals(layer.Type, "objectgroup", StringComparison.Ordinal))
            .SelectMany(layer => layer.Objects ?? [])
            .ToList();

        List<TiledObject> startObjects = objects
            .Where(candidate => GetObjectClass(candidate) == "Start")
            .ToList();

        if (startObjects.Count == 0)
        {
            return TiledConversionResult.Failure(Issue(
                "content.tiled_import.no_start_object",
                "The Tiled map has no object of type/class \"Start\"."));
        }

        if (startObjects.Count > 1)
        {
            return TiledConversionResult.Failure(Issue(
                "content.tiled_import.multiple_start_objects",
                $"The Tiled map has {startObjects.Count} \"Start\" objects; exactly one is required."));
        }

        List<ValidationIssue> issues = [];
        Dictionary<string, JsonArray> stairsByFloor = [];
        JsonArray floorNodes = [];

        foreach (TiledLayer layer in floorLayers)
        {
            JsonArray traversable = BuildTraversablePositions(layer, document);
            JsonArray stairs = [];

            stairsByFloor[layer.Name] = stairs;
            floorNodes.Add(new JsonObject
            {
                ["Floor"] = layer.Name,
                ["TraversablePositions"] = traversable,
                ["Stairs"] = stairs
            });
        }

        foreach (TiledObject stairObject in objects.Where(candidate => GetObjectClass(candidate) == "Stair"))
        {
            string? floor = GetStringProperty(stairObject.Properties, "Floor");
            string? destinationFloor = GetStringProperty(stairObject.Properties, "DestinationFloor");
            int? destinationX = GetIntProperty(stairObject.Properties, "DestinationX");
            int? destinationY = GetIntProperty(stairObject.Properties, "DestinationY");

            if (floor is null || !stairsByFloor.TryGetValue(floor, out JsonArray? stairs))
            {
                issues.Add(Issue(
                    "content.tiled_import.stair_floor_unknown",
                    $"A \"Stair\" object (\"{stairObject.Name}\") names Floor \"{floor}\", which isn't one of this map's floor layers."));
                continue;
            }

            if (destinationFloor is null || destinationX is null || destinationY is null)
            {
                issues.Add(Issue(
                    "content.tiled_import.stair_missing_field",
                    $"A \"Stair\" object (\"{stairObject.Name}\") is missing DestinationFloor/DestinationX/DestinationY."));
                continue;
            }

            stairs.Add(new JsonObject
            {
                ["Position"] = Position(TileX(stairObject, document), TileY(stairObject, document)),
                ["DestinationFloor"] = destinationFloor,
                ["DestinationPosition"] = Position(destinationX.Value, destinationY.Value)
            });
        }

        JsonArray triggerNodes = [];

        foreach (TiledObject triggerObject in objects.Where(candidate => GetObjectClass(candidate) == "Trigger"))
        {
            string? floor = GetStringProperty(triggerObject.Properties, "Floor");
            string? triggerId = GetStringProperty(triggerObject.Properties, "TriggerId");
            string? resultingProgressId = GetStringProperty(triggerObject.Properties, "ResultingProgressId");

            if (floor is null || !floorNames.Contains(floor))
            {
                issues.Add(Issue(
                    "content.tiled_import.trigger_floor_unknown",
                    $"A \"Trigger\" object (\"{triggerObject.Name}\") names Floor \"{floor}\", which isn't one of this map's floor layers."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(triggerId) || string.IsNullOrWhiteSpace(resultingProgressId))
            {
                issues.Add(Issue(
                    "content.tiled_import.trigger_missing_field",
                    $"A \"Trigger\" object (\"{triggerObject.Name}\") is missing a required TriggerId/ResultingProgressId property."));
                continue;
            }

            string displayName = GetStringProperty(triggerObject.Properties, "DisplayName")
                ?? triggerObject.Name;
            string[] requiredProgressIds = (GetStringProperty(triggerObject.Properties, "RequiredProgressIds") ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string? encounterId = GetStringProperty(triggerObject.Properties, "EncounterId");

            JsonObject trigger = new()
            {
                ["TriggerId"] = triggerId,
                ["DisplayName"] = displayName,
                ["LocationId"] = locationId,
                ["Floor"] = floor,
                ["Position"] = Position(TileX(triggerObject, document), TileY(triggerObject, document)),
                ["RequiredProgressIds"] = new JsonArray(requiredProgressIds.Select(id => (JsonNode)id).ToArray()),
                ["ResultingProgressId"] = resultingProgressId
            };

            if (encounterId is not null)
            {
                trigger["EncounterId"] = encounterId;
            }

            triggerNodes.Add(trigger);
        }

        if (issues.Any(issue => issue.Severity == ValidationSeverity.Error))
        {
            return TiledConversionResult.Failure(issues);
        }

        TiledObject start = startObjects[0];
        string? startFloor = GetStringProperty(start.Properties, "Floor");
        string? startFacing = GetStringProperty(start.Properties, "Facing");

        if (startFloor is null || !floorNames.Contains(startFloor) || startFacing is null)
        {
            return TiledConversionResult.Failure(Issue(
                "content.tiled_import.start_missing_field",
                "The \"Start\" object is missing a Floor (matching a floor layer) or Facing property."));
        }

        JsonObject explorationMap = new()
        {
            ["MapId"] = mapId,
            ["Width"] = document.Width,
            ["Height"] = document.Height,
            ["StartingFloor"] = startFloor,
            ["StartingPosition"] = Position(TileX(start, document), TileY(start, document)),
            ["StartingFacing"] = startFacing,
            ["Floors"] = floorNodes
        };

        return TiledConversionResult.Success(explorationMap, triggerNodes, issues);
    }

    /// Merges a converted map into an existing scenario document: sets or
    /// replaces the target location's ExplorationMap, and replaces this
    /// location's tile-anchored triggers (both Floor and Position set) with
    /// the freshly generated ones -- non-spatial triggers for the same
    /// location are hand-authored story beats, not map geometry, and are
    /// left alone.
    internal static IReadOnlyList<ValidationIssue> Merge(
        JsonObject scenarioRoot,
        JsonObject explorationMap,
        JsonArray triggers,
        string locationId,
        string? displayName)
    {
        ArgumentNullException.ThrowIfNull(scenarioRoot);
        ArgumentNullException.ThrowIfNull(explorationMap);
        ArgumentNullException.ThrowIfNull(triggers);
        ArgumentNullException.ThrowIfNull(locationId);

        JsonArray locations = scenarioRoot["Locations"] as JsonArray
            ?? throw new ArgumentException(
                "The scenario document has no Locations array.",
                nameof(scenarioRoot));

        JsonObject? location = locations
            .OfType<JsonObject>()
            .FirstOrDefault(candidate => string.Equals(
                candidate["LocationId"]?.GetValue<string>(),
                locationId,
                StringComparison.Ordinal));

        if (location is null)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return [Issue(
                    "content.tiled_import.display_name_required",
                    $"Location '{locationId}' doesn't exist yet in this scenario; a display name is required to create it.")];
            }

            location = new JsonObject
            {
                ["LocationId"] = locationId,
                ["DisplayName"] = displayName
            };
            locations.Add(location);
        }
        else if (!string.IsNullOrWhiteSpace(displayName))
        {
            location["DisplayName"] = displayName;
        }

        location["ExplorationMap"] = explorationMap;

        JsonArray existingTriggers = scenarioRoot["Triggers"] as JsonArray
            ?? throw new ArgumentException(
                "The scenario document has no Triggers array.",
                nameof(scenarioRoot));

        for (int index = existingTriggers.Count - 1; index >= 0; index--)
        {
            if (existingTriggers[index] is JsonObject candidate && IsTileAnchoredFor(candidate, locationId))
            {
                existingTriggers.RemoveAt(index);
            }
        }

        while (triggers.Count > 0)
        {
            JsonNode? trigger = triggers[0];
            triggers.RemoveAt(0);
            existingTriggers.Add(trigger);
        }

        return [];
    }

    private static bool IsTileAnchoredFor(
        JsonObject trigger,
        string locationId)
    {
        return string.Equals(
                trigger["LocationId"]?.GetValue<string>(),
                locationId,
                StringComparison.Ordinal)
            && trigger["Floor"] is not null
            && trigger["Position"] is not null;
    }

    private static JsonArray BuildTraversablePositions(
        TiledLayer layer,
        TiledDocument document)
    {
        JsonArray traversable = [];
        IReadOnlyList<long> data = layer.Data
            ?? throw new InvalidOperationException(
                $"Floor layer \"{layer.Name}\" has no tile data.");

        for (int row = 0; row < document.Height; row++)
        {
            for (int column = 0; column < document.Width; column++)
            {
                long gid = data[(row * document.Width) + column];

                if ((gid & GidFlipMask) != 0)
                {
                    traversable.Add(Position(column, row));
                }
            }
        }

        return traversable;
    }

    private static int TileX(TiledObject tiledObject, TiledDocument document) =>
        (int)(tiledObject.X / document.TileWidth);

    private static int TileY(TiledObject tiledObject, TiledDocument document) =>
        (int)(tiledObject.Y / document.TileHeight);

    private static JsonObject Position(int x, int y) =>
        new() { ["X"] = x, ["Y"] = y };

    private static string? GetObjectClass(TiledObject tiledObject) =>
        tiledObject.Class ?? tiledObject.Type;

    private static TiledProperty? FindProperty(
        IReadOnlyList<TiledProperty>? properties,
        string name)
    {
        return properties?.FirstOrDefault(
            property => string.Equals(property.Name, name, StringComparison.Ordinal));
    }

    private static string? GetStringProperty(
        IReadOnlyList<TiledProperty>? properties,
        string name)
    {
        TiledProperty? property = FindProperty(properties, name);
        return property is null || property.Value.ValueKind != JsonValueKind.String
            ? null
            : property.Value.GetString();
    }

    private static bool? GetBoolProperty(
        IReadOnlyList<TiledProperty>? properties,
        string name)
    {
        TiledProperty? property = FindProperty(properties, name);

        return property?.Value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static int? GetIntProperty(
        IReadOnlyList<TiledProperty>? properties,
        string name)
    {
        TiledProperty? property = FindProperty(properties, name);
        return property is null || property.Value.ValueKind != JsonValueKind.Number
            ? null
            : property.Value.GetInt32();
    }

    private static ValidationIssue Issue(string code, string message) =>
        new(ValidationSeverity.Error, code, message);

    private sealed record TiledDocument
    {
        [JsonPropertyName("width")]
        public int Width { get; init; }

        [JsonPropertyName("height")]
        public int Height { get; init; }

        [JsonPropertyName("tilewidth")]
        public int TileWidth { get; init; }

        [JsonPropertyName("tileheight")]
        public int TileHeight { get; init; }

        [JsonPropertyName("layers")]
        public List<TiledLayer> Layers { get; init; } = [];
    }

    private sealed record TiledLayer
    {
        [JsonPropertyName("type")]
        public string Type { get; init; } = "";

        [JsonPropertyName("name")]
        public string Name { get; init; } = "";

        [JsonPropertyName("data")]
        public List<long>? Data { get; init; }

        [JsonPropertyName("objects")]
        public List<TiledObject>? Objects { get; init; }

        [JsonPropertyName("properties")]
        public List<TiledProperty>? Properties { get; init; }
    }

    private sealed record TiledObject
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = "";

        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("class")]
        public string? Class { get; init; }

        [JsonPropertyName("x")]
        public double X { get; init; }

        [JsonPropertyName("y")]
        public double Y { get; init; }

        [JsonPropertyName("properties")]
        public List<TiledProperty>? Properties { get; init; }
    }

    private sealed record TiledProperty
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = "";

        [JsonPropertyName("value")]
        public JsonElement Value { get; init; }
    }
}

/// The outcome of converting a Tiled map: either a ready-to-merge
/// ExplorationMap plus Triggers, or a list of issues explaining why not --
/// mirrors ValidationResult's "collect every issue" shape rather than
/// throwing on the first problem, matching ContentPackValidation's own
/// philosophy for reporting on hand-authored content.
internal sealed record TiledConversionResult
{
    private TiledConversionResult(
        JsonObject? explorationMap,
        JsonArray triggers,
        IReadOnlyList<ValidationIssue> issues)
    {
        ExplorationMap = explorationMap;
        Triggers = triggers;
        Issues = issues;
    }

    internal JsonObject? ExplorationMap { get; }

    internal JsonArray Triggers { get; }

    internal IReadOnlyList<ValidationIssue> Issues { get; }

    internal bool Succeeded => ExplorationMap is not null;

    internal static TiledConversionResult Success(
        JsonObject explorationMap,
        JsonArray triggers,
        IReadOnlyList<ValidationIssue> issues) =>
        new(explorationMap, triggers, issues);

    internal static TiledConversionResult Failure(ValidationIssue issue) =>
        new(null, [], [issue]);

    internal static TiledConversionResult Failure(IReadOnlyList<ValidationIssue> issues) =>
        new(null, [], issues);
}

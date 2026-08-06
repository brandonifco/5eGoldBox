using System.Globalization;
using System.Text;
using FiveEGoldBox.Application.Content.V1;
using static FiveEGoldBox.ContentEditor.Services.JsonLayoutPrimitives;

namespace FiveEGoldBox.ContentEditor.Services;

/// Hand-rolled JSON layout for the scenario content shapes this editor
/// writes (ScenarioLocationDefinitionV1, TravelRouteDefinitionV1,
/// ShopDefinitionV1), matching data/scenarios/*/scenario.json's existing
/// hand-authored formatting byte-for-byte, verified against all three real
/// scenario files (see ScenarioNoOpSaveFormattingTests). Reuses
/// JsonLayoutPrimitives for every rule that matches RulesetJsonFormatting's
/// -- 4-space indentation, a top-level content record always fully exploded
/// -- but scenario content has one convention core.json does not: a compact
/// array of progress-id strings (ExplorableProgressIds,
/// RequiredProgressIds) stays on one line for one or two items and only
/// explodes to one item per line at three or more (confirmed against real
/// content: a two-item ExplorableProgressIds and a two-item Trigger's
/// RequiredProgressIds both stay inline, a three-item ExplorableProgressIds
/// explodes) -- one item looser than RulesetJsonFormatting's own "explodes
/// at two or more" rule for Properties/AbilityModifiers. See
/// RenderProgressIdList below.
///
/// ExplorationMap is rendered two different ways depending on whether the
/// caller is editing it. A location whose map is untouched still copies the
/// exact original bytes through verbatim (from
/// ScenarioPackDocument.FindRawExplorationMapText), because real committed
/// content orders a floor's Doors/Treasures/Npcs inconsistently from file to
/// file (Hollow Mill writes Npcs first, Watchtower writes Doors/Treasures
/// first) and no fixed field order reproduces both byte-for-byte. Editing a
/// map goes through RenderExplorationMap instead, which writes one canonical
/// field order -- the DTO's own, matching Watchtower and Sunken Chapel
/// exactly. Editing a Hollow Mill floor therefore also normalizes its
/// Npcs/Doors/Treasures ordering; that file is being rewritten anyway, and
/// carrying each floor's original key order through the form round trip
/// purely to preserve an inconsistency isn't worth the machinery.
internal static class ScenarioJsonFormatting
{
    // ----- Top-level arrays -----

    /// explorationMapTextByLocationId is given the location's id and the
    /// column its map's opening brace sits at, and returns the map's JSON
    /// text (or null for a location with no map). Callers editing a map
    /// render it through RenderExplorationMap at that column; callers leaving
    /// maps alone return the original bytes verbatim.
    internal static string RenderLocations(
        IReadOnlyList<ScenarioLocationDefinitionV1> locations,
        Func<string, int, string?> explorationMapTextByLocationId)
    {
        return RenderTopLevelArray(
            locations,
            (location, column) => RenderLocation(location, column, explorationMapTextByLocationId));
    }

    internal static string RenderRoutes(IReadOnlyList<TravelRouteDefinitionV1> routes)
    {
        return RenderTopLevelArray(routes, RenderRoute);
    }

    internal static string RenderShops(IReadOnlyList<ShopDefinitionV1> shops)
    {
        return RenderTopLevelArray(shops, RenderShop);
    }

    // ----- ScenarioLocationDefinitionV1 -----

    private static string RenderLocation(
        ScenarioLocationDefinitionV1 location,
        int column,
        Func<string, int, string?> explorationMapTextByLocationId)
    {
        List<(string Key, string? Value)> fields =
        [
            ("LocationId", JsonString(location.LocationId)),
            ("DisplayName", JsonString(location.DisplayName)),
            ("ExplorationMap", explorationMapTextByLocationId(location.LocationId, column + 4)),
            ("ExplorableProgressIds", RenderProgressIdList(location.ExplorableProgressIds, column + 8))
        ];

        return RenderExplodedObject(fields, column);
    }

    // ----- TravelRouteDefinitionV1 -----

    private static string RenderRoute(
        TravelRouteDefinitionV1 route,
        int column)
    {
        List<(string Key, string? Value)> fields =
        [
            ("RouteId", JsonString(route.RouteId)),
            ("OriginLocationId", JsonString(route.OriginLocationId)),
            ("DestinationLocationId", JsonString(route.DestinationLocationId)),
            ("FinalStepIndex", route.FinalStepIndex.ToString(CultureInfo.InvariantCulture)),
            ("RequiredProgressIds", RenderProgressIdList(route.RequiredProgressIds, column + 8))
        ];

        return RenderExplodedObject(fields, column);
    }

    // ----- ShopDefinitionV1 -----

    private static string RenderShop(
        ShopDefinitionV1 shop,
        int column)
    {
        List<(string Key, string? Value)> fields =
        [
            ("ShopId", JsonString(shop.ShopId)),
            ("DisplayName", JsonString(shop.DisplayName)),
            ("LocationId", JsonString(shop.LocationId)),
            ("Items", RenderCompactArrayRequired(shop.Items, column + 8, RenderShopItem))
        ];

        return RenderExplodedObject(fields, column);
    }

    private static string RenderShopItem(ShopItemDefinitionV1 item)
    {
        List<(string Key, string Value)> fields =
        [
            ("ItemId", JsonString(item.ItemId)),
            ("PriceGoldPieces", item.PriceGoldPieces.ToString(CultureInfo.InvariantCulture))
        ];

        return RenderCompactObject(fields);
    }

    // ----- ExplorationMapDefinitionV1 -----

    internal static string RenderExplorationMap(
        ExplorationMapDefinitionV1 map,
        int column)
    {
        List<(string Key, string? Value)> fields =
        [
            ("MapId", JsonString(map.MapId)),
            ("Width", map.Width.ToString(CultureInfo.InvariantCulture)),
            ("Height", map.Height.ToString(CultureInfo.InvariantCulture)),
            ("StartingFloor", JsonString(map.StartingFloor)),
            ("StartingPosition", RenderGridPosition(map.StartingPosition)),
            ("StartingFacing", JsonString(map.StartingFacing.ToString())),
            ("Floors", RenderAlwaysExplodedArrayRequired(map.Floors, column + 8, RenderFloor))
        ];

        return RenderExplodedObject(fields, column);
    }

    private static string RenderFloor(
        ExplorationFloorDefinitionV1 floor,
        int column)
    {
        List<(string Key, string? Value)> fields =
        [
            ("Floor", JsonString(floor.Floor)),
            ("TraversablePositions", RenderAlwaysExplodedArrayRequired(
                floor.TraversablePositions,
                column + 8,
                (position, _) => RenderGridPosition(position))),
            ("Stairs", RenderAlwaysExplodedArrayRequired(floor.Stairs, column + 8, RenderStair)),
            ("Doors", RenderAlwaysExplodedArray(floor.Doors, column + 8, RenderDoor)),
            ("Treasures", RenderAlwaysExplodedArray(floor.Treasures, column + 8, RenderTreasure)),
            ("Npcs", RenderAlwaysExplodedArray(floor.Npcs, column + 8, RenderNpc))
        ];

        return RenderExplodedObject(fields, column);
    }

    private static string RenderStair(
        StairDefinitionV1 stair,
        int column)
    {
        List<(string Key, string? Value)> fields =
        [
            ("Position", RenderGridPosition(stair.Position)),
            ("DestinationFloor", JsonString(stair.DestinationFloor)),
            ("DestinationPosition", RenderGridPosition(stair.DestinationPosition))
        ];

        return RenderExplodedObject(fields, column);
    }

    private static string RenderDoor(
        DoorDefinitionV1 door,
        int column)
    {
        List<(string Key, string? Value)> fields =
        [
            ("DoorId", JsonString(door.DoorId)),
            ("Position", RenderGridPosition(door.Position)),
            ("Side", JsonString(door.Side.ToString())),
            ("IsSecret", RenderBool(door.IsSecret)),
            ("IsLocked", RenderBool(door.IsLocked))
        ];

        return RenderExplodedObject(fields, column);
    }

    /// GoldPieces deliberately precedes ItemId here, matching committed
    /// content (Watchtower's armory cache) rather than TreasureDefinitionV1's
    /// own property order, which declares ItemId first.
    private static string RenderTreasure(
        TreasureDefinitionV1 treasure,
        int column)
    {
        List<(string Key, string? Value)> fields =
        [
            ("TreasureId", JsonString(treasure.TreasureId)),
            ("Position", RenderGridPosition(treasure.Position)),
            ("GoldPieces", RenderNullableInt(treasure.GoldPieces)),
            ("ItemId", treasure.ItemId is { } itemId ? JsonString(itemId) : null),
            ("Quantity", RenderNullableInt(treasure.Quantity))
        ];

        return RenderExplodedObject(fields, column);
    }

    private static string RenderNpc(
        NpcDefinitionV1 npc,
        int column)
    {
        List<(string Key, string? Value)> fields =
        [
            ("NpcId", JsonString(npc.NpcId)),
            ("Position", RenderGridPosition(npc.Position)),
            ("Name", JsonString(npc.Name)),
            ("DialogueText", JsonString(npc.DialogueText))
        ];

        return RenderExplodedObject(fields, column);
    }

    private static string RenderGridPosition(GridPositionV1 position)
    {
        List<(string Key, string Value)> fields =
        [
            ("X", position.X.ToString(CultureInfo.InvariantCulture)),
            ("Y", position.Y.ToString(CultureInfo.InvariantCulture))
        ];

        return RenderCompactObject(fields);
    }

    private static string RenderBool(bool value)
    {
        return value ? "true" : "false";
    }

    // ----- Scenario-only convention: progress-id string lists -----

    /// ExplorableProgressIds/RequiredProgressIds stay on one line for one or
    /// two entries and only explode to one entry per line at three or more
    /// -- confirmed against real content (Watchtower's two-entry
    /// ExplorableProgressIds and Hollow Mill's two-entry Trigger's
    /// RequiredProgressIds both stay inline; every three-or-more-entry list
    /// in committed content explodes). This is deliberately not the same
    /// threshold RenderCompactArray uses for ruleset content (which
    /// explodes at two or more) -- the two files were authored under
    /// different conventions and this class matches scenario content's own.
    private static string? RenderProgressIdList(
        IReadOnlyList<string> progressIds,
        int column)
    {
        if (progressIds.Count == 0)
        {
            return null;
        }

        if (progressIds.Count <= 2)
        {
            string inline = string.Join(", ", progressIds.Select(JsonString));
            return $"[ {inline} ]";
        }

        StringBuilder sb = new();
        sb.Append('[').Append('\n');

        for (int i = 0; i < progressIds.Count; i++)
        {
            sb.Append(Spaces(column)).Append(JsonString(progressIds[i]));
            sb.Append(i < progressIds.Count - 1 ? ",\n" : "\n");
        }

        sb.Append(Spaces(column - 4)).Append(']');
        return sb.ToString();
    }
}

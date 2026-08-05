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
/// This class deliberately does NOT render ExplorationMap. That field isn't
/// editable through this editor yet, and real committed content orders a
/// floor's Doors/Treasures/Npcs inconsistently from file to file (Hollow
/// Mill writes Npcs first, Watchtower writes Doors/Treasures first), so no
/// fixed field order reproduces every file's ExplorationMap byte-for-byte.
/// RenderLocation instead takes the exact original bytes for each
/// location's map (from ScenarioPackDocument.FindRawExplorationMapText) and
/// copies them through verbatim -- true byte preservation instead of an
/// approximation that would need to reverse-engineer that ordering.
internal static class ScenarioJsonFormatting
{
    // ----- Top-level arrays -----

    internal static string RenderLocations(
        IReadOnlyList<ScenarioLocationDefinitionV1> locations,
        Func<string, string?> rawExplorationMapTextByLocationId)
    {
        return RenderTopLevelArray(
            locations,
            (location, column) => RenderLocation(location, column, rawExplorationMapTextByLocationId));
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
        Func<string, string?> rawExplorationMapTextByLocationId)
    {
        List<(string Key, string? Value)> fields =
        [
            ("LocationId", JsonString(location.LocationId)),
            ("DisplayName", JsonString(location.DisplayName)),
            ("ExplorationMap", rawExplorationMapTextByLocationId(location.LocationId)),
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

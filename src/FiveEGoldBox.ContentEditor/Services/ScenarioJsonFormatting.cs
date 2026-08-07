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
/// ScenarioPackDocument.FindRawExplorationMapText); an edited one goes
/// through RenderExplorationMap, which writes one canonical field order --
/// the DTO's own.
///
/// Committed content used to disagree on that order (Hollow Mill wrote a
/// floor's Npcs before its Doors, Watchtower the reverse), which meant a
/// no-op save was byte-identical for one file and a large spurious block
/// move for the other. Rather than carry each floor's original key order
/// through the form round trip purely to preserve an inconsistency, the
/// committed files were normalized to this renderer's own order once, via
/// CommittedScenarioMapNormalizer. A no-op save is now byte-identical for
/// every scenario, which ScenarioNoOpSaveFormattingTests asserts for all
/// three -- so this comment is load-bearing: reintroducing a hand-authored
/// ordering that disagrees with the DTO's will fail that test rather than
/// silently produce noisy diffs.
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

    internal static string RenderTriggers(IReadOnlyList<ScenarioTriggerDefinitionV1> triggers)
    {
        return RenderTopLevelArray(triggers, RenderTrigger);
    }

    internal static string RenderDecisions(IReadOnlyList<ScenarioDecisionDefinitionV1> decisions)
    {
        return RenderTopLevelArray(decisions, RenderDecision);
    }

    internal static string RenderEncounters(IReadOnlyList<EncounterDefinitionV1> encounters)
    {
        return RenderTopLevelArray(encounters, RenderEncounter);
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

    // ----- EncounterDefinitionV1 -----

    /// BlockedPositions is required-and-always-rendered (every committed
    /// encounter carries it, all of them empty today, written inline as []),
    /// which is the same shape a floor's Stairs already had. Note the
    /// non-empty case has no committed example to check byte-for-byte
    /// against, so it follows PartyStartingPositions' proven layout rather
    /// than a guess of its own.
    private static string RenderEncounter(
        EncounterDefinitionV1 encounter,
        int column)
    {
        List<(string Key, string? Value)> fields =
        [
            ("EncounterId", JsonString(encounter.EncounterId)),
            ("BattlefieldId", JsonString(encounter.BattlefieldId)),
            ("Width", encounter.Width.ToString(CultureInfo.InvariantCulture)),
            ("Height", encounter.Height.ToString(CultureInfo.InvariantCulture)),
            ("PartySideId", JsonString(encounter.PartySideId)),
            ("BlockedPositions", RenderCompactArrayRequired(
                encounter.BlockedPositions,
                column + 8,
                RenderGridPosition)),
            ("PartyStartingPositions", RenderCompactArrayRequired(
                encounter.PartyStartingPositions,
                column + 8,
                RenderGridPosition)),
            ("Combatants", RenderAlwaysExplodedArrayRequired(
                encounter.Combatants,
                column + 8,
                RenderEncounterCombatant)),
            ("Outcome", RenderEncounterOutcome(encounter.Outcome, column + 4))
        ];

        return RenderExplodedObject(fields, column);
    }

    private static string RenderEncounterCombatant(
        EncounterCombatantDefinitionV1 combatant,
        int column)
    {
        List<(string Key, string? Value)> fields =
        [
            ("CombatantId", JsonString(combatant.CombatantId)),
            ("MonsterId", JsonString(combatant.MonsterId)),
            ("SideId", JsonString(combatant.SideId)),
            ("StartingPosition", RenderGridPosition(combatant.StartingPosition))
        ];

        return RenderExplodedObject(fields, column);
    }

    private static string RenderEncounterOutcome(
        EncounterOutcomeDefinitionV1 outcome,
        int column)
    {
        List<(string Key, string? Value)> fields =
        [
            ("VictoryProgressId", JsonString(outcome.VictoryProgressId)),
            ("DefeatProgressId", JsonString(outcome.DefeatProgressId))
        ];

        return RenderExplodedObject(fields, column);
    }

    // ----- ScenarioTriggerDefinitionV1 -----

    /// Floor, Position and EncounterId are each independently optional and
    /// are omitted entirely rather than written as null -- RenderExplodedObject
    /// drops a null-valued field, which is exactly the committed shape (Hollow
    /// Mill's non-combat triggers carry no EncounterId at all).
    private static string RenderTrigger(
        ScenarioTriggerDefinitionV1 trigger,
        int column)
    {
        List<(string Key, string? Value)> fields =
        [
            ("TriggerId", JsonString(trigger.TriggerId)),
            ("DisplayName", JsonString(trigger.DisplayName)),
            ("LocationId", JsonString(trigger.LocationId)),
            ("Floor", trigger.Floor is null ? null : JsonString(trigger.Floor)),
            ("Position", trigger.Position is null ? null : RenderGridPosition(trigger.Position)),
            ("RequiredProgressIds", RenderProgressIdList(trigger.RequiredProgressIds, column + 8)),
            ("ResultingProgressId", JsonString(trigger.ResultingProgressId)),
            ("EncounterId", trigger.EncounterId is null ? null : JsonString(trigger.EncounterId))
        ];

        return RenderExplodedObject(fields, column);
    }

    // ----- ScenarioDecisionDefinitionV1 -----

    private static string RenderDecision(
        ScenarioDecisionDefinitionV1 decision,
        int column)
    {
        List<(string Key, string? Value)> fields =
        [
            ("DecisionId", JsonString(decision.DecisionId)),
            ("DisplayName", JsonString(decision.DisplayName)),
            ("LocationId", JsonString(decision.LocationId)),
            ("RequiredProgressIds", RenderProgressIdList(decision.RequiredProgressIds, column + 8)),
            ("Options", RenderAlwaysExplodedArrayRequired(decision.Options, column + 8, RenderDecisionOption))
        ];

        return RenderExplodedObject(fields, column);
    }

    /// A declining option ("Not yet") legitimately advances no progress, so a
    /// null ResultingProgressId writes no property at all.
    private static string RenderDecisionOption(
        ScenarioDecisionOptionDefinitionV1 option,
        int column)
    {
        List<(string Key, string? Value)> fields =
        [
            ("OptionId", JsonString(option.OptionId)),
            ("DisplayName", JsonString(option.DisplayName)),
            ("ResultingProgressId", option.ResultingProgressId is null
                ? null
                : JsonString(option.ResultingProgressId))
        ];

        return RenderExplodedObject(fields, column);
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

using FiveEGoldBox.ContentEditor.Models;
using FiveEGoldBox.Application.Content.V1;
using FiveEGoldBox.ContentEditor.Services;

namespace FiveEGoldBox.ContentEditor.Tests;

/// Create/edit/delete coverage for Locations/Routes/Shops, plus the
/// atomic-write invariants (a validation failure leaves the real file
/// untouched) and the two behaviors specific to scenario content: an edit
/// to one location must never disturb a sibling location's ExplorationMap,
/// and saving the first-ever Shop in a file that has no "Shops" key yet
/// must insert the property rather than fail. Always runs against a temp
/// copy of a real committed scenario file, never the committed file itself.
public sealed class ScenarioContentServiceTests
{
    // ----- Locations -----

    [Fact]
    public void SaveLocation_AddsANewLocationWithNoExplorationMap()
    {
        WithTempScenarioFile("hollow-mill", (service, path) =>
        {
            ScenarioLocationDefinitionV1 location = new()
            {
                LocationId = "location.test-waystation",
                DisplayName = "Test Waystation"
            };

            var result = service.SaveLocation(path, location);

            Assert.True(result.IsValid, DescribeIssues(result));
            Assert.Equivalent(location, service.FindLocation(path, "location.test-waystation"));
        });
    }

    [Fact]
    public void SaveLocation_RenamingALocationLeavesASiblingLocationsExplorationMapUntouched()
    {
        WithTempScenarioFile("hollow-mill", (service, path) =>
        {
            byte[] beforeMap = ReadRawExplorationMapBytes(path, "location.hollow-mill-house");

            ScenarioLocationDefinitionV1 village = service.FindLocation(path, "location.hollow-mill-village")!;
            var result = service.SaveLocation(path, village with { DisplayName = "Renamed Village" });

            Assert.True(result.IsValid, DescribeIssues(result));
            Assert.Equal(
                "Renamed Village",
                service.FindLocation(path, "location.hollow-mill-village")!.DisplayName);

            byte[] afterMap = ReadRawExplorationMapBytes(path, "location.hollow-mill-house");
            Assert.Equal(beforeMap, afterMap);
        });
    }

    [Fact]
    public void DeleteLocation_RemovesAnExistingLocation()
    {
        WithTempScenarioFile("hollow-mill", (service, path) =>
        {
            ScenarioLocationDefinitionV1 extra = new()
            {
                LocationId = "location.test-unreferenced",
                DisplayName = "Test Unreferenced"
            };
            Assert.True(service.SaveLocation(path, extra).IsValid);

            var result = service.DeleteLocation(path, "location.test-unreferenced");

            Assert.True(result.IsValid, DescribeIssues(result));
            Assert.Null(service.FindLocation(path, "location.test-unreferenced"));
        });
    }

    [Fact]
    public void DeleteLocation_RejectsDeletingTheScenariosStartingLocationAndLeavesFileUntouched()
    {
        WithTempScenarioFile("hollow-mill", (service, path) =>
        {
            byte[] before = File.ReadAllBytes(path);

            // location.hollow-mill-village is scenario.hollow-mill's StartingLocationId.
            var result = service.DeleteLocation(path, "location.hollow-mill-village");

            Assert.False(result.IsValid);

            byte[] after = File.ReadAllBytes(path);
            Assert.Equal(before, after);
        });
    }

    // ----- Routes -----

    [Fact]
    public void SaveRoute_AddsANewRoute()
    {
        WithTempScenarioFile("hollow-mill", (service, path) =>
        {
            TravelRouteDefinitionV1 route = new()
            {
                RouteId = "route.test-shortcut",
                OriginLocationId = "location.hollow-mill-village",
                DestinationLocationId = "location.hollow-mill-house",
                FinalStepIndex = 1
            };

            var result = service.SaveRoute(path, route);

            Assert.True(result.IsValid, DescribeIssues(result));
            Assert.Equivalent(route, service.FindRoute(path, "route.test-shortcut"));
        });
    }

    [Fact]
    public void SaveRoute_RejectsAnUnknownOriginLocationAndLeavesFileUntouched()
    {
        WithTempScenarioFile("hollow-mill", (service, path) =>
        {
            byte[] before = File.ReadAllBytes(path);

            TravelRouteDefinitionV1 invalid = new()
            {
                RouteId = "route.test-invalid",
                OriginLocationId = "location.does-not-exist",
                DestinationLocationId = "location.hollow-mill-house",
                FinalStepIndex = 1
            };

            var result = service.SaveRoute(path, invalid);

            Assert.False(result.IsValid);

            byte[] after = File.ReadAllBytes(path);
            Assert.Equal(before, after);
        });
    }

    [Fact]
    public void SaveRoute_RejectsAnEmptyOriginLocationAndLeavesFileUntouched()
    {
        // Reproduces what an unfilled InputSelect submits: "" rather than
        // null, since the picker's blank "(choose a location)" option has
        // value="".
        WithTempScenarioFile("hollow-mill", (service, path) =>
        {
            byte[] before = File.ReadAllBytes(path);

            TravelRouteDefinitionV1 invalid = new()
            {
                RouteId = "route.test-invalid",
                OriginLocationId = "",
                DestinationLocationId = "",
                FinalStepIndex = 1
            };

            var result = service.SaveRoute(path, invalid);

            Assert.False(result.IsValid);

            byte[] after = File.ReadAllBytes(path);
            Assert.Equal(before, after);
        });
    }

    [Fact]
    public void DeleteRoute_RemovesAnExistingRoute()
    {
        WithTempScenarioFile("hollow-mill", (service, path) =>
        {
            var result = service.DeleteRoute(path, "route.village-to-mill.shortcut");

            Assert.True(result.IsValid, DescribeIssues(result));
            Assert.Null(service.FindRoute(path, "route.village-to-mill.shortcut"));
        });
    }

    // ----- Shops -----

    [Fact]
    public void SaveShop_ReplacesAnExistingShopWithTheSameId()
    {
        WithTempScenarioFile("hollow-mill", (service, path) =>
        {
            ShopDefinitionV1 shop = service.FindShop(path, "shop.hollow-mill-village.general-store")!;
            ShopDefinitionV1 updated = shop with
            {
                DisplayName = "The Renamed General Store"
            };

            var result = service.SaveShop(path, updated);

            Assert.True(result.IsValid, DescribeIssues(result));
            Assert.Equal(
                "The Renamed General Store",
                service.FindShop(path, "shop.hollow-mill-village.general-store")!.DisplayName);
        });
    }

    [Fact]
    public void SaveShop_InsertsTheShopsPropertyIntoAFileThatHasNoneYet()
    {
        // Watchtower has zero shops today -- no "Shops" key at all in the
        // committed file -- exercising ReplaceOrInsertRootPropertyValue's
        // insert path, not the plain replace path every other test here
        // covers.
        WithTempScenarioFile("watchtower", (service, path) =>
        {
            Assert.Empty(service.LoadShops(path));

            ShopDefinitionV1 shop = new()
            {
                ShopId = "shop.test-outpost-quartermaster",
                DisplayName = "Test Outpost Quartermaster",
                LocationId = "location.outpost",
                Items = [new ShopItemDefinitionV1 { ItemId = "item.torch", PriceGoldPieces = 1 }]
            };

            var result = service.SaveShop(path, shop);

            Assert.True(result.IsValid, DescribeIssues(result));
            Assert.Equivalent(shop, service.FindShop(path, "shop.test-outpost-quartermaster"));
        });
    }

    [Fact]
    public void SaveShop_RejectsAnUnknownLocationAndLeavesFileUntouched()
    {
        WithTempScenarioFile("hollow-mill", (service, path) =>
        {
            byte[] before = File.ReadAllBytes(path);

            ShopDefinitionV1 invalid = new()
            {
                ShopId = "shop.test-invalid",
                DisplayName = "Test Invalid",
                LocationId = "location.does-not-exist",
                Items = [new ShopItemDefinitionV1 { ItemId = "item.torch", PriceGoldPieces = 1 }]
            };

            var result = service.SaveShop(path, invalid);

            Assert.False(result.IsValid);

            byte[] after = File.ReadAllBytes(path);
            Assert.Equal(before, after);
        });
    }

    [Fact]
    public void DeleteShop_RemovesAnExistingShop()
    {
        WithTempScenarioFile("hollow-mill", (service, path) =>
        {
            var result = service.DeleteShop(path, "shop.hollow-mill-village.general-store");

            Assert.True(result.IsValid, DescribeIssues(result));
            Assert.Null(service.FindShop(path, "shop.hollow-mill-village.general-store"));
        });
    }

    // ----- Scenario listing -----

    [Fact]
    public void ListScenarios_FindsAllThreeRealScenarios()
    {
        ScenarioContentService service = new();

        var scenarios = service.ListScenarios();

        Assert.Contains(scenarios, s => s.ScenarioId == "scenario.watchtower");
        Assert.Contains(scenarios, s => s.ScenarioId == "scenario.sunken-chapel");
        Assert.Contains(scenarios, s => s.ScenarioId == "scenario.hollow-mill");
    }

    // ----- Helpers -----

    // ----- ExplorationMap -----

    [Fact]
    public void SaveExplorationMap_AddingATraversableCellPersistsIt()
    {
        WithTempScenarioFile("watchtower", (service, path) =>
        {
            string locationId = service.LoadLocations(path)
                .First(location => location.ExplorationMap is not null)
                .LocationId;

            ExplorationMapDefinitionV1 map = service.FindExplorationMap(path, locationId)!;
            ExplorationFloorDefinitionV1 groundFloor = map.Floors[0];

            // (0, 0) is in bounds on this 6x3 map and not already traversable.
            var widened = groundFloor with
            {
                TraversablePositions =
                [
                    .. groundFloor.TraversablePositions,
                    new GridPositionV1 { X = 0, Y = 0 }
                ]
            };

            var result = service.SaveExplorationMap(
                path,
                locationId,
                map with { Floors = [widened, .. map.Floors.Skip(1)] });

            Assert.True(result.IsValid, DescribeIssues(result));

            var reloaded = service.FindExplorationMap(path, locationId)!;
            Assert.Contains(
                reloaded.Floors[0].TraversablePositions,
                position => position is { X: 0, Y: 0 });
        });
    }

    [Fact]
    public void SaveExplorationMap_EditingOneLocationLeavesASiblingsMapByteUntouched()
    {
        WithTempScenarioFile("hollow-mill", (service, path) =>
        {
            var mapped = service.LoadLocations(path)
                .Where(location => location.ExplorationMap is not null)
                .ToList();

            // Hollow Mill has exactly one mapped location today, so the
            // sibling this guards is any *unmapped* location's absence of a
            // map -- re-rendering one map must not invent one elsewhere.
            string editedId = mapped[0].LocationId;
            var untouchedIds = service.LoadLocations(path)
                .Select(location => location.LocationId)
                .Where(id => id != editedId)
                .ToList();

            var before = untouchedIds
                .ToDictionary(id => id, id => ReadRawExplorationMapBytes(path, id));

            var map = service.FindExplorationMap(path, editedId)!;
            var result = service.SaveExplorationMap(path, editedId, map);
            Assert.True(result.IsValid, DescribeIssues(result));

            foreach (string id in untouchedIds)
            {
                Assert.Equal(before[id], ReadRawExplorationMapBytes(path, id));
            }
        });
    }

    [Fact]
    public void SaveExplorationMap_RejectingAnOutOfBoundsCellLeavesTheFileUntouched()
    {
        WithTempScenarioFile("watchtower", (service, path) =>
        {
            string locationId = service.LoadLocations(path)
                .First(location => location.ExplorationMap is not null)
                .LocationId;

            byte[] before = File.ReadAllBytes(path);
            ExplorationMapDefinitionV1 map = service.FindExplorationMap(path, locationId)!;
            ExplorationFloorDefinitionV1 groundFloor = map.Floors[0];

            var outOfBounds = groundFloor with
            {
                TraversablePositions =
                [
                    .. groundFloor.TraversablePositions,
                    new GridPositionV1 { X = map.Width + 5, Y = 0 }
                ]
            };

            var result = service.SaveExplorationMap(
                path,
                locationId,
                map with { Floors = [outOfBounds, .. map.Floors.Skip(1)] });

            Assert.False(result.IsValid);
            Assert.Contains(
                result.Issues,
                issue => issue.Code == "scenario.map.position_out_of_bounds");
            Assert.Equal(before, File.ReadAllBytes(path));
        });
    }

    [Fact]
    public void ToggleTraversable_RemovingACellLeavesTheRemainingCellsInTheirOriginalOrder()
    {
        WithTempScenarioFile("watchtower", (service, path) =>
        {
            string locationId = service.LoadLocations(path)
                .First(location => location.ExplorationMap is not null)
                .LocationId;

            var model = ExplorationMapFormModel.FromDefinition(
                service.FindExplorationMap(path, locationId)!);
            ExplorationFloorFormModel floor = model.Floors[0];

            var originalOrder = floor.TraversablePositions
                .Select(p => (p.X, p.Y))
                .ToList();
            var removed = originalOrder[3];

            floor.ToggleTraversable(removed.X, removed.Y);

            Assert.Equal(
                originalOrder.Where(cell => cell != removed),
                floor.TraversablePositions.Select(p => (p.X, p.Y)));
        });
    }

    [Fact]
    public void ToggleTraversable_AddingACellAppendsItRatherThanReorderingExistingCells()
    {
        WithTempScenarioFile("watchtower", (service, path) =>
        {
            string locationId = service.LoadLocations(path)
                .First(location => location.ExplorationMap is not null)
                .LocationId;

            var model = ExplorationMapFormModel.FromDefinition(
                service.FindExplorationMap(path, locationId)!);
            ExplorationFloorFormModel floor = model.Floors[0];

            var originalOrder = floor.TraversablePositions
                .Select(p => (p.X, p.Y))
                .ToList();

            // (0, 0) is in bounds on this 6x3 map and not already walkable.
            floor.ToggleTraversable(0, 0);

            Assert.Equal(
                originalOrder.Append((0, 0)),
                floor.TraversablePositions.Select(p => (p.X, p.Y)));
        });
    }

    [Fact]
    public void ToggleTraversable_LeavesStairsDoorsTreasuresAndNpcsIntact()
    {
        WithTempScenarioFile("watchtower", (service, path) =>
        {
            string locationId = service.LoadLocations(path)
                .First(location => location.ExplorationMap is not null)
                .LocationId;

            var before = service.FindExplorationMap(path, locationId)!;
            var model = ExplorationMapFormModel.FromDefinition(before);
            model.Floors[0].ToggleTraversable(0, 0);

            var result = service.SaveExplorationMap(path, locationId, model.ToDefinition());
            Assert.True(result.IsValid, DescribeIssues(result));

            var after = service.FindExplorationMap(path, locationId)!;

            Assert.Equal(
                before.Floors[0].Stairs.Select(s => (s.Position.X, s.Position.Y, s.DestinationFloor)),
                after.Floors[0].Stairs.Select(s => (s.Position.X, s.Position.Y, s.DestinationFloor)));
            Assert.Equal(
                before.Floors[0].Doors.Select(d => (d.DoorId, d.Side, d.IsSecret, d.IsLocked)),
                after.Floors[0].Doors.Select(d => (d.DoorId, d.Side, d.IsSecret, d.IsLocked)));
            Assert.Equal(
                before.Floors[0].Treasures.Select(t => (t.TreasureId, t.GoldPieces, t.ItemId, t.Quantity)),
                after.Floors[0].Treasures.Select(t => (t.TreasureId, t.GoldPieces, t.ItemId, t.Quantity)));
            Assert.Equal(
                before.Floors[0].Npcs.Select(n => (n.NpcId, n.Name, n.DialogueText)),
                after.Floors[0].Npcs.Select(n => (n.NpcId, n.Name, n.DialogueText)));
        });
    }

    private static void WithTempScenarioFile(
        string scenarioDirectoryName,
        Action<ScenarioContentService, string> action)
    {
        string realPath = RepositoryLocator.ResolveScenarioPackPaths()
            .Single(path => path.Contains(scenarioDirectoryName));
        string tempPath = ScenarioNoOpSaveFormattingTests.CopyToTempFile(realPath);

        try
        {
            action(new ScenarioContentService(), tempPath);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    private static byte[] ReadRawExplorationMapBytes(
        string scenarioFilePath,
        string locationId)
    {
        string? text = ScenarioPackDocument.Read(scenarioFilePath).FindRawExplorationMapText(locationId);
        return text is null ? [] : System.Text.Encoding.UTF8.GetBytes(text);
    }

    private static string DescribeIssues(
        FiveEGoldBox.Core.Validation.ValidationResult result)
    {
        return string.Join(
            "\n",
            result.Issues.Select(issue => $"{issue.Severity} {issue.Code}: {issue.Message}"));
    }
}

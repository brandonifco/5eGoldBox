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

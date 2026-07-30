using System.Text.Json;
using System.Text.Json.Nodes;

namespace FiveEGoldBox.Console.Tests;

public sealed class TiledMapImportCommandTests
{
    [Fact]
    public void Run_WithTooFewArguments_PrintsUsageAndReturnsTwo()
    {
        StringWriter output = new();
        StringWriter error = new();

        int exitCode = TiledMapImportCommand.Run(
            ["a", "b", "c"],
            output,
            error);

        Assert.Equal(2, exitCode);
        Assert.Contains("Usage:", error.ToString());
        Assert.Empty(output.ToString());
    }

    [Fact]
    public void Run_WithTooManyArguments_PrintsUsageAndReturnsTwo()
    {
        StringWriter output = new();
        StringWriter error = new();

        int exitCode = TiledMapImportCommand.Run(
            ["a", "b", "c", "d", "e", "f"],
            output,
            error);

        Assert.Equal(2, exitCode);
        Assert.Contains("Usage:", error.ToString());
    }

    [Fact]
    public void Run_WithMissingTiledFile_ReportsIssueAndReturnsOne()
    {
        (string workingDirectory, string scenarioPath) = CreateFixtureScenario(
            withExistingDungeonLocation: false);

        try
        {
            StringWriter output = new();
            StringWriter error = new();

            int exitCode = TiledMapImportCommand.Run(
                [Path.Combine(workingDirectory, "missing.tmj"), scenarioPath, "location.dungeon", "map.dungeon", "The Dungeon"],
                output,
                error);

            Assert.Equal(1, exitCode);
            Assert.Contains("content.tiled_import.tiled_file_not_found", output.ToString());
        }
        finally
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }

    [Fact]
    public void Run_WithNoStartObject_ReportsIssueAndDoesNotWriteScenario()
    {
        (string workingDirectory, string scenarioPath) = CreateFixtureScenario(
            withExistingDungeonLocation: false);
        string beforeContents = File.ReadAllText(scenarioPath);

        try
        {
            string tiledPath = WriteTiledFile(
                workingDirectory,
                BuildTiledMap(includeStart: false));
            StringWriter output = new();
            StringWriter error = new();

            int exitCode = TiledMapImportCommand.Run(
                [tiledPath, scenarioPath, "location.dungeon", "map.dungeon", "The Dungeon"],
                output,
                error);

            Assert.Equal(1, exitCode);
            Assert.Contains("content.tiled_import.no_start_object", output.ToString());
            Assert.Equal(beforeContents, File.ReadAllText(scenarioPath));
        }
        finally
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }

    [Fact]
    public void Run_CreatingANewLocationWithoutADisplayName_ReportsIssueAndReturnsOne()
    {
        (string workingDirectory, string scenarioPath) = CreateFixtureScenario(
            withExistingDungeonLocation: false);

        try
        {
            string tiledPath = WriteTiledFile(
                workingDirectory,
                BuildTiledMap(includeStart: true));
            StringWriter output = new();
            StringWriter error = new();

            int exitCode = TiledMapImportCommand.Run(
                [tiledPath, scenarioPath, "location.dungeon", "map.dungeon"],
                output,
                error);

            Assert.Equal(1, exitCode);
            Assert.Contains("content.tiled_import.display_name_required", output.ToString());
        }
        finally
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }

    [Fact]
    public void Run_WithAValidTwoFloorMap_CreatesTheLocationAndReportsValid()
    {
        (string workingDirectory, string scenarioPath) = CreateFixtureScenario(
            withExistingDungeonLocation: false);

        try
        {
            string tiledPath = WriteTiledFile(
                workingDirectory,
                BuildTiledMap(includeStart: true));
            StringWriter output = new();
            StringWriter error = new();

            int exitCode = TiledMapImportCommand.Run(
                [tiledPath, scenarioPath, "location.dungeon", "map.dungeon", "The Dungeon"],
                output,
                error);

            Assert.Equal(0, exitCode);
            Assert.Contains("Valid: no issues found.", output.ToString());
            Assert.Empty(error.ToString());

            JsonObject scenario = JsonNode.Parse(File.ReadAllText(scenarioPath))!.AsObject();
            JsonObject location = scenario["Locations"]!.AsArray()
                .OfType<JsonObject>()
                .Single(candidate => candidate["LocationId"]!.GetValue<string>() == "location.dungeon");

            Assert.Equal("The Dungeon", location["DisplayName"]!.GetValue<string>());

            JsonObject map = location["ExplorationMap"]!.AsObject();
            Assert.Equal("map.dungeon", map["MapId"]!.GetValue<string>());
            Assert.Equal(3, map["Width"]!.GetValue<int>());
            Assert.Equal(3, map["Height"]!.GetValue<int>());
            Assert.Equal("GroundFloor", map["StartingFloor"]!.GetValue<string>());
            Assert.Equal("South", map["StartingFacing"]!.GetValue<string>());
            Assert.Equal(2, map["Floors"]!.AsArray().Count);

            JsonObject groundFloor = map["Floors"]!.AsArray()
                .OfType<JsonObject>()
                .Single(floor => floor["Floor"]!.GetValue<string>() == "GroundFloor");
            Assert.Equal(9, groundFloor["TraversablePositions"]!.AsArray().Count);
            Assert.Single(groundFloor["Stairs"]!.AsArray());

            JsonObject stair = (JsonObject)groundFloor["Stairs"]!.AsArray()[0]!;
            Assert.Equal("UpperFloor", stair["DestinationFloor"]!.GetValue<string>());

            JsonArray triggers = scenario["Triggers"]!.AsArray();
            JsonObject trigger = triggers.OfType<JsonObject>()
                .Single(candidate => candidate["TriggerId"]!.GetValue<string>() == "trigger.test");
            Assert.Equal("location.dungeon", trigger["LocationId"]!.GetValue<string>());
            Assert.Equal("UpperFloor", trigger["Floor"]!.GetValue<string>());
            Assert.Equal("progress.two", trigger["ResultingProgressId"]!.GetValue<string>());
            Assert.Equal(
                ["progress.one"],
                trigger["RequiredProgressIds"]!.AsArray().Select(node => node!.GetValue<string>()));
        }
        finally
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }

    [Fact]
    public void Run_ReimportingIntoAnExistingLocation_ReplacesTheMapAndTileAnchoredTriggersOnly()
    {
        (string workingDirectory, string scenarioPath) = CreateFixtureScenario(
            withExistingDungeonLocation: true);

        try
        {
            string tiledPath = WriteTiledFile(
                workingDirectory,
                BuildTiledMap(includeStart: true));
            StringWriter firstOutput = new();

            int firstExitCode = TiledMapImportCommand.Run(
                [tiledPath, scenarioPath, "location.dungeon", "map.dungeon"],
                firstOutput,
                new StringWriter());

            Assert.Equal(0, firstExitCode);

            StringWriter secondOutput = new();
            int secondExitCode = TiledMapImportCommand.Run(
                [tiledPath, scenarioPath, "location.dungeon", "map.dungeon"],
                secondOutput,
                new StringWriter());

            Assert.Equal(0, secondExitCode);

            JsonObject scenario = JsonNode.Parse(File.ReadAllText(scenarioPath))!.AsObject();
            JsonArray triggers = scenario["Triggers"]!.AsArray();

            // The one tile-anchored trigger this map produces should appear
            // exactly once even after a second import, and the pre-existing
            // non-spatial trigger for the same location should survive both.
            Assert.Single(
                triggers.OfType<JsonObject>(),
                candidate => candidate["TriggerId"]!.GetValue<string>() == "trigger.test");
            Assert.Single(
                triggers.OfType<JsonObject>(),
                candidate => candidate["TriggerId"]!.GetValue<string>() == "trigger.non-spatial");
        }
        finally
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }

    private static string WriteTiledFile(string workingDirectory, string json)
    {
        string path = Path.Combine(workingDirectory, "map.tmj");
        File.WriteAllText(path, json);
        return path;
    }

    private static (string WorkingDirectory, string ScenarioPath) CreateFixtureScenario(
        bool withExistingDungeonLocation)
    {
        string workingDirectory = Directory.CreateTempSubdirectory(
            "FiveEGoldBox.Console.TiledImportTests.").FullName;
        string scenarioPath = Path.Combine(workingDirectory, "scenario.json");

        List<object> locations =
        [
            new { LocationId = "location.hub", DisplayName = "Hub" }
        ];

        List<object> triggers = [];

        if (withExistingDungeonLocation)
        {
            locations.Add(new { LocationId = "location.dungeon", DisplayName = "The Dungeon" });
            triggers.Add(new
            {
                TriggerId = "trigger.non-spatial",
                DisplayName = "A story beat",
                LocationId = "location.dungeon",
                RequiredProgressIds = new[] { "progress.one" },
                ResultingProgressId = "progress.two"
            });
        }

        object scenario = new
        {
            FormatVersion = 1,
            ScenarioId = "scenario.test",
            DisplayName = "Test Scenario",
            RulesetId = "ruleset.campaign",
            StartingLocationId = "location.hub",
            Progress = new
            {
                InitialProgressId = "progress.one",
                ProgressIds = new[] { "progress.one", "progress.two" },
                Conclusions = new[]
                {
                    new { ProgressId = "progress.two", IsSuccess = true, LocationId = "location.hub" }
                }
            },
            PartyRequirement = new
            {
                MinimumMembers = 1,
                MaximumMembers = 4,
                MinimumConsciousMembers = 1
            },
            Locations = locations,
            Routes = Array.Empty<object>(),
            Encounters = Array.Empty<object>(),
            Triggers = triggers,
            Decisions = Array.Empty<object>()
        };

        File.WriteAllText(
            scenarioPath,
            JsonSerializer.Serialize(scenario, new JsonSerializerOptions { WriteIndented = true }));

        return (workingDirectory, scenarioPath);
    }

    /// A 3x3, two-floor map: both floors fully walkable, a stair from
    /// GroundFloor(1,2) to UpperFloor(0,0), a Start object on GroundFloor
    /// facing South, and one Trigger object on UpperFloor.
    private static string BuildTiledMap(bool includeStart)
    {
        int[] fullFloor = Enumerable.Repeat(1, 9).ToArray();

        List<object> objects =
        [
            new
            {
                name = "stair-up",
                type = "Stair",
                x = 32,
                y = 64,
                properties = new object[]
                {
                    new { name = "Floor", type = "string", value = "GroundFloor" },
                    new { name = "DestinationFloor", type = "string", value = "UpperFloor" },
                    new { name = "DestinationX", type = "int", value = 0 },
                    new { name = "DestinationY", type = "int", value = 0 }
                }
            },
            new
            {
                name = "test-trigger",
                type = "Trigger",
                x = 64,
                y = 0,
                properties = new object[]
                {
                    new { name = "Floor", type = "string", value = "UpperFloor" },
                    new { name = "TriggerId", type = "string", value = "trigger.test" },
                    new { name = "ResultingProgressId", type = "string", value = "progress.two" },
                    new { name = "RequiredProgressIds", type = "string", value = "progress.one" }
                }
            }
        ];

        if (includeStart)
        {
            objects.Add(new
            {
                name = "party-start",
                type = "Start",
                x = 32,
                y = 0,
                properties = new object[]
                {
                    new { name = "Floor", type = "string", value = "GroundFloor" },
                    new { name = "Facing", type = "string", value = "South" }
                }
            });
        }

        object document = new
        {
            width = 3,
            height = 3,
            tilewidth = 32,
            tileheight = 32,
            layers = new object[]
            {
                new
                {
                    type = "tilelayer",
                    name = "GroundFloor",
                    data = fullFloor,
                    properties = new object[]
                    {
                        new { name = "IsFloor", type = "bool", value = true }
                    }
                },
                new
                {
                    type = "tilelayer",
                    name = "UpperFloor",
                    data = fullFloor,
                    properties = new object[]
                    {
                        new { name = "IsFloor", type = "bool", value = true }
                    }
                },
                new
                {
                    type = "objectgroup",
                    name = "Objects",
                    objects
                }
            }
        };

        return JsonSerializer.Serialize(document);
    }
}

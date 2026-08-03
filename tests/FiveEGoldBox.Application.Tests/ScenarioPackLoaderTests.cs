using FiveEGoldBox.Application.Content;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class ScenarioPackLoaderTests
{
    private const string FullScenarioPack = """
        {
            "FormatVersion": 1,
            "ScenarioId": "scenario.test",
            "DisplayName": "Test Scenario",
            "RulesetId": "ruleset.test",
            "StartingLocationId": "location.hub",
            "Progress": {
                "InitialProgressId": "test.not_started",
                "ProgressIds": [ "test.not_started", "test.started", "test.won", "test.lost" ],
                "Conclusions": [
                    { "ProgressId": "test.won", "IsSuccess": true, "LocationId": "location.hub", "EpilogueText": "Won." },
                    { "ProgressId": "test.lost", "IsSuccess": false, "LocationId": "location.hub", "EpilogueText": "Lost." }
                ]
            },
            "PartyRequirement": {
                "MinimumMembers": 1,
                "MaximumMembers": 4,
                "MinimumConsciousMembers": 1
            },
            "Locations": [
                {
                    "LocationId": "location.hub",
                    "DisplayName": "Hub"
                },
                {
                    "LocationId": "location.dungeon",
                    "DisplayName": "Dungeon",
                    "ExplorationMap": {
                        "MapId": "map.dungeon",
                        "Width": 10,
                        "Height": 10,
                        "StartingFloor": "GroundFloor",
                        "StartingPosition": { "X": 1, "Y": 1 },
                        "StartingFacing": "North",
                        "Floors": [
                            {
                                "Floor": "GroundFloor",
                                "TraversablePositions": [
                                    { "X": 1, "Y": 1 },
                                    { "X": 1, "Y": 2 }
                                ],
                                "Stairs": [
                                    {
                                        "Position": { "X": 1, "Y": 2 },
                                        "DestinationFloor": "UpperFloor",
                                        "DestinationPosition": { "X": 2, "Y": 2 }
                                    }
                                ]
                            },
                            {
                                "Floor": "UpperFloor",
                                "TraversablePositions": [
                                    { "X": 2, "Y": 2 }
                                ],
                                "Stairs": []
                            }
                        ]
                    },
                    "ExplorableProgressIds": [ "test.started" ]
                }
            ],
            "Routes": [
                {
                    "RouteId": "route.test",
                    "OriginLocationId": "location.hub",
                    "DestinationLocationId": "location.dungeon",
                    "FinalStepIndex": 2,
                    "RequiredProgressIds": [ "test.started" ]
                }
            ],
            "Encounters": [
                {
                    "EncounterId": "encounter.test",
                    "BattlefieldId": "battlefield.test",
                    "Width": 8,
                    "Height": 8,
                    "PartySideId": "side.party",
                    "BlockedPositions": [ { "X": 4, "Y": 4 } ],
                    "PartyStartingPositions": [ { "X": 0, "Y": 0 } ],
                    "Combatants": [
                        {
                            "CombatantId": "combatant.foe",
                            "MonsterId": "monster.foe",
                            "SideId": "side.enemy",
                            "StartingPosition": { "X": 3, "Y": 3 }
                        }
                    ],
                    "Outcome": {
                        "VictoryProgressId": "test.won",
                        "DefeatProgressId": "test.lost"
                    }
                }
            ],
            "Triggers": [
                {
                    "TriggerId": "trigger.plain",
                    "DisplayName": "Plain Trigger",
                    "LocationId": "location.hub",
                    "RequiredProgressIds": [ "test.not_started" ],
                    "ResultingProgressId": "test.started"
                },
                {
                    "TriggerId": "trigger.positional",
                    "DisplayName": "Positional Trigger",
                    "LocationId": "location.dungeon",
                    "Floor": "UpperFloor",
                    "Position": { "X": 2, "Y": 2 },
                    "RequiredFacing": "East",
                    "RequiredProgressIds": [ "test.started" ],
                    "ResultingProgressId": "test.won",
                    "EncounterId": "encounter.test"
                }
            ],
            "Decisions": [
                {
                    "DecisionId": "decision.test",
                    "DisplayName": "Test Decision",
                    "LocationId": "location.hub",
                    "RequiredProgressIds": [ "test.not_started" ],
                    "Options": [
                        {
                            "OptionId": "option.accept",
                            "DisplayName": "Accept",
                            "ResultingProgressId": "test.started"
                        },
                        {
                            "OptionId": "option.decline",
                            "DisplayName": "Decline"
                        }
                    ]
                }
            ]
        }
        """;

    [Fact]
    public void Parse_FullScenarioPack_MapsEveryField()
    {
        ScenarioDefinition scenario = ScenarioPackLoader.Parse(
            FullScenarioPack);

        Assert.Equal("scenario.test", scenario.ScenarioId);
        Assert.Equal("Test Scenario", scenario.DisplayName);
        Assert.Equal("ruleset.test", scenario.RulesetId);
        Assert.Equal("location.hub", scenario.StartingLocationId);

        Assert.Equal("test.not_started", scenario.Progress.InitialProgressId);
        Assert.Equal(4, scenario.Progress.ProgressIds.Count);
        Assert.Equal(2, scenario.Progress.Conclusions.Count);
        Assert.True(scenario.Progress.Conclusions
            .Single(conclusion => conclusion.ProgressId == "test.won")
            .IsSuccess);

        Assert.Equal(1, scenario.PartyRequirement.MinimumMembers);
        Assert.Equal(4, scenario.PartyRequirement.MaximumMembers);

        ScenarioLocationDefinition hub = scenario.Locations
            .Single(location => location.LocationId == "location.hub");
        Assert.Null(hub.ExplorationMap);

        ScenarioLocationDefinition dungeon = scenario.Locations
            .Single(location => location.LocationId == "location.dungeon");
        Assert.NotNull(dungeon.ExplorationMap);
        Assert.Equal("GroundFloor", dungeon.ExplorationMap.StartingFloor);
        Assert.Equal(new GridPosition(1, 1), dungeon.ExplorationMap.StartingPosition);
        Assert.Equal(ExplorationFacing.North, dungeon.ExplorationMap.StartingFacing);
        Assert.Equal(2, dungeon.ExplorationMap.Floors.Count);

        ExplorationFloorDefinition groundFloor = dungeon.ExplorationMap.Floors
            .Single(floor => floor.Floor == "GroundFloor");
        Assert.Equal(2, groundFloor.TraversablePositions.Count);
        StairDefinition stair = Assert.Single(groundFloor.Stairs);
        Assert.Equal("UpperFloor", stair.DestinationFloor);
        Assert.Equal(new GridPosition(2, 2), stair.DestinationPosition);

        TravelRouteDefinition route = Assert.Single(scenario.Routes);
        Assert.Equal("location.hub", route.OriginLocationId);
        Assert.Equal("location.dungeon", route.DestinationLocationId);
        Assert.Equal(2, route.FinalStepIndex);

        EncounterDefinition encounter = Assert.Single(scenario.Encounters);
        Assert.Equal("side.party", encounter.PartySideId);
        Assert.Equal(new GridPosition(4, 4), Assert.Single(encounter.BlockedPositions));

        EncounterCombatantDefinition foe = Assert.Single(encounter.Combatants);
        Assert.Equal("monster.foe", foe.MonsterId);
        Assert.Equal("side.enemy", foe.SideId);
        Assert.Equal(new GridPosition(3, 3), foe.StartingPosition);
        Assert.Equal("test.won", encounter.Outcome.VictoryProgressId);
        Assert.Equal("test.lost", encounter.Outcome.DefeatProgressId);

        Assert.Equal(2, scenario.Triggers.Count);
        ScenarioTriggerDefinition positionalTrigger = scenario.Triggers
            .Single(trigger => trigger.TriggerId == "trigger.positional");
        Assert.Equal("UpperFloor", positionalTrigger.Floor);
        Assert.Equal(new GridPosition(2, 2), positionalTrigger.Position);
        Assert.Equal(ExplorationFacing.East, positionalTrigger.RequiredFacing);
        Assert.Equal("encounter.test", positionalTrigger.EncounterId);

        ScenarioTriggerDefinition plainTrigger = scenario.Triggers
            .Single(trigger => trigger.TriggerId == "trigger.plain");
        Assert.Null(plainTrigger.Floor);
        Assert.Null(plainTrigger.Position);
        Assert.Null(plainTrigger.RequiredFacing);
        Assert.Null(plainTrigger.EncounterId);

        ScenarioDecisionDefinition decision = Assert.Single(scenario.Decisions);
        Assert.Equal(2, decision.Options.Count);
        Assert.Equal(
            "test.started",
            decision.Options
                .Single(option => option.OptionId == "option.accept")
                .ResultingProgressId);
        Assert.Null(
            decision.Options
                .Single(option => option.OptionId == "option.decline")
                .ResultingProgressId);
    }

    [Fact]
    public void Parse_WithUnsupportedFormatVersion_Throws()
    {
        const string futureVersionPack = """
            {
                "FormatVersion": 2,
                "ScenarioId": "scenario.test",
                "DisplayName": "Test",
                "RulesetId": "ruleset.test",
                "StartingLocationId": "location.hub",
                "Progress": {
                    "InitialProgressId": "test.not_started",
                    "ProgressIds": [ "test.not_started" ],
                    "Conclusions": []
                },
                "PartyRequirement": {
                    "MinimumMembers": 1,
                    "MaximumMembers": 4,
                    "MinimumConsciousMembers": 1
                },
                "Locations": [],
                "Routes": [],
                "Encounters": [],
                "Triggers": [],
                "Decisions": []
            }
            """;

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => ScenarioPackLoader.Parse(futureVersionPack));

        Assert.Contains("format version", exception.Message);
    }

    [Fact]
    public void Load_ReadsFileAndParses()
    {
        string path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, FullScenarioPack);

            ScenarioDefinition scenario = ScenarioPackLoader.Load(path);

            Assert.Equal("scenario.test", scenario.ScenarioId);
        }
        finally
        {
            File.Delete(path);
        }
    }
}

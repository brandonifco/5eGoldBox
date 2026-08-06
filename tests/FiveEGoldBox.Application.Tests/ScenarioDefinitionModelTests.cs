using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

/// Proves the definition model can express a real scenario by building
/// Watchtower with it and checking the result against the constants the running
/// implementation actually uses. If the model cannot hold something Watchtower
/// needs, this file stops compiling or stops matching.
///
/// Nothing here is wired into execution yet — moving Watchtower onto these
/// definitions is a later step. This is the shape check that comes first.
public sealed class ScenarioDefinitionModelTests
{
    [Fact]
    public void Definition_ExpressesWatchtowerIdentityAndProgress()
    {
        ScenarioDefinition definition = CreateWatchtowerDefinition();

        Assert.Equal(
            WatchtowerScenarioContent.ScenarioId,
            definition.ScenarioId);
        Assert.Equal(
            WatchtowerScenarioContent.OutpostLocationId,
            definition.StartingLocationId);

        // Every marker the running scenario uses is declared by the definition.
        Assert.Equal(
            Enum.GetNames<WatchtowerScenarioProgress>().Order(),
            definition.Progress.ProgressIds.Order());
        Assert.Equal(
            WatchtowerScenarioProgress.MissionNotAccepted.ToString(),
            definition.Progress.InitialProgressId);
    }

    /// Party-defeated ends the scenario at the watchtower, which is exactly the
    /// rule ScenarioConclusionValidator enforces today.
    [Fact]
    public void Definition_ExpressesTheDefeatConclusion()
    {
        ScenarioConclusionDefinition defeat = Assert.Single(
            CreateWatchtowerDefinition().Progress.Conclusions,
            conclusion => conclusion.ProgressId
                == WatchtowerScenarioProgress.PartyDefeated.ToString());

        Assert.False(defeat.IsSuccess);
        Assert.Equal(
            WatchtowerRegionalRoute.WatchtowerLocationId,
            defeat.LocationId);
    }

    [Fact]
    public void Definition_ExpressesBothLocationsAndTheRouteBetweenThem()
    {
        ScenarioDefinition definition = CreateWatchtowerDefinition();

        ScenarioLocationDefinition outpost = Assert.Single(
            definition.Locations,
            location => location.LocationId
                == WatchtowerScenarioContent.OutpostLocationId);
        ScenarioLocationDefinition watchtower = Assert.Single(
            definition.Locations,
            location => location.LocationId
                == WatchtowerRegionalRoute.WatchtowerLocationId);

        // A hub has no map; a dungeon does.
        Assert.Null(outpost.ExplorationMap);
        ExplorationMapDefinition map = Assert.IsType<ExplorationMapDefinition>(
            watchtower.ExplorationMap);
        Assert.Equal("map.ruined-watchtower", map.MapId);
        Assert.Equal("GroundFloor", map.StartingFloor);
        Assert.Equal(ExplorationFacing.East, map.StartingFacing);

        TravelRouteDefinition route = Assert.Single(definition.Routes);
        Assert.Equal(WatchtowerRegionalRoute.RouteId, route.RouteId);
        Assert.Equal(
            WatchtowerRegionalRoute.FinalStepIndex,
            route.FinalStepIndex);
        Assert.Equal(
            WatchtowerScenarioContent.OutpostLocationId,
            route.OriginLocationId);
        Assert.Equal(
            WatchtowerRegionalRoute.WatchtowerLocationId,
            route.DestinationLocationId);
    }

    /// The signal is a positional interaction that starts the ambush — the
    /// model has to carry both parts of that.
    [Fact]
    public void Definition_ExpressesTheSignalTrigger()
    {
        ScenarioTriggerDefinition signal = Assert.Single(
            CreateWatchtowerDefinition().Triggers);

        Assert.Equal(
            WatchtowerRegionalRoute.WatchtowerLocationId,
            signal.LocationId);
        Assert.Equal("UpperFloor", signal.Floor);
        Assert.Equal(
            new GridPosition(1, 1),
            signal.Position);
        Assert.Equal(
            WatchtowerSignalEncounter.EncounterId,
            signal.EncounterId);
        Assert.Equal(
            WatchtowerScenarioProgress.SignalActivated.ToString(),
            signal.ResultingProgressId);
    }

    [Fact]
    public void Definition_ExpressesTheAmbushAndItsRaiders()
    {
        EncounterDefinition encounter = Assert.Single(
            CreateWatchtowerDefinition().Encounters);

        Assert.Equal(
            WatchtowerSignalEncounter.EncounterId,
            encounter.EncounterId);
        Assert.Equal(
            WatchtowerSignalEncounter.BattlefieldId,
            encounter.BattlefieldId);
        Assert.Equal(
            WatchtowerSignalEncounter.PartySideId,
            encounter.PartySideId);

        EncounterCombatantDefinition melee = Assert.Single(
            encounter.Combatants,
            combatant => combatant.CombatantId
                == WatchtowerSignalEncounter.MeleeRaiderId);
        EncounterCombatantDefinition ranged = Assert.Single(
            encounter.Combatants,
            combatant => combatant.CombatantId
                == WatchtowerSignalEncounter.RangedRaiderId);

        Assert.All(
            new[] { melee, ranged },
            raider => Assert.Equal(
                WatchtowerSignalEncounter.RaiderSideId,
                raider.SideId));

        Assert.Equal(new GridPosition(2, 1), melee.StartingPosition);
        Assert.Equal(new GridPosition(4, 2), ranged.StartingPosition);

        // A monster's own stats (hit points, weapons, ...) now live once in
        // the ruleset and are only referenced here by ID -- that stat data
        // is covered by Core-side monster validator/mapper tests, not here.
        Assert.Equal(MeleeMonsterId, melee.MonsterId);
        Assert.Equal(RangedMonsterId, ranged.MonsterId);
    }

    /// The scenario states what it needs of a party without describing one,
    /// which is what keeps composition a campaign concern.
    [Fact]
    public void Definition_StatesPartyRequirementsWithoutAnyRoster()
    {
        PartyRequirementDefinition requirement =
            CreateWatchtowerDefinition().PartyRequirement;

        Assert.Equal(3, requirement.MinimumMembers);
        Assert.Equal(3, requirement.MaximumMembers);
        Assert.Equal(1, requirement.MinimumConsciousMembers);

        Assert.DoesNotContain(
            typeof(ScenarioDefinition).GetProperties(),
            property => property.Name.Contains(
                "Party",
                StringComparison.Ordinal)
                && property.Name != nameof(ScenarioDefinition.PartyRequirement));
    }

    /// Shared with ScenarioDefinitionValidatorTests, which uses this as the
    /// known-good baseline it then breaks one field at a time.
    internal static ScenarioDefinition CreateWatchtowerDefinition()
    {
        return new ScenarioDefinition
        {
            ScenarioId = WatchtowerScenarioContent.ScenarioId,
            DisplayName = "The Ruined Watchtower",
            RulesetId = "ruleset.watchtower",
            StartingLocationId = WatchtowerScenarioContent.OutpostLocationId,
            Progress = new ScenarioProgressDefinition
            {
                InitialProgressId =
                    WatchtowerScenarioProgress.MissionNotAccepted.ToString(),
                ProgressIds = Enum.GetNames<WatchtowerScenarioProgress>(),
                Conclusions =
                [
                    new ScenarioConclusionDefinition
                    {
                        ProgressId =
                            WatchtowerScenarioProgress.PartyDefeated.ToString(),
                        IsSuccess = false,
                        LocationId =
                            WatchtowerRegionalRoute.WatchtowerLocationId,
                        EpilogueText = "The party is defeated."
                    }
                ]
            },
            PartyRequirement = new PartyRequirementDefinition
            {
                MinimumMembers = 3,
                MaximumMembers = 3,
                MinimumConsciousMembers = 1
            },
            Locations =
            [
                new ScenarioLocationDefinition
                {
                    LocationId = WatchtowerScenarioContent.OutpostLocationId,
                    DisplayName = "Frontier Outpost",
                    ExplorationMap = null
                },
                new ScenarioLocationDefinition
                {
                    LocationId = WatchtowerRegionalRoute.WatchtowerLocationId,
                    DisplayName = "Ruined Watchtower",
                    ExplorationMap = CreateWatchtowerMap()
                }
            ],
            Routes =
            [
                new TravelRouteDefinition
                {
                    RouteId = WatchtowerRegionalRoute.RouteId,
                    OriginLocationId =
                        WatchtowerScenarioContent.OutpostLocationId,
                    DestinationLocationId =
                        WatchtowerRegionalRoute.WatchtowerLocationId,
                    FinalStepIndex = WatchtowerRegionalRoute.FinalStepIndex,
                    RequiredProgressIds =
                    [
                        WatchtowerScenarioProgress.MissionAccepted.ToString()
                    ]
                }
            ],
            Encounters = [CreateAmbush()],
            Triggers =
            [
                new ScenarioTriggerDefinition
                {
                    TriggerId = "trigger.watchtower-signal",
                    DisplayName = "Signal Mechanism",
                    LocationId = WatchtowerRegionalRoute.WatchtowerLocationId,
                    Floor = "UpperFloor",
                    Position = new GridPosition(1, 1),
                    RequiredProgressIds =
                    [
                        WatchtowerScenarioProgress.MissionAccepted.ToString()
                    ],
                    ResultingProgressId =
                        WatchtowerScenarioProgress.SignalActivated.ToString(),
                    EncounterId = WatchtowerSignalEncounter.EncounterId
                }
            ],
            Decisions =
            [
                new ScenarioDecisionDefinition
                {
                    DecisionId = "decision.watchtower-mission",
                    DisplayName = "Investigate the ruined watchtower",
                    LocationId = WatchtowerScenarioContent.OutpostLocationId,
                    RequiredProgressIds =
                    [
                        WatchtowerScenarioProgress.MissionNotAccepted.ToString()
                    ],
                    Options =
                    [
                        new ScenarioDecisionOptionDefinition
                        {
                            OptionId = "AcceptMission",
                            DisplayName = "Accept the commission",
                            ResultingProgressId =
                                WatchtowerScenarioProgress.MissionAccepted.ToString()
                        }
                    ]
                }
            ],
            Shops = []
        };
    }

    private static ExplorationMapDefinition CreateWatchtowerMap()
    {
        return new ExplorationMapDefinition
        {
            MapId = "map.ruined-watchtower",
            Width = 5,
            Height = 5,
            StartingFloor = "GroundFloor",
            StartingPosition = new GridPosition(0, 0),
            StartingFacing = ExplorationFacing.East,
            Floors =
            [
                new ExplorationFloorDefinition
                {
                    Floor = "GroundFloor",
                    TraversablePositions =
                    [
                        new GridPosition(0, 0),
                        new GridPosition(1, 0)
                    ],
                    Stairs =
                    [
                        new StairDefinition
                        {
                            Position = new GridPosition(1, 0),
                            DestinationFloor = "UpperFloor",
                            DestinationPosition = new GridPosition(1, 0)
                        }
                    ],
                    Doors = [],
                    Treasures = [],
                    Npcs = []
                },
                new ExplorationFloorDefinition
                {
                    Floor = "UpperFloor",
                    TraversablePositions =
                    [
                        new GridPosition(1, 0),
                        new GridPosition(1, 1)
                    ],
                    Stairs = [],
                    Doors = [],
                    Treasures = [],
                    Npcs = []
                }
            ]
        };
    }

    /// This file proves the definition model, not the ruleset -- the stat
    /// blocks these IDs would resolve against are exercised by Core-side
    /// monster validator/mapper tests, so the literal values here only need
    /// to be stable strings, not real ruleset content.
    private const string MeleeMonsterId = "monster.watchtower-raider.marauder";

    private const string RangedMonsterId = "monster.watchtower-raider.archer";

    private static EncounterDefinition CreateAmbush()
    {
        return new EncounterDefinition
        {
            EncounterId = WatchtowerSignalEncounter.EncounterId,
            BattlefieldId = WatchtowerSignalEncounter.BattlefieldId,
            Width = 6,
            Height = 5,
            PartySideId = WatchtowerSignalEncounter.PartySideId,
            BlockedPositions = [],
            PartyStartingPositions =
            [
                new GridPosition(0, 0),
                new GridPosition(0, 1),
                new GridPosition(0, 2)
            ],
            Combatants =
            [
                new EncounterCombatantDefinition
                {
                    CombatantId = WatchtowerSignalEncounter.MeleeRaiderId,
                    MonsterId = MeleeMonsterId,
                    SideId = WatchtowerSignalEncounter.RaiderSideId,
                    StartingPosition = new GridPosition(2, 1)
                },
                new EncounterCombatantDefinition
                {
                    CombatantId = WatchtowerSignalEncounter.RangedRaiderId,
                    MonsterId = RangedMonsterId,
                    SideId = WatchtowerSignalEncounter.RaiderSideId,
                    StartingPosition = new GridPosition(4, 2)
                }
            ],
            Outcome = new EncounterOutcomeDefinition
            {
                VictoryProgressId =
                    WatchtowerScenarioProgress.RaidersDefeated.ToString(),
                DefeatProgressId =
                    WatchtowerScenarioProgress.PartyDefeated.ToString()
            }
        };
    }
}

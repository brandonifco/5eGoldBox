using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Scenarios;

/// A second scenario, authored to prove the engine runs on content rather than
/// on the Watchtower.
///
/// It shares nothing with the first scenario: different progress markers,
/// locations, route, map and triggers, and no combat at all. Nothing here is
/// derived from Watchtower constants, so if any rule still reached for those
/// this scenario would not run.
///
/// It is deliberately small. Its job is to exercise the whole non-combat path -
/// decide, travel, arrive, explore, act, finish - not to be an adventure worth
/// playing.
internal static class SunkenChapelScenarioDefinitionProvider
{
    internal const string ScenarioId = "scenario.sunken-chapel";

    internal const string HarborLocationId = "location.harbor-town";

    internal const string ChapelLocationId = "location.sunken-chapel";

    internal const string RouteId = "route.harbor-to-chapel";

    /// This scenario's own vocabulary. The strings look nothing like the first
    /// scenario's, which is the point: the engine stores a marker it cannot
    /// interpret.
    internal const string RumourHeard = "chapel.rumour-heard";

    internal const string CharterSigned = "chapel.charter-signed";

    internal const string SealBroken = "chapel.seal-broken";

    internal const string RelicRecovered = "chapel.relic-recovered";

    private const string RulesetId = "ruleset.core";

    private const int MapWidth = 2;

    private const int MapHeight = 2;

    internal static ScenarioDefinition Create()
    {
        return new ScenarioDefinition
        {
            ScenarioId = ScenarioId,
            DisplayName = "The Sunken Chapel",
            RulesetId = RulesetId,
            StartingLocationId = HarborLocationId,
            Progress = CreateProgress(),
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
                    LocationId = HarborLocationId,
                    DisplayName = "Harbor Town",
                    ExplorationMap = null
                },
                new ScenarioLocationDefinition
                {
                    LocationId = ChapelLocationId,
                    DisplayName = "The Sunken Chapel",
                    ExplorationMap = CreateMap(),
                    // Explorable from arrival until the relic is lifted, which
                    // ends the scenario.
                    ExplorableProgressIds =
                    [
                        CharterSigned,
                        SealBroken
                    ]
                }
            ],
            Routes = [CreateRoute()],
            Encounters = [],
            Triggers = [CreateSealTrigger(), CreateRelicTrigger()],
            Decisions = [CreateCharterDecision()]
        };
    }

    private static ScenarioProgressDefinition CreateProgress()
    {
        return new ScenarioProgressDefinition
        {
            InitialProgressId = RumourHeard,
            ProgressIds =
            [
                RumourHeard,
                CharterSigned,
                SealBroken,
                RelicRecovered
            ],
            Conclusions =
            [
                new ScenarioConclusionDefinition
                {
                    ProgressId = RelicRecovered,
                    IsSuccess = true,
                    LocationId = ChapelLocationId
                }
            ]
        };
    }

    private static TravelRouteDefinition CreateRoute()
    {
        return new TravelRouteDefinition
        {
            RouteId = RouteId,
            OriginLocationId = HarborLocationId,
            DestinationLocationId = ChapelLocationId,
            FinalStepIndex = 2,
            RequiredProgressIds = [CharterSigned]
        };
    }

    private static ScenarioDecisionDefinition CreateCharterDecision()
    {
        return new ScenarioDecisionDefinition
        {
            DecisionId = "decision.chapel-charter",
            DisplayName = "Sign the salvage charter",
            LocationId = HarborLocationId,
            RequiredProgressIds = [RumourHeard],
            Options =
            [
                new ScenarioDecisionOptionDefinition
                {
                    OptionId = OutpostMissionChoice.AcceptMission.ToString(),
                    DisplayName = "Sign the charter",
                    ResultingProgressId = CharterSigned
                },
                new ScenarioDecisionOptionDefinition
                {
                    OptionId = OutpostMissionChoice.NotYet.ToString(),
                    DisplayName = "Not yet",
                    ResultingProgressId = null
                }
            ]
        };
    }

    /// Fixed to a square and a facing, so it exercises a trigger's strictest
    /// positional conditions.
    private static ScenarioTriggerDefinition CreateSealTrigger()
    {
        return new ScenarioTriggerDefinition
        {
            TriggerId = "trigger.chapel-seal",
            DisplayName = "Tide seal",
            LocationId = ChapelLocationId,
            Floor = ExplorationFloor.GroundFloor,
            Position = new GridPosition(1, 0),
            RequiredFacing = ExplorationFacing.East,
            RequiredProgressIds = [CharterSigned],
            ResultingProgressId = SealBroken,
            EncounterId = null
        };
    }

    /// Fixed to a square but not a facing, and it ends the scenario rather than
    /// starting a fight.
    private static ScenarioTriggerDefinition CreateRelicTrigger()
    {
        return new ScenarioTriggerDefinition
        {
            TriggerId = "trigger.chapel-relic",
            DisplayName = "Drowned reliquary",
            LocationId = ChapelLocationId,
            Floor = ExplorationFloor.GroundFloor,
            Position = new GridPosition(1, 1),
            RequiredFacing = null,
            RequiredProgressIds = [SealBroken],
            ResultingProgressId = RelicRecovered,
            EncounterId = null
        };
    }

    /// One small floor, no stairs - the first scenario already covers those.
    private static ExplorationMapDefinition CreateMap()
    {
        return new ExplorationMapDefinition
        {
            MapId = "map.sunken-chapel",
            Width = MapWidth,
            Height = MapHeight,
            StartingFloor = ExplorationFloor.GroundFloor,
            StartingPosition = new GridPosition(0, 0),
            StartingFacing = ExplorationFacing.East,
            Floors =
            [
                new ExplorationFloorDefinition
                {
                    Floor = ExplorationFloor.GroundFloor,
                    TraversablePositions =
                    [
                        new GridPosition(0, 0),
                        new GridPosition(1, 0),
                        new GridPosition(0, 1),
                        new GridPosition(1, 1)
                    ],
                    Stairs = []
                }
            ]
        };
    }
}

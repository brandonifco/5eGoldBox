using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Scenarios;

/// The Watchtower scenario expressed as authored data.
///
/// Every value here is taken from the constants the running implementation
/// already uses, so this describes the scenario that exists rather than a
/// parallel one. Execution does not read from it yet — rewiring the rules to
/// consume definitions instead of constants is the next step, and this is the
/// content it will consume.
internal static class WatchtowerScenarioDefinitionProvider
{
    private const string RulesetId = "ruleset.watchtower";

    internal static ScenarioDefinition Create()
    {
        return new ScenarioDefinition
        {
            ScenarioId = WatchtowerScenarioContent.ScenarioId,
            DisplayName = "The Ruined Watchtower",
            RulesetId = RulesetId,
            StartingLocationId = WatchtowerScenarioContent.OutpostLocationId,
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
                    LocationId = WatchtowerScenarioContent.OutpostLocationId,
                    DisplayName = "Frontier Outpost",
                    ExplorationMap = null
                },
                new ScenarioLocationDefinition
                {
                    LocationId = WatchtowerRegionalRoute.WatchtowerLocationId,
                    DisplayName = "Ruined Watchtower",
                    ExplorationMap = CreateMap()
                }
            ],
            Routes = [CreateRoute()],
            Encounters = [CreateSignalAmbush()],
            Triggers = [CreateSignalTrigger()],
            Decisions = [CreateMissionDecision()]
        };
    }

    private static ScenarioProgressDefinition CreateProgress()
    {
        return new ScenarioProgressDefinition
        {
            InitialProgressId = Progress(
                WatchtowerScenarioProgress.MissionNotAccepted),
            // The full vocabulary the save format records. Two markers are
            // declared but unproduced today; validation reports them.
            ProgressIds = Enum.GetNames<WatchtowerScenarioProgress>(),
            Conclusions =
            [
                new ScenarioConclusionDefinition
                {
                    ProgressId = Progress(
                        WatchtowerScenarioProgress.PartyDefeated),
                    IsSuccess = false,
                    LocationId = WatchtowerRegionalRoute.WatchtowerLocationId
                }
            ]
        };
    }

    private static TravelRouteDefinition CreateRoute()
    {
        return new TravelRouteDefinition
        {
            RouteId = WatchtowerRegionalRoute.RouteId,
            OriginLocationId = WatchtowerScenarioContent.OutpostLocationId,
            DestinationLocationId =
                WatchtowerRegionalRoute.WatchtowerLocationId,
            FinalStepIndex = WatchtowerRegionalRoute.FinalStepIndex,
            RequiredProgressIds =
            [
                Progress(WatchtowerScenarioProgress.MissionAccepted)
            ]
        };
    }

    private static ScenarioDecisionDefinition CreateMissionDecision()
    {
        return new ScenarioDecisionDefinition
        {
            DecisionId = "decision.watchtower-mission",
            DisplayName = "Investigate the ruined watchtower",
            LocationId = WatchtowerScenarioContent.OutpostLocationId,
            RequiredProgressIds =
            [
                Progress(WatchtowerScenarioProgress.MissionNotAccepted)
            ],
            Options =
            [
                new ScenarioDecisionOptionDefinition
                {
                    OptionId = OutpostMissionChoice.AcceptMission.ToString(),
                    DisplayName = "Accept the commission",
                    ResultingProgressId = Progress(
                        WatchtowerScenarioProgress.MissionAccepted)
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

    private static ScenarioTriggerDefinition CreateSignalTrigger()
    {
        return new ScenarioTriggerDefinition
        {
            TriggerId = "trigger.watchtower-signal",
            DisplayName = "Signal mechanism",
            LocationId = WatchtowerRegionalRoute.WatchtowerLocationId,
            // The definition is now the authority for where the mechanism is
            // and which way the party must face to work it.
            Floor = ExplorationFloor.UpperFloor,
            Position = new GridPosition(1, 1),
            RequiredFacing = ExplorationFacing.East,
            RequiredProgressIds =
            [
                Progress(WatchtowerScenarioProgress.MissionAccepted)
            ],
            ResultingProgressId = Progress(
                WatchtowerScenarioProgress.SignalActivated),
            EncounterId = WatchtowerSignalEncounter.EncounterId
        };
    }

    private static EncounterDefinition CreateSignalAmbush()
    {
        return new EncounterDefinition
        {
            EncounterId = WatchtowerSignalEncounter.EncounterId,
            BattlefieldId = WatchtowerSignalEncounter.BattlefieldId,
            Width = WatchtowerSignalEncounter.BattlefieldWidth,
            Height = WatchtowerSignalEncounter.BattlefieldHeight,
            PartySideId = WatchtowerSignalEncounter.PartySideId,
            BlockedPositions = [],
            PartyStartingPositions =
                WatchtowerSignalEncounter.PartyStartingPositions,
            Combatants =
            [
                new CombatantDefinition
                {
                    CombatantId = WatchtowerSignalEncounter.MeleeRaiderId,
                    DisplayName = "Raider marauder",
                    SideId = WatchtowerSignalEncounter.RaiderSideId,
                    MaximumHitPoints = 9,
                    ArmorClass = 13,
                    MovementSpeedFeet = 30,
                    StartingPosition =
                        WatchtowerSignalEncounter.MeleeRaiderStartingPosition,
                    ZeroHitPointPolicy =
                        CombatantZeroHitPointPolicy.Defeated,
                    Weapons =
                    [
                        new CombatantWeaponDefinition
                        {
                            WeaponId = WatchtowerSignalEncounter.MeleeRaiderWeaponId
                        }
                    ]
                },
                new CombatantDefinition
                {
                    CombatantId = WatchtowerSignalEncounter.RangedRaiderId,
                    DisplayName = "Raider archer",
                    SideId = WatchtowerSignalEncounter.RaiderSideId,
                    MaximumHitPoints = 8,
                    ArmorClass = 13,
                    MovementSpeedFeet = 30,
                    StartingPosition =
                        WatchtowerSignalEncounter.RangedRaiderStartingPosition,
                    ZeroHitPointPolicy =
                        CombatantZeroHitPointPolicy.Defeated,
                    Weapons =
                    [
                        new CombatantWeaponDefinition
                        {
                            WeaponId =
                                WatchtowerSignalEncounter.RangedRaiderWeaponId,
                            AmmunitionItemId =
                                WatchtowerSignalEncounter.RangedRaiderAmmunitionItemId,
                            AmmunitionQuantity =
                                WatchtowerSignalEncounter.RangedRaiderAmmunitionQuantity
                        }
                    ]
                }
            ],
            Outcome = new EncounterOutcomeDefinition
            {
                VictoryProgressId = Progress(
                    WatchtowerScenarioProgress.RaidersDefeated),
                DefeatProgressId = Progress(
                    WatchtowerScenarioProgress.PartyDefeated)
            }
        };
    }

    private const int MapWidth = 3;

    private const int MapHeight = 3;

    private static ExplorationMapDefinition CreateMap()
    {
        return new ExplorationMapDefinition
        {
            MapId = "map.ruined-watchtower",
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
                        new GridPosition(2, 0),
                        new GridPosition(0, 1),
                        new GridPosition(2, 1),
                        new GridPosition(0, 2),
                        new GridPosition(1, 2),
                        new GridPosition(2, 2)
                    ],
                    Stairs =
                    [
                        new StairDefinition
                        {
                            Position = new GridPosition(2, 0),
                            DestinationFloor = ExplorationFloor.UpperFloor,
                            DestinationPosition = new GridPosition(2, 0)
                        }
                    ]
                },
                new ExplorationFloorDefinition
                {
                    Floor = ExplorationFloor.UpperFloor,
                    TraversablePositions =
                    [
                        new GridPosition(0, 0),
                        new GridPosition(1, 0),
                        new GridPosition(2, 0),
                        new GridPosition(0, 1),
                        new GridPosition(1, 1),
                        new GridPosition(2, 1)
                    ],
                    Stairs =
                    [
                        new StairDefinition
                        {
                            Position = new GridPosition(2, 0),
                            DestinationFloor = ExplorationFloor.GroundFloor,
                            DestinationPosition = new GridPosition(2, 0)
                        }
                    ]
                }
            ]
        };
    }

    private static string Progress(
        WatchtowerScenarioProgress progress)
    {
        return WatchtowerScenario.ToProgressId(progress);
    }
}

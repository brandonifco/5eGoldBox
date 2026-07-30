using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Scenarios;

/// A third scenario, authored to be worth playing rather than to prove a
/// seam — the first two adventures made the case that the engine runs on
/// content; this one asks something more of that content.
///
/// It adds two things neither earlier scenario has: a real branching choice
/// with mechanical consequences, and a two-floor dungeon reached by stairs.
/// The branch is expressed entirely with existing primitives — two triggers
/// gated on mutually exclusive progress markers — rather than any new
/// content type, per this codebase's own discipline of generalizing only
/// after a pattern repeats. If a fourth scenario wants a third branch, or a
/// branch offered mid-dungeon rather than only at start, that is the
/// evidence a real "decision with more than two outcomes" type would need;
/// it is not manufactured here.
internal static class HollowMillScenarioDefinitionProvider
{
    internal const string ScenarioId = "scenario.hollow-mill";

    internal const string VillageLocationId = "location.hollow-mill-village";

    internal const string MillLocationId = "location.hollow-mill-house";

    internal const string RouteId = "route.village-to-mill";

    /// A second, faster road to the same place — the vehicle for proving
    /// multi-route travel with real content rather than a mock. The cart
    /// track is the safe default every test before this one walks; the
    /// shortcut is the same destination in fewer steps, for whichever test
    /// wants to prove the party can choose between them.
    internal const string ShortcutRouteId = "route.village-to-mill.shortcut";

    /// This scenario's own vocabulary.
    internal const string RumorHeard = "mill.rumor-heard";

    internal const string CommissionAccepted = "mill.commission-accepted";

    internal const string HerbalistConsulted = "mill.herbalist-consulted";

    internal const string MillerConfronted = "mill.miller-confronted";

    internal const string VerminRoused = "mill.vermin-roused";

    internal const string MillersLost = "mill.millers-lost";

    internal const string BlightSevered = "mill.blight-severed";

    internal const string InformedEncounterId = "encounter.mill-vermin-swarm";

    internal const string BlindEncounterId = "encounter.mill-thrall-ambush";

    internal const string VerminSideId = "side.mill-vermin";

    internal const string RatBiteWeaponId = "weapon.mill-rat.bite";

    internal const string ThrallCleaverWeaponId = "weapon.mill-thrall.cleaver";

    // Shared with the other two scenarios: one campaign, one body of rules.
    private const string RulesetId = RulesetRegistry.CampaignRulesetId;

    private const int MapWidth = 3;

    private const int MapHeight = 3;

    internal static ScenarioDefinition Create()
    {
        return new ScenarioDefinition
        {
            ScenarioId = ScenarioId,
            DisplayName = "The Hollow Mill",
            RulesetId = RulesetId,
            StartingLocationId = VillageLocationId,
            Progress = CreateProgress(),
            PartyRequirement = new PartyRequirementDefinition
            {
                MinimumMembers = 4,
                MaximumMembers = 4,
                MinimumConsciousMembers = 1
            },
            Locations =
            [
                new ScenarioLocationDefinition
                {
                    LocationId = VillageLocationId,
                    DisplayName = "Hollow Mill Village",
                    ExplorationMap = null
                },
                new ScenarioLocationDefinition
                {
                    LocationId = MillLocationId,
                    DisplayName = "The Old Mill",
                    ExplorationMap = CreateMap(),
                    // Explorable from the moment the party sets out for it
                    // until the root is severed, which ends the scenario.
                    ExplorableProgressIds =
                    [
                        CommissionAccepted,
                        HerbalistConsulted,
                        MillerConfronted,
                        VerminRoused
                    ]
                }
            ],
            Routes = [CreateRoute(), CreateShortcutRoute()],
            Encounters = [CreateInformedEncounter(), CreateBlindEncounter()],
            Triggers =
            [
                CreateHerbalistTrigger(),
                CreateConfrontMillerTrigger(),
                CreateInformedApproachTrigger(),
                CreateBlindApproachTrigger(),
                CreateBlightRootTrigger()
            ],
            Decisions = [CreateCommissionDecision()]
        };
    }

    private static ScenarioProgressDefinition CreateProgress()
    {
        return new ScenarioProgressDefinition
        {
            InitialProgressId = RumorHeard,
            ProgressIds =
            [
                RumorHeard,
                CommissionAccepted,
                HerbalistConsulted,
                MillerConfronted,
                VerminRoused,
                MillersLost,
                BlightSevered
            ],
            Conclusions =
            [
                new ScenarioConclusionDefinition
                {
                    ProgressId = BlightSevered,
                    IsSuccess = true,
                    LocationId = MillLocationId
                },
                new ScenarioConclusionDefinition
                {
                    ProgressId = MillersLost,
                    IsSuccess = false,
                    LocationId = MillLocationId
                }
            ]
        };
    }

    private static TravelRouteDefinition CreateRoute()
    {
        return new TravelRouteDefinition
        {
            RouteId = RouteId,
            OriginLocationId = VillageLocationId,
            DestinationLocationId = MillLocationId,
            FinalStepIndex = 2,
            RequiredProgressIds = [CommissionAccepted]
        };
    }

    private static TravelRouteDefinition CreateShortcutRoute()
    {
        return new TravelRouteDefinition
        {
            RouteId = ShortcutRouteId,
            OriginLocationId = VillageLocationId,
            DestinationLocationId = MillLocationId,
            FinalStepIndex = 1,
            RequiredProgressIds = [CommissionAccepted]
        };
    }

    /// The only decision the current outpost machinery can offer — take the
    /// commission or don't — same shape as the other two scenarios' hub
    /// decisions. The real branch lives inside the mill itself, below.
    /// Three options, deliberately — the vehicle for proving a decision can
    /// offer more than the old fixed accept/decline shape with real content
    /// rather than a mock. "Ask what it pays" is a second, distinctly-worded
    /// way to hold off that leaves the scenario exactly where "Not yet"
    /// does; the point is that a third option resolves purely by its own ID,
    /// with no built-in ceiling on how many a decision can carry.
    private static ScenarioDecisionDefinition CreateCommissionDecision()
    {
        return new ScenarioDecisionDefinition
        {
            DecisionId = "decision.mill-commission",
            DisplayName = "Look into the spoiled grain",
            LocationId = VillageLocationId,
            RequiredProgressIds = [RumorHeard],
            Options =
            [
                new ScenarioDecisionOptionDefinition
                {
                    OptionId = "AcceptMission",
                    DisplayName = "Take the commission",
                    ResultingProgressId = CommissionAccepted
                },
                new ScenarioDecisionOptionDefinition
                {
                    OptionId = "AskAboutPay",
                    DisplayName = "Ask what it pays before deciding",
                    ResultingProgressId = null
                },
                new ScenarioDecisionOptionDefinition
                {
                    OptionId = "NotYet",
                    DisplayName = "Not yet",
                    ResultingProgressId = null
                }
            ]
        };
    }

    /// The west door. Finding Old Rosa here instead of at her usual stall is
    /// what actually earns the party its warning — nothing prevents walking
    /// past this square to the stairs unwarned, which is exactly the point:
    /// the choice is real because skipping it is possible.
    private static ScenarioTriggerDefinition CreateHerbalistTrigger()
    {
        return new ScenarioTriggerDefinition
        {
            TriggerId = "trigger.herbalist-alcove",
            DisplayName = "Old Rosa's alcove",
            LocationId = MillLocationId,
            Floor = "GroundFloor",
            Position = new GridPosition(0, 1),
            RequiredFacing = null,
            RequiredProgressIds = [CommissionAccepted],
            ResultingProgressId = HerbalistConsulted,
            EncounterId = null
        };
    }

    /// The east door. Confronting the miller tips him off, which is why the
    /// blind approach below treats this the same as never stopping to ask
    /// anyone at all.
    private static ScenarioTriggerDefinition CreateConfrontMillerTrigger()
    {
        return new ScenarioTriggerDefinition
        {
            TriggerId = "trigger.confront-miller",
            DisplayName = "The mill floor",
            LocationId = MillLocationId,
            Floor = "GroundFloor",
            Position = new GridPosition(2, 1),
            RequiredFacing = null,
            RequiredProgressIds = [CommissionAccepted],
            ResultingProgressId = MillerConfronted,
            EncounterId = null
        };
    }

    /// Same square as the trigger below, gated on the opposite branch.
    /// Whichever the party's progress actually is, exactly one of the two
    /// is ever available — they can never fight both variants.
    private static ScenarioTriggerDefinition CreateInformedApproachTrigger()
    {
        return new ScenarioTriggerDefinition
        {
            TriggerId = "trigger.cellar-informed-approach",
            DisplayName = "The flooded grain vault",
            LocationId = MillLocationId,
            Floor = "UpperFloor",
            Position = new GridPosition(2, 0),
            RequiredFacing = null,
            RequiredProgressIds = [HerbalistConsulted],
            ResultingProgressId = VerminRoused,
            EncounterId = InformedEncounterId
        };
    }

    /// Reached with progress still at CommissionAccepted (never stopped for
    /// either door) or at MillerConfronted (stopped and tipped him off) —
    /// both are "the vermin were never talked down," which is why both
    /// share this one trigger rather than needing a third variant.
    private static ScenarioTriggerDefinition CreateBlindApproachTrigger()
    {
        return new ScenarioTriggerDefinition
        {
            TriggerId = "trigger.cellar-blind-approach",
            DisplayName = "The flooded grain vault",
            LocationId = MillLocationId,
            Floor = "UpperFloor",
            Position = new GridPosition(2, 0),
            RequiredFacing = null,
            RequiredProgressIds = [CommissionAccepted, MillerConfronted],
            ResultingProgressId = VerminRoused,
            EncounterId = BlindEncounterId
        };
    }

    private static ScenarioTriggerDefinition CreateBlightRootTrigger()
    {
        return new ScenarioTriggerDefinition
        {
            TriggerId = "trigger.blight-root",
            DisplayName = "The blighted root",
            LocationId = MillLocationId,
            Floor = "UpperFloor",
            Position = new GridPosition(2, 1),
            RequiredFacing = null,
            RequiredProgressIds = [VerminRoused],
            ResultingProgressId = BlightSevered,
            EncounterId = null
        };
    }

    /// The easier fight: the herbalist's warning kept the miller's thrall
    /// from ever being roused, so only the rat swarm is here.
    private static EncounterDefinition CreateInformedEncounter()
    {
        return new EncounterDefinition
        {
            EncounterId = InformedEncounterId,
            BattlefieldId = "battlefield.mill-vault-rats",
            Width = 4,
            Height = 4,
            PartySideId = "side.party",
            BlockedPositions = [],
            PartyStartingPositions =
            [
                new GridPosition(0, 0),
                new GridPosition(0, 1),
                new GridPosition(0, 2),
                new GridPosition(0, 3)
            ],
            Combatants =
            [
                CreateRat("combatant.mill-rat.first", new GridPosition(3, 1)),
                CreateRat("combatant.mill-rat.second", new GridPosition(3, 2))
            ],
            Outcome = new EncounterOutcomeDefinition
            {
                VictoryProgressId = VerminRoused,
                DefeatProgressId = MillersLost
            }
        };
    }

    /// The harder fight: the same two rats, plus the miller's thrall,
    /// roused either by a direct confrontation or by nobody warning anyone
    /// at all.
    private static EncounterDefinition CreateBlindEncounter()
    {
        return new EncounterDefinition
        {
            EncounterId = BlindEncounterId,
            BattlefieldId = "battlefield.mill-vault-ambush",
            Width = 4,
            Height = 4,
            PartySideId = "side.party",
            BlockedPositions = [],
            PartyStartingPositions =
            [
                new GridPosition(0, 0),
                new GridPosition(0, 1),
                new GridPosition(0, 2),
                new GridPosition(0, 3)
            ],
            Combatants =
            [
                CreateRat("combatant.mill-rat.first", new GridPosition(3, 1)),
                CreateRat("combatant.mill-rat.second", new GridPosition(3, 2)),
                CreateThrall(new GridPosition(3, 0))
            ],
            Outcome = new EncounterOutcomeDefinition
            {
                VictoryProgressId = VerminRoused,
                DefeatProgressId = MillersLost
            }
        };
    }

    private static CombatantDefinition CreateRat(
        string combatantId,
        GridPosition startingPosition)
    {
        return new CombatantDefinition
        {
            CombatantId = combatantId,
            DisplayName = "Giant rat",
            SideId = VerminSideId,
            MaximumHitPoints = 6,
            ArmorClass = 11,
            MovementSpeedFeet = 30,
            StartingPosition = startingPosition,
            ZeroHitPointPolicy = CombatantZeroHitPointPolicy.Defeated,
            AbilityModifiers =
            [
                Modifier(Ability.Strength, -1),
                Modifier(Ability.Dexterity, 1),
                Modifier(Ability.Constitution, 0),
                Modifier(Ability.Intelligence, -4),
                Modifier(Ability.Wisdom, 0),
                Modifier(Ability.Charisma, -3)
            ],
            ProficiencyBonus = 2,
            Weapons =
            [
                new CombatantWeaponDefinition
                {
                    WeaponId = RatBiteWeaponId
                }
            ]
        };
    }

    private static CombatantDefinition CreateThrall(
        GridPosition startingPosition)
    {
        return new CombatantDefinition
        {
            CombatantId = "combatant.mill-thrall",
            DisplayName = "Miller's thrall",
            SideId = VerminSideId,
            MaximumHitPoints = 14,
            ArmorClass = 13,
            MovementSpeedFeet = 25,
            StartingPosition = startingPosition,
            // Driven, not directed — the blight has left nobody home to
            // surrender, the same call the chapel's guardians made.
            ZeroHitPointPolicy = CombatantZeroHitPointPolicy.Defeated,
            AbilityModifiers =
            [
                Modifier(Ability.Strength, 2),
                Modifier(Ability.Dexterity, 0),
                Modifier(Ability.Constitution, 2),
                Modifier(Ability.Intelligence, -2),
                Modifier(Ability.Wisdom, -1),
                Modifier(Ability.Charisma, -2)
            ],
            ProficiencyBonus = 2,
            Weapons =
            [
                new CombatantWeaponDefinition
                {
                    WeaponId = ThrallCleaverWeaponId
                }
            ]
        };
    }

    private static CombatantAbilityModifier Modifier(
        Ability ability,
        int modifier)
    {
        return new CombatantAbilityModifier
        {
            Ability = ability,
            Modifier = modifier
        };
    }

    /// One map, two floors: the mill house, and the flooded cellar below
    /// it. The ground floor's two doors are both open from the start; the
    /// cellar's two vault triggers occupy the same square and are gated on
    /// mutually exclusive progress, so only one of them is ever the one the
    /// party actually reaches.
    private static ExplorationMapDefinition CreateMap()
    {
        return new ExplorationMapDefinition
        {
            MapId = "map.hollow-mill",
            Width = MapWidth,
            Height = MapHeight,
            StartingFloor = "GroundFloor",
            StartingPosition = new GridPosition(1, 0),
            StartingFacing = ExplorationFacing.South,
            Floors =
            [
                new ExplorationFloorDefinition
                {
                    Floor = "GroundFloor",
                    TraversablePositions =
                    [
                        new GridPosition(0, 0),
                        new GridPosition(1, 0),
                        new GridPosition(2, 0),
                        new GridPosition(0, 1),
                        new GridPosition(1, 1),
                        new GridPosition(2, 1),
                        new GridPosition(0, 2),
                        new GridPosition(1, 2),
                        new GridPosition(2, 2)
                    ],
                    Stairs =
                    [
                        new StairDefinition
                        {
                            Position = new GridPosition(1, 2),
                            DestinationFloor = "UpperFloor",
                            DestinationPosition = new GridPosition(0, 0)
                        }
                    ]
                },
                new ExplorationFloorDefinition
                {
                    Floor = "UpperFloor",
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
                            Position = new GridPosition(0, 0),
                            DestinationFloor = "GroundFloor",
                            DestinationPosition = new GridPosition(1, 2)
                        }
                    ]
                }
            ]
        };
    }
}

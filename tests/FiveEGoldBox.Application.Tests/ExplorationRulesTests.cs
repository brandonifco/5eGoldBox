using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class ExplorationRulesTests
{
    [Fact]
    public void CanEnterWatchtower_AfterCanonicalArrival_ReturnsTrue()
    {
        Assert.True(
            ExplorationRules.CanEnterWatchtower(
                CreateCompletedArrival()));
    }

    [Fact]
    public void CanEnterWatchtower_InOrdinarilyUnavailableStates_ReturnsFalse()
    {
        ApplicationSessionState beforeAcceptance =
            CreateMissionNotAcceptedSession();
        ApplicationSessionState beforeArrival =
            CreateTravelingSession();
        ApplicationSessionState exploring =
            CreateExplorationSession();
        ApplicationSessionState encounter =
            SignalMechanismRules.Activate(
                WatchtowerSignalTestData
                    .CreateSignalReadySession());
        ApplicationSessionState conclusion =
            WatchtowerCombatOutcomeRules.Finalize(
                WatchtowerCombatOutcomeTestData
                    .CreateRaiderVictorySession())
                .State;
        ApplicationSessionState completed =
            CreateCompletedArrival();
        RegionalTravelState completedTravel =
            Assert.IsType<RegionalTravelState>(
                completed.RegionalTravel);
        ApplicationSessionState wrongDestination =
            completed with
            {
                CurrentLocationId = "location.outpost",
                RegionalTravel = completedTravel with
                {
                    OriginLocationId =
                        "location.ruined-watchtower",
                    DestinationLocationId =
                        "location.outpost"
                }
            };
        ApplicationSessionState incompatibleProgress =
            completed with
            {
                Scenario = new WatchtowerScenarioState
                {
                    Progress =
                        WatchtowerScenarioProgress
                            .SignalActivated
                }
            };

        ApplicationSessionState[] unavailableStates =
        [
            beforeAcceptance,
            beforeArrival,
            exploring,
            encounter,
            conclusion,
            wrongDestination,
            incompatibleProgress
        ];

        foreach (ApplicationSessionState state
            in unavailableStates)
        {
            Assert.False(
                ExplorationRules.CanEnterWatchtower(
                    state));
        }
    }

    [Fact]
    public void CanEnterWatchtower_DoesNotMutateOrConsumeRandomness()
    {
        ApplicationSessionState arrived =
            CreateCompletedArrival();
        RegionalTravelState travel =
            Assert.IsType<RegionalTravelState>(
                arrived.RegionalTravel);

        _ = ExplorationRules.CanEnterWatchtower(arrived);

        Assert.Equal(ApplicationMode.RegionalTravel, arrived.CurrentMode);
        Assert.True(travel.IsComplete);
        Assert.Null(arrived.Exploration);
        Assert.Equal(8675309, arrived.RandomSeed);
        Assert.Equal(12, arrived.RandomValuesConsumed);
    }

    [Fact]
    public void CanEnterWatchtower_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ExplorationRules.CanEnterWatchtower(null!));
    }

    [Fact]
    public void CanEnterWatchtower_WithMalformedRegionalTravelState_Throws()
    {
        ApplicationSessionState malformed =
            CreateAcceptedSession() with
            {
                CurrentMode = ApplicationMode.RegionalTravel,
                RegionalTravel = null
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            ExplorationRules.CanEnterWatchtower(
                malformed));
    }

    [Fact]
    public void CanUseStairs_AtBothAuthoredStairStates_ReturnsTrue()
    {
        ApplicationSessionState groundFloor =
            CreateAtGroundFloorStairs();
        ApplicationSessionState upperFloor =
            ExplorationRules.UseStairs(groundFloor);

        Assert.True(ExplorationRules.CanUseStairs(groundFloor));
        Assert.True(ExplorationRules.CanUseStairs(upperFloor));
    }

    [Fact]
    public void CanUseStairs_AwayFromStairsOrOutsideExploration_ReturnsFalse()
    {
        ApplicationSessionState beforeStairs =
            ExplorationRules.MoveForward(
                CreateExplorationSession())
                .State;
        ApplicationSessionState upperFloor =
            ExplorationRules.UseStairs(
                CreateAtGroundFloorStairs());
        upperFloor = ExplorationRules.Turn(
            upperFloor,
            ExplorationTurnDirection.Right);
        ApplicationSessionState afterStairs =
            ExplorationRules.MoveForward(upperFloor)
                .State;

        Assert.False(
            ExplorationRules.CanUseStairs(beforeStairs));
        Assert.False(
            ExplorationRules.CanUseStairs(afterStairs));
        Assert.False(
            ExplorationRules.CanUseStairs(
                CreateAcceptedSession()));
    }

    [Fact]
    public void CanUseStairs_DoesNotMutateOrConsumeRandomness()
    {
        ApplicationSessionState stairs =
            CreateAtGroundFloorStairs();
        ExplorationState exploration =
            AssertExploration(stairs);

        _ = ExplorationRules.CanUseStairs(stairs);

        Assert.Equal(ExplorationFloor.GroundFloor, exploration.Floor);
        Assert.Equal(new GridPosition(2, 0), exploration.Position);
        Assert.Equal(8675309, stairs.RandomSeed);
        Assert.Equal(12, stairs.RandomValuesConsumed);
    }

    [Fact]
    public void CanUseStairs_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ExplorationRules.CanUseStairs(null!));
    }

    [Fact]
    public void CanUseStairs_WithMalformedExplorationState_Throws()
    {
        ApplicationSessionState malformed =
            CreateExplorationSession() with
            {
                Exploration = null
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            ExplorationRules.CanUseStairs(malformed));
    }

    [Fact]
    public void EnterWatchtower_WithCompletedArrival_CreatesAuthoredExplorationState()
    {
        ApplicationSessionState arrived =
            CreateCompletedArrival();

        ApplicationSessionState exploring =
            ExplorationRules.EnterWatchtower(arrived);
        ExplorationState exploration =
            Assert.IsType<ExplorationState>(
                exploring.Exploration);

        Assert.Equal(
            ApplicationMode.Exploration,
            exploring.CurrentMode);
        Assert.Equal(
            "location.ruined-watchtower",
            exploring.CurrentLocationId);
        Assert.Null(exploring.RegionalTravel);
        Assert.Equal(
            "map.ruined-watchtower",
            exploration.MapId);
        Assert.Equal(
            ExplorationFloor.GroundFloor,
            exploration.Floor);
        Assert.Equal(
            new GridPosition(0, 0),
            exploration.Position);
        Assert.Equal(
            ExplorationFacing.East,
            exploration.Facing);
    }

    [Fact]
    public void EnterWatchtower_WithCompletedArrival_PreservesPersistentState()
    {
        ApplicationSessionState arrived =
            CreateCompletedArrival();

        ApplicationSessionState exploring =
            ExplorationRules.EnterWatchtower(arrived);

        Assert.Equal(arrived.ScenarioId, exploring.ScenarioId);
        Assert.Equal(
            WatchtowerScenarioProgress.MissionAccepted,
            exploring.Scenario.Progress);
        Assert.Equal(arrived.RandomSeed, exploring.RandomSeed);
        Assert.Equal(
            arrived.RandomValuesConsumed,
            exploring.RandomValuesConsumed);
        AssertPartyEquivalent(
            arrived.Party,
            exploring.Party);
    }

    [Fact]
    public void EnterWatchtower_DoesNotMutateInputSession()
    {
        ApplicationSessionState arrived =
            CreateCompletedArrival();
        RegionalTravelState travel =
            Assert.IsType<RegionalTravelState>(
                arrived.RegionalTravel);

        _ = ExplorationRules.EnterWatchtower(arrived);

        Assert.Equal(
            ApplicationMode.RegionalTravel,
            arrived.CurrentMode);
        Assert.Same(travel, arrived.RegionalTravel);
        Assert.Null(arrived.Exploration);
        Assert.Equal(
            WatchtowerScenarioProgress.MissionAccepted,
            arrived.Scenario.Progress);
        Assert.Equal(12, arrived.RandomValuesConsumed);
    }

    [Fact]
    public void EnterWatchtower_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ExplorationRules.EnterWatchtower(null!));
    }

    [Fact]
    public void EnterWatchtower_WithInvalidSession_Throws()
    {
        ApplicationSessionState arrived =
            CreateCompletedArrival() with
            {
                ScenarioId = " "
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            ExplorationRules.EnterWatchtower(arrived));
    }

    [Fact]
    public void EnterWatchtower_WhenModeIsNotRegionalTravel_Throws()
    {
        ApplicationSessionState accepted =
            CreateAcceptedSession();

        Assert.Throws<InvalidOperationException>(() =>
            ExplorationRules.EnterWatchtower(accepted));
    }

    [Fact]
    public void EnterWatchtower_WithMissingTravelState_Throws()
    {
        ApplicationSessionState arrived =
            CreateCompletedArrival() with
            {
                RegionalTravel = null
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            ExplorationRules.EnterWatchtower(arrived));
    }

    [Fact]
    public void EnterWatchtower_BeforeRouteCompletion_Throws()
    {
        ApplicationSessionState traveling =
            CreateTravelingSession();

        Assert.Throws<InvalidOperationException>(() =>
            ExplorationRules.EnterWatchtower(traveling));
    }

    [Fact]
    public void EnterWatchtower_WithWrongDestination_Throws()
    {
        ApplicationSessionState arrived =
            CreateCompletedArrival();
        RegionalTravelState travel =
            Assert.IsType<RegionalTravelState>(
                arrived.RegionalTravel);
        ApplicationSessionState reverseArrival =
            arrived with
            {
                CurrentLocationId = "location.outpost",
                RegionalTravel = travel with
                {
                    OriginLocationId =
                        "location.ruined-watchtower",
                    DestinationLocationId =
                        "location.outpost"
                }
            };

        Assert.Throws<InvalidOperationException>(() =>
            ExplorationRules.EnterWatchtower(
                reverseArrival));
    }

    [Fact]
    public void EnterWatchtower_WithWrongCurrentLocation_Throws()
    {
        ApplicationSessionState arrived =
            CreateCompletedArrival() with
            {
                CurrentLocationId = "location.wrong"
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            ExplorationRules.EnterWatchtower(arrived));
    }

    [Fact]
    public void EnterWatchtower_WithUnsupportedRoute_Throws()
    {
        ApplicationSessionState arrived =
            CreateCompletedArrival();
        RegionalTravelState travel =
            Assert.IsType<RegionalTravelState>(
                arrived.RegionalTravel);
        ApplicationSessionState invalid =
            arrived with
            {
                RegionalTravel = travel with
                {
                    RouteId = "route.unsupported"
                }
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            ExplorationRules.EnterWatchtower(invalid));
    }

    [Fact]
    public void EnterWatchtower_BeforeMissionAcceptance_Throws()
    {
        ApplicationSessionState arrived =
            CreateCompletedArrival() with
            {
                Scenario = new WatchtowerScenarioState
                {
                    Progress =
                        WatchtowerScenarioProgress
                            .MissionNotAccepted
                }
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            ExplorationRules.EnterWatchtower(arrived));
    }

    [Theory]
    [InlineData(
        WatchtowerScenarioProgress.SignalActivated)]
    [InlineData(
        WatchtowerScenarioProgress.RaidersDefeated)]
    [InlineData(
        WatchtowerScenarioProgress.SuccessReported)]
    [InlineData(
        WatchtowerScenarioProgress.ScenarioCompleted)]
    public void EnterWatchtower_AfterMissionAcceptedStage_Throws(
        WatchtowerScenarioProgress progress)
    {
        ApplicationSessionState arrived =
            CreateCompletedArrival() with
            {
                Scenario = new WatchtowerScenarioState
                {
                    Progress = progress
                }
            };

        Assert.Throws<InvalidOperationException>(() =>
            ExplorationRules.EnterWatchtower(arrived));
    }

    [Theory]
    [InlineData(
        ExplorationFacing.North,
        ExplorationFacing.West)]
    [InlineData(
        ExplorationFacing.West,
        ExplorationFacing.South)]
    [InlineData(
        ExplorationFacing.South,
        ExplorationFacing.East)]
    [InlineData(
        ExplorationFacing.East,
        ExplorationFacing.North)]
    public void Turn_WithLeft_TurnsExactlyNinetyDegrees(
        ExplorationFacing originalFacing,
        ExplorationFacing expectedFacing)
    {
        ApplicationSessionState exploring =
            WithFacing(
                CreateExplorationSession(),
                originalFacing);

        ApplicationSessionState turned =
            ExplorationRules.Turn(
                exploring,
                ExplorationTurnDirection.Left);

        Assert.Equal(
            expectedFacing,
            AssertExploration(turned).Facing);
    }

    [Theory]
    [InlineData(
        ExplorationFacing.North,
        ExplorationFacing.East)]
    [InlineData(
        ExplorationFacing.East,
        ExplorationFacing.South)]
    [InlineData(
        ExplorationFacing.South,
        ExplorationFacing.West)]
    [InlineData(
        ExplorationFacing.West,
        ExplorationFacing.North)]
    public void Turn_WithRight_TurnsExactlyNinetyDegrees(
        ExplorationFacing originalFacing,
        ExplorationFacing expectedFacing)
    {
        ApplicationSessionState exploring =
            WithFacing(
                CreateExplorationSession(),
                originalFacing);

        ApplicationSessionState turned =
            ExplorationRules.Turn(
                exploring,
                ExplorationTurnDirection.Right);

        Assert.Equal(
            expectedFacing,
            AssertExploration(turned).Facing);
    }

    [Fact]
    public void Turn_PreservesPositionFloorAndPersistentState()
    {
        ApplicationSessionState exploring =
            CreateExplorationSession();
        ExplorationState original =
            AssertExploration(exploring);

        ApplicationSessionState turned =
            ExplorationRules.Turn(
                exploring,
                ExplorationTurnDirection.Right);
        ExplorationState result =
            AssertExploration(turned);

        Assert.Equal(original.Position, result.Position);
        Assert.Equal(original.Floor, result.Floor);
        Assert.Equal(exploring.Scenario, turned.Scenario);
        Assert.Equal(exploring.RandomSeed, turned.RandomSeed);
        Assert.Equal(
            exploring.RandomValuesConsumed,
            turned.RandomValuesConsumed);
        AssertPartyEquivalent(
            exploring.Party,
            turned.Party);
        Assert.Equal(
            ExplorationFacing.East,
            AssertExploration(exploring).Facing);
    }

    [Fact]
    public void Turn_WithUndefinedDirection_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ExplorationRules.Turn(
                CreateExplorationSession(),
                (ExplorationTurnDirection)999));
    }

    [Fact]
    public void Turn_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ExplorationRules.Turn(
                null!,
                ExplorationTurnDirection.Left));
    }

    [Fact]
    public void Turn_WhenModeIsNotExploration_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ExplorationRules.Turn(
                CreateAcceptedSession(),
                ExplorationTurnDirection.Left));
    }

    [Fact]
    public void Turn_WithMissingExplorationState_Throws()
    {
        ApplicationSessionState exploring =
            CreateExplorationSession() with
            {
                Exploration = null
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            ExplorationRules.Turn(
                exploring,
                ExplorationTurnDirection.Left));
    }

    [Fact]
    public void MoveForward_WhenDestinationIsOpen_MovesExactlyOneTile()
    {
        ApplicationSessionState exploring =
            CreateExplorationSession();

        ExplorationMoveResult result =
            ExplorationRules.MoveForward(exploring);
        ExplorationState moved =
            AssertExploration(result.State);

        Assert.True(result.DidMove);
        Assert.Equal(
            new GridPosition(1, 0),
            moved.Position);
        Assert.Equal(
            ExplorationFacing.East,
            moved.Facing);
        Assert.Equal(
            ExplorationFloor.GroundFloor,
            moved.Floor);
    }

    [Fact]
    public void MoveForward_WhenBlockedByWall_DoesNotMove()
    {
        ApplicationSessionState exploring =
            ExplorationRules.MoveForward(
                CreateExplorationSession()).State;
        exploring = ExplorationRules.Turn(
            exploring,
            ExplorationTurnDirection.Right);
        ExplorationState before =
            AssertExploration(exploring);

        ExplorationMoveResult result =
            ExplorationRules.MoveForward(exploring);
        ExplorationState after =
            AssertExploration(result.State);

        Assert.False(result.DidMove);
        Assert.Equal(before.Position, after.Position);
        Assert.Equal(before.Facing, after.Facing);
    }

    [Fact]
    public void MoveForward_WhenOutsideMap_DoesNotMove()
    {
        ApplicationSessionState exploring =
            ExplorationRules.Turn(
                CreateExplorationSession(),
                ExplorationTurnDirection.Left);
        ExplorationState before =
            AssertExploration(exploring);

        ExplorationMoveResult result =
            ExplorationRules.MoveForward(exploring);
        ExplorationState after =
            AssertExploration(result.State);

        Assert.False(result.DidMove);
        Assert.Equal(before.Position, after.Position);
        Assert.Equal(before.Facing, after.Facing);
    }

    [Fact]
    public void MoveForward_OnUpperFloor_MovesExactlyOneTile()
    {
        ApplicationSessionState upper =
            ExplorationRules.UseStairs(
                CreateAtGroundFloorStairs());
        upper = ExplorationRules.Turn(
            upper,
            ExplorationTurnDirection.Right);

        ExplorationMoveResult result =
            ExplorationRules.MoveForward(upper);
        ExplorationState moved =
            AssertExploration(result.State);

        Assert.True(result.DidMove);
        Assert.Equal(
            ExplorationFloor.UpperFloor,
            moved.Floor);
        Assert.Equal(
            new GridPosition(2, 1),
            moved.Position);
        Assert.Equal(
            ExplorationFacing.South,
            moved.Facing);
    }

    [Fact]
    public void MoveForward_PreservesPersistentStateAndDoesNotMutateInput()
    {
        ApplicationSessionState exploring =
            CreateExplorationSession();
        ExplorationState original =
            AssertExploration(exploring);

        ExplorationMoveResult result =
            ExplorationRules.MoveForward(exploring);

        Assert.Equal(
            WatchtowerScenarioProgress.MissionAccepted,
            result.State.Scenario.Progress);
        Assert.Equal(
            exploring.RandomSeed,
            result.State.RandomSeed);
        Assert.Equal(
            exploring.RandomValuesConsumed,
            result.State.RandomValuesConsumed);
        AssertPartyEquivalent(
            exploring.Party,
            result.State.Party);
        Assert.Equal(
            new GridPosition(0, 0),
            original.Position);
        Assert.Equal(
            new GridPosition(0, 0),
            AssertExploration(exploring).Position);
    }

    [Fact]
    public void MoveForward_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ExplorationRules.MoveForward(null!));
    }

    [Fact]
    public void MoveForward_WhenModeIsNotExploration_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ExplorationRules.MoveForward(
                CreateAcceptedSession()));
    }

    [Fact]
    public void MoveForward_WithMissingExplorationState_Throws()
    {
        AssertInvalidExplorationActionThrows(
            state => ExplorationRules.MoveForward(state),
            exploration => null);
    }

    [Fact]
    public void MoveForward_WithUnsupportedMap_Throws()
    {
        AssertInvalidExplorationActionThrows(
            state => ExplorationRules.MoveForward(state),
            exploration => exploration with
            {
                MapId = "map.unsupported"
            });
    }

    [Fact]
    public void MoveForward_WithUndefinedFloor_Throws()
    {
        AssertInvalidExplorationActionThrows(
            state => ExplorationRules.MoveForward(state),
            exploration => exploration with
            {
                Floor = (ExplorationFloor)999
            });
    }

    [Fact]
    public void MoveForward_WithUndefinedFacing_Throws()
    {
        AssertInvalidExplorationActionThrows(
            state => ExplorationRules.MoveForward(state),
            exploration => exploration with
            {
                Facing = (ExplorationFacing)999
            });
    }

    [Fact]
    public void MoveForward_WithInvalidCurrentPosition_Throws()
    {
        AssertInvalidExplorationActionThrows(
            state => ExplorationRules.MoveForward(state),
            exploration => exploration with
            {
                Position = new GridPosition(99, 99)
            });
    }

    [Fact]
    public void UseStairs_OnGroundFloorStair_TransitionsToUpperFloor()
    {
        ApplicationSessionState atStairs =
            CreateAtGroundFloorStairs();
        ExplorationFacing facing =
            AssertExploration(atStairs).Facing;

        ApplicationSessionState upper =
            ExplorationRules.UseStairs(atStairs);
        ExplorationState exploration =
            AssertExploration(upper);

        Assert.Equal(
            ExplorationFloor.UpperFloor,
            exploration.Floor);
        Assert.Equal(
            new GridPosition(2, 0),
            exploration.Position);
        Assert.Equal(facing, exploration.Facing);
    }

    [Fact]
    public void UseStairs_OnUpperFloorStair_TransitionsToGroundFloor()
    {
        ApplicationSessionState atGroundStairs =
            CreateAtGroundFloorStairs();
        ApplicationSessionState atUpperStairs =
            ExplorationRules.UseStairs(
                atGroundStairs);

        ApplicationSessionState ground =
            ExplorationRules.UseStairs(
                atUpperStairs);
        ExplorationState exploration =
            AssertExploration(ground);

        Assert.Equal(
            ExplorationFloor.GroundFloor,
            exploration.Floor);
        Assert.Equal(
            new GridPosition(2, 0),
            exploration.Position);
    }

    [Fact]
    public void UseStairs_PreservesPersistentStateAndDoesNotMutateInput()
    {
        ApplicationSessionState atStairs =
            CreateAtGroundFloorStairs();
        ExplorationState original =
            AssertExploration(atStairs);

        ApplicationSessionState upper =
            ExplorationRules.UseStairs(atStairs);

        Assert.Equal(atStairs.Scenario, upper.Scenario);
        Assert.Equal(atStairs.RandomSeed, upper.RandomSeed);
        Assert.Equal(
            atStairs.RandomValuesConsumed,
            upper.RandomValuesConsumed);
        AssertPartyEquivalent(
            atStairs.Party,
            upper.Party);
        Assert.Equal(
            ExplorationFloor.GroundFloor,
            original.Floor);
        Assert.Equal(
            new GridPosition(2, 0),
            AssertExploration(atStairs).Position);
    }

    [Fact]
    public void UseStairs_WhenNotOnStair_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ExplorationRules.UseStairs(
                CreateExplorationSession()));
    }

    [Fact]
    public void UseStairs_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ExplorationRules.UseStairs(null!));
    }

    [Fact]
    public void UseStairs_WhenModeIsNotExploration_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ExplorationRules.UseStairs(
                CreateAcceptedSession()));
    }

    [Fact]
    public void UseStairs_WithMissingExplorationState_Throws()
    {
        AssertInvalidExplorationActionThrows(
            state => ExplorationRules.UseStairs(state),
            exploration => null);
    }

    [Fact]
    public void UseStairs_WithInvalidExplorationState_Throws()
    {
        AssertInvalidExplorationActionThrows(
            state => ExplorationRules.UseStairs(state),
            exploration => exploration with
            {
                MapId = "map.unsupported"
            });
    }

    private static void AssertInvalidExplorationActionThrows(
        Action<ApplicationSessionState> action,
        Func<ExplorationState, ExplorationState?>
            changeExploration)
    {
        ApplicationSessionState exploring =
            CreateExplorationSession();
        ExplorationState exploration =
            AssertExploration(exploring);
        ApplicationSessionState invalid =
            exploring with
            {
                Exploration =
                    changeExploration(exploration)
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            action(invalid));
    }

    private static ApplicationSessionState WithFacing(
        ApplicationSessionState session,
        ExplorationFacing facing)
    {
        ExplorationState exploration =
            AssertExploration(session);

        return session with
        {
            Exploration = exploration with
            {
                Facing = facing
            }
        };
    }

    private static ApplicationSessionState
        CreateAtGroundFloorStairs()
    {
        ApplicationSessionState current =
            CreateExplorationSession();

        current = ExplorationRules.MoveForward(current)
            .State;
        current = ExplorationRules.MoveForward(current)
            .State;

        return current;
    }

    private static ApplicationSessionState
        CreateExplorationSession()
    {
        return ExplorationRules.EnterWatchtower(
            CreateCompletedArrival());
    }

    private static ApplicationSessionState
        CreateCompletedArrival()
    {
        ApplicationSessionState current =
            CreateTravelingSession();

        while (!Assert.IsType<RegionalTravelState>(
            current.RegionalTravel).IsComplete)
        {
            current = RegionalTravelRules.Advance(current)
                .State;
        }

        return current;
    }

    private static ApplicationSessionState
        CreateTravelingSession()
    {
        return RegionalTravelRules.BeginWatchtowerJourney(
            CreateAcceptedSession());
    }

    private static ApplicationSessionState
        CreateAcceptedSession()
    {
        return OutpostMissionRules.Resolve(
            CreateMissionNotAcceptedSession() with
            {
                RandomValuesConsumed = 12
            },
            OutpostMissionChoice.AcceptMission)
                .State;
    }

    private static ApplicationSessionState
        CreateMissionNotAcceptedSession()
    {
        return WatchtowerScenarioSessionFactory
            .CreateNew(8675309);
    }

    private static ExplorationState AssertExploration(
        ApplicationSessionState state)
    {
        return Assert.IsType<ExplorationState>(
            state.Exploration);
    }

    private static void AssertPartyEquivalent(
        PartyState expected,
        PartyState actual)
    {
        Assert.Equal(expected.PartyId, actual.PartyId);
        Assert.Equal(
            expected.Members.Count,
            actual.Members.Count);

        for (int index = 0;
            index < expected.Members.Count;
            index++)
        {
            PartyMemberState expectedMember =
                expected.Members[index];
            PartyMemberState actualMember =
                actual.Members[index];

            Assert.Equal(
                expectedMember.PartyMemberId,
                actualMember.PartyMemberId);
            Assert.Equal(
                expectedMember.CharacterDefinitionId,
                actualMember.CharacterDefinitionId);
            Assert.Equal(
                expectedMember.DisplayName,
                actualMember.DisplayName);
            Assert.Equal(
                expectedMember.ClassId,
                actualMember.ClassId);
            Assert.Equal(
                expectedMember.ZeroHitPointPolicy,
                actualMember.ZeroHitPointPolicy);
            Assert.Equal(
                expectedMember.Health,
                actualMember.Health);
            Assert.Equal(
                expectedMember.Ammunition,
                actualMember.Ammunition);
        }
    }
}

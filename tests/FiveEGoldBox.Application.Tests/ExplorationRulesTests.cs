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
    public void CanEnterDestination_AfterCanonicalArrival_ReturnsTrue()
    {
        Assert.True(
            ExplorationRules.CanEnterDestination(
                CreateCompletedArrival()));
    }

    [Fact]
    public void CanEnterDestination_InOrdinarilyUnavailableStates_ReturnsFalse()
    {
        ApplicationSessionState beforeAcceptance =
            CreateMissionNotAcceptedSession();
        ApplicationSessionState beforeArrival =
            CreateTravelingSession();
        ApplicationSessionState exploring =
            CreateExplorationSession();
        ApplicationSessionState encounter =
            ScenarioTriggerRules.Activate(
                WatchtowerSignalTestData
                    .CreateSignalReadySession());
        ApplicationSessionState conclusion =
            CombatOutcomeRules.Finalize(
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
        ApplicationSessionState[] unavailableStates =
        [
            beforeAcceptance,
            beforeArrival,
            exploring,
            encounter,
            conclusion,
            wrongDestination
        ];

        foreach (ApplicationSessionState state
            in unavailableStates)
        {
            Assert.False(
                ExplorationRules.CanEnterDestination(
                    state));
        }
    }

    [Fact]
    public void CanEnterDestination_DoesNotMutateOrConsumeRandomness()
    {
        ApplicationSessionState arrived =
            CreateCompletedArrival();
        RegionalTravelState travel =
            Assert.IsType<RegionalTravelState>(
                arrived.RegionalTravel);

        _ = ExplorationRules.CanEnterDestination(arrived);

        Assert.Equal(ApplicationMode.RegionalTravel, arrived.CurrentMode);
        Assert.True(travel.IsComplete);
        Assert.Null(arrived.Exploration);
        Assert.Equal(8675309, arrived.RandomSeed);
        Assert.Equal(12, arrived.RandomValuesConsumed);
    }

    [Fact]
    public void CanEnterDestination_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ExplorationRules.CanEnterDestination(null!));
    }

    [Fact]
    public void CanEnterDestination_WithMalformedRegionalTravelState_Throws()
    {
        ApplicationSessionState malformed =
            CreateAcceptedSession() with
            {
                CurrentMode = ApplicationMode.RegionalTravel,
                RegionalTravel = null
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            ExplorationRules.CanEnterDestination(
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

        Assert.Equal("GroundFloor", exploration.Floor);
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
    public void EnterDestination_WithCompletedArrival_CreatesAuthoredExplorationState()
    {
        ApplicationSessionState arrived =
            CreateCompletedArrival();

        ApplicationSessionState exploring =
            ExplorationRules.EnterDestination(arrived);
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
            "GroundFloor",
            exploration.Floor);
        Assert.Equal(
            new GridPosition(0, 0),
            exploration.Position);
        Assert.Equal(
            ExplorationFacing.East,
            exploration.Facing);
    }

    [Fact]
    public void EnterDestination_WithCompletedArrival_PreservesPersistentState()
    {
        ApplicationSessionState arrived =
            CreateCompletedArrival();

        ApplicationSessionState exploring =
            ExplorationRules.EnterDestination(arrived);

        Assert.Equal(arrived.ScenarioId, exploring.ScenarioId);
        Assert.Equal(
            WatchtowerScenarioProgress.MissionAccepted,
            WatchtowerScenario.ProgressOf(exploring));
        Assert.Equal(arrived.RandomSeed, exploring.RandomSeed);
        Assert.Equal(
            arrived.RandomValuesConsumed,
            exploring.RandomValuesConsumed);
        AssertPartyEquivalent(
            arrived.Party,
            exploring.Party);
    }

    [Fact]
    public void EnterDestination_DoesNotMutateInputSession()
    {
        ApplicationSessionState arrived =
            CreateCompletedArrival();
        RegionalTravelState travel =
            Assert.IsType<RegionalTravelState>(
                arrived.RegionalTravel);

        _ = ExplorationRules.EnterDestination(arrived);

        Assert.Equal(
            ApplicationMode.RegionalTravel,
            arrived.CurrentMode);
        Assert.Same(travel, arrived.RegionalTravel);
        Assert.Null(arrived.Exploration);
        Assert.Equal(
            WatchtowerScenarioProgress.MissionAccepted,
            WatchtowerScenario.ProgressOf(arrived));
        Assert.Equal(12, arrived.RandomValuesConsumed);
    }

    [Fact]
    public void EnterDestination_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ExplorationRules.EnterDestination(null!));
    }

    [Fact]
    public void EnterDestination_WithInvalidSession_Throws()
    {
        ApplicationSessionState arrived =
            CreateCompletedArrival() with
            {
                ScenarioId = " "
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            ExplorationRules.EnterDestination(arrived));
    }

    [Fact]
    public void EnterDestination_WhenModeIsNotRegionalTravel_Throws()
    {
        ApplicationSessionState accepted =
            CreateAcceptedSession();

        Assert.Throws<InvalidOperationException>(() =>
            ExplorationRules.EnterDestination(accepted));
    }

    [Fact]
    public void EnterDestination_WithMissingTravelState_Throws()
    {
        ApplicationSessionState arrived =
            CreateCompletedArrival() with
            {
                RegionalTravel = null
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            ExplorationRules.EnterDestination(arrived));
    }

    [Fact]
    public void EnterDestination_BeforeRouteCompletion_Throws()
    {
        ApplicationSessionState traveling =
            CreateTravelingSession();

        Assert.Throws<InvalidOperationException>(() =>
            ExplorationRules.EnterDestination(traveling));
    }

    [Fact]
    public void EnterDestination_WithWrongDestination_Throws()
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
            ExplorationRules.EnterDestination(
                reverseArrival));
    }

    [Fact]
    public void EnterDestination_WithWrongCurrentLocation_Throws()
    {
        ApplicationSessionState arrived =
            CreateCompletedArrival() with
            {
                CurrentLocationId = "location.wrong"
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            ExplorationRules.EnterDestination(arrived));
    }

    [Fact]
    public void EnterDestination_WithUnsupportedRoute_Throws()
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
            ExplorationRules.EnterDestination(invalid));
    }

    [Fact]
    public void EnterDestination_BeforeMissionAcceptance_Throws()
    {
        ApplicationSessionState arrived =
            CreateCompletedArrival() with
            {
                Scenario = WatchtowerScenario.CreateState(
WatchtowerScenarioProgress
                            .MissionNotAccepted)
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            ExplorationRules.EnterDestination(arrived));
    }

    [Theory]
    [InlineData("SignalActivated")]
    [InlineData("RaidersDefeated")]
    public void EnterDestination_AfterMissionAcceptedStage_Throws(
        string progressId)
    {
        ApplicationSessionState arrived =
            CreateCompletedArrival() with
            {
                Scenario = new ScenarioState
                {
                    ProgressId = progressId
                }
            };

        // Completed travel state held past the progress its route is open at
        // is malformed rather than merely unavailable, so this is rejected by
        // session validation before entry availability is consulted.
        Assert.ThrowsAny<ArgumentException>(() =>
            ExplorationRules.EnterDestination(arrived));
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
            "GroundFloor",
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
            "UpperFloor",
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
            WatchtowerScenario.ProgressOf(result.State));
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
    public void MoveForward_WithUnknownFloor_Throws()
    {
        AssertInvalidExplorationActionThrows(
            state => ExplorationRules.MoveForward(state),
            exploration => exploration with
            {
                Floor = "floor.unsupported"
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
            "UpperFloor",
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
            "GroundFloor",
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
            "GroundFloor",
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

    [Fact]
    public void CanOpenDoor_WithNothingAheadOfThatKind_ReturnsFalse()
    {
        Assert.False(
            ExplorationRules.CanOpenDoor(
                CreateExplorationSession()));
    }

    [Fact]
    public void CanOpenDoor_FacingALockedDoor_ReturnsFalse()
    {
        Assert.False(
            ExplorationRules.CanOpenDoor(
                CreateFacingSealedPostern()));
    }

    [Fact]
    public void CanOpenDoor_FacingAnUnrevealedSecretDoor_ReturnsFalse()
    {
        Assert.False(
            ExplorationRules.CanOpenDoor(
                CreateFacingHiddenVaultDoor()));
    }

    [Fact]
    public void CanOpenDoor_FacingAnOrdinaryDoor_ReturnsTrue()
    {
        Assert.True(
            ExplorationRules.CanOpenDoor(
                CreateFacingArmoryDoor()));
    }

    [Fact]
    public void CanOpenDoor_AfterTheDoorIsAlreadyOpen_ReturnsFalse()
    {
        ApplicationSessionState opened =
            ExplorationRules.OpenDoor(
                CreateFacingArmoryDoor());

        Assert.False(ExplorationRules.CanOpenDoor(opened));
    }

    [Fact]
    public void CanOpenDoor_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ExplorationRules.CanOpenDoor(null!));
    }

    [Fact]
    public void OpenDoor_OpensTheDoorAndAllowsMovingThroughIt()
    {
        ApplicationSessionState facingDoor =
            CreateFacingArmoryDoor();

        ApplicationSessionState opened =
            ExplorationRules.OpenDoor(facingDoor);

        Assert.Contains(
            "door.watchtower.armory-door",
            AssertExploration(opened).OpenDoorIds);

        ExplorationMoveResult moved =
            ExplorationRules.MoveForward(opened);

        Assert.True(moved.DidMove);
        Assert.Equal(
            new GridPosition(3, 1),
            AssertExploration(moved.State).Position);
    }

    [Fact]
    public void OpenDoor_WhenNoOpenableDoorAhead_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ExplorationRules.OpenDoor(
                CreateExplorationSession()));
    }

    [Fact]
    public void OpenDoor_FacingALockedDoor_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ExplorationRules.OpenDoor(
                CreateFacingSealedPostern()));
    }

    [Fact]
    public void OpenDoor_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ExplorationRules.OpenDoor(null!));
    }

    [Fact]
    public void OpenDoor_WhenModeIsNotExploration_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ExplorationRules.OpenDoor(
                CreateAcceptedSession()));
    }

    [Fact]
    public void CanRevealSecretDoor_FacingASecretDoor_ReturnsTrue()
    {
        Assert.True(
            ExplorationRules.CanRevealSecretDoor(
                CreateFacingHiddenVaultDoor()));
    }

    [Fact]
    public void CanRevealSecretDoor_FacingAnOrdinaryDoor_ReturnsFalse()
    {
        Assert.False(
            ExplorationRules.CanRevealSecretDoor(
                CreateFacingArmoryDoor()));
    }

    [Fact]
    public void CanRevealSecretDoor_AfterAlreadyRevealed_ReturnsFalse()
    {
        ApplicationSessionState revealed =
            ExplorationRules.RevealSecretDoor(
                CreateFacingHiddenVaultDoor());

        Assert.False(
            ExplorationRules.CanRevealSecretDoor(revealed));
    }

    [Fact]
    public void CanRevealSecretDoor_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ExplorationRules.CanRevealSecretDoor(null!));
    }

    [Fact]
    public void RevealSecretDoor_FindsTheDoorAndThenAllowsOpeningIt()
    {
        ApplicationSessionState facingSecretDoor =
            CreateFacingHiddenVaultDoor();

        Assert.False(
            ExplorationRules.CanOpenDoor(facingSecretDoor));

        ApplicationSessionState revealed =
            ExplorationRules.RevealSecretDoor(facingSecretDoor);

        Assert.Contains(
            "door.watchtower.hidden-vault",
            AssertExploration(revealed).RevealedSecretDoorIds);
        Assert.True(ExplorationRules.CanOpenDoor(revealed));
    }

    [Fact]
    public void RevealSecretDoor_WhenNoSecretDoorAhead_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ExplorationRules.RevealSecretDoor(
                CreateFacingArmoryDoor()));
    }

    [Fact]
    public void RevealSecretDoor_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ExplorationRules.RevealSecretDoor(null!));
    }

    [Fact]
    public void CanCollectTreasure_AtTheTreasurePosition_ReturnsTrue()
    {
        Assert.True(
            ExplorationRules.CanCollectTreasure(
                CreateAtArmoryCacheTreasure()));
    }

    [Fact]
    public void CanCollectTreasure_ElsewhereOnTheMap_ReturnsFalse()
    {
        Assert.False(
            ExplorationRules.CanCollectTreasure(
                CreateExplorationSession()));
    }

    [Fact]
    public void CanCollectTreasure_AfterAlreadyCollected_ReturnsFalse()
    {
        ApplicationSessionState collected =
            ExplorationRules.CollectTreasure(
                CreateAtArmoryCacheTreasure());

        Assert.False(
            ExplorationRules.CanCollectTreasure(collected));
    }

    [Fact]
    public void CanCollectTreasure_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ExplorationRules.CanCollectTreasure(null!));
    }

    [Fact]
    public void CollectTreasure_FlagsTheTreasureAsCollectedWithoutGrantingAnything()
    {
        ApplicationSessionState atTreasure =
            CreateAtArmoryCacheTreasure();

        ApplicationSessionState collected =
            ExplorationRules.CollectTreasure(atTreasure);

        Assert.Contains(
            "treasure.watchtower.armory-cache",
            AssertExploration(collected).CollectedTreasureIds);
        AssertPartyEquivalent(
            atTreasure.Party,
            collected.Party);
    }

    [Fact]
    public void CollectTreasure_CollectingTheSameTreasureTwice_Throws()
    {
        ApplicationSessionState collected =
            ExplorationRules.CollectTreasure(
                CreateAtArmoryCacheTreasure());

        Assert.Throws<InvalidOperationException>(() =>
            ExplorationRules.CollectTreasure(collected));
    }

    [Fact]
    public void CollectTreasure_WhenNoTreasureAtThePosition_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ExplorationRules.CollectTreasure(
                CreateExplorationSession()));
    }

    [Fact]
    public void CollectTreasure_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ExplorationRules.CollectTreasure(null!));
    }

    [Fact]
    public void Query_DuringExploration_ReturnsRealMapData()
    {
        ApplicationSessionState session = CreateExplorationSession();

        ExplorationMapView? view = ExplorationRules.Query(session);

        Assert.NotNull(view);
        Assert.Equal("map.ruined-watchtower", view!.MapId);
        Assert.Equal("GroundFloor", view.Floor);
        Assert.Equal(5, view.Width);
        Assert.Equal(3, view.Height);
        Assert.Equal(new GridPosition(0, 0), view.PartyPosition);
        Assert.Equal(ExplorationFacing.East, view.PartyFacing);
        Assert.Equal(
            new HashSet<GridPosition>
            {
                new(0, 0), new(1, 0), new(2, 0),
                new(0, 1), new(2, 1),
                new(0, 2), new(1, 2), new(2, 2),
                new(4, 1), new(4, 2)
            },
            view.TraversablePositions.ToHashSet());
        Assert.Equal(
            new[] { new GridPosition(2, 0) },
            view.StairPositions);
    }

    [Fact]
    public void Query_AfterMovingAndTurning_ReflectsRealPositionAndFacing()
    {
        ApplicationSessionState session = CreateExplorationSession();
        session = ExplorationRules.MoveForward(session).State;
        session = ExplorationRules.Turn(
            session,
            ExplorationTurnDirection.Right);

        ExplorationMapView? view = ExplorationRules.Query(session);

        Assert.NotNull(view);
        Assert.Equal(new GridPosition(1, 0), view!.PartyPosition);
        Assert.Equal(ExplorationFacing.South, view.PartyFacing);
    }

    [Fact]
    public void Query_AfterUsingStairs_ReflectsTheDestinationFloorOnly()
    {
        ApplicationSessionState atStairs = CreateAtGroundFloorStairs();
        ApplicationSessionState upper =
            ExplorationRules.UseStairs(atStairs);

        ExplorationMapView? view = ExplorationRules.Query(upper);

        Assert.NotNull(view);
        Assert.Equal("UpperFloor", view!.Floor);
        Assert.Equal(
            new HashSet<GridPosition>
            {
                new(0, 0), new(1, 0), new(2, 0),
                new(0, 1), new(1, 1), new(2, 1)
            },
            view.TraversablePositions.ToHashSet());
    }

    [Fact]
    public void Query_OutsideExploration_ReturnsNull()
    {
        Assert.Null(
            ExplorationRules.Query(CreateMissionNotAcceptedSession()));
        Assert.Null(
            ExplorationRules.Query(CreateTravelingSession()));
    }

    [Fact]
    public void Query_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ExplorationRules.Query(null!));
    }

    [Fact]
    public void Query_FreshSession_ClassifiesDoorsAndTreasureIntoTheirBuckets()
    {
        ExplorationMapView? view =
            ExplorationRules.Query(CreateExplorationSession());

        Assert.NotNull(view);
        Assert.Contains(
            new GridPosition(3, 1),
            view!.ClosedDoorPositions);
        Assert.Contains(
            new GridPosition(1, 1),
            view.LockedDoorPositions);

        // The hidden vault (3, 2) is an unrevealed secret door: invisible.
        // It appears in no list at all, including TraversablePositions.
        Assert.DoesNotContain(
            new GridPosition(3, 2),
            view.ClosedDoorPositions);
        Assert.DoesNotContain(
            new GridPosition(3, 2),
            view.LockedDoorPositions);
        Assert.DoesNotContain(
            new GridPosition(3, 2),
            view.TraversablePositions);

        Assert.Contains(
            new GridPosition(4, 1),
            view.TreasurePositions);
    }

    [Fact]
    public void Query_AfterOpeningAnOrdinaryDoor_MovesItIntoTraversablePositions()
    {
        ApplicationSessionState opened =
            ExplorationRules.OpenDoor(
                CreateFacingArmoryDoor());

        ExplorationMapView? view = ExplorationRules.Query(opened);

        Assert.NotNull(view);
        Assert.Contains(
            new GridPosition(3, 1),
            view!.TraversablePositions);
        Assert.DoesNotContain(
            new GridPosition(3, 1),
            view.ClosedDoorPositions);
    }

    [Fact]
    public void Query_AfterRevealingASecretDoorWithoutOpeningIt_ShowsItAsClosed()
    {
        ApplicationSessionState revealed =
            ExplorationRules.RevealSecretDoor(
                CreateFacingHiddenVaultDoor());

        ExplorationMapView? view = ExplorationRules.Query(revealed);

        Assert.NotNull(view);
        Assert.Contains(
            new GridPosition(3, 2),
            view!.ClosedDoorPositions);
        Assert.DoesNotContain(
            new GridPosition(3, 2),
            view.TraversablePositions);
    }

    [Fact]
    public void Query_AfterCollectingTreasure_RemovesItFromTreasurePositions()
    {
        ApplicationSessionState collected =
            ExplorationRules.CollectTreasure(
                CreateAtArmoryCacheTreasure());

        ExplorationMapView? view = ExplorationRules.Query(collected);

        Assert.NotNull(view);
        Assert.DoesNotContain(
            new GridPosition(4, 1),
            view!.TreasurePositions);
    }

    [Fact]
    public void Query_TheLockedPosternNeverAppearsAsClosedOrTraversable_AcrossStateChanges()
    {
        ApplicationSessionState fresh = CreateExplorationSession();
        ApplicationSessionState doorOpened =
            ExplorationRules.OpenDoor(
                CreateFacingArmoryDoor());
        ApplicationSessionState secretRevealed =
            ExplorationRules.RevealSecretDoor(
                CreateFacingHiddenVaultDoor());
        ApplicationSessionState treasureCollected =
            ExplorationRules.CollectTreasure(
                CreateAtArmoryCacheTreasure());

        foreach (ApplicationSessionState state
            in new[] { fresh, doorOpened, secretRevealed, treasureCollected })
        {
            ExplorationMapView? view = ExplorationRules.Query(state);

            Assert.NotNull(view);
            Assert.Contains(
                new GridPosition(1, 1),
                view!.LockedDoorPositions);
            Assert.DoesNotContain(
                new GridPosition(1, 1),
                view.ClosedDoorPositions);
            Assert.DoesNotContain(
                new GridPosition(1, 1),
                view.TraversablePositions);
        }
    }

    [Fact]
    public void Query_DoesNotMutateOrConsumeRandomness()
    {
        ApplicationSessionState session = CreateExplorationSession();
        ExplorationState original = AssertExploration(session);
        int randomValuesConsumedBefore = session.RandomValuesConsumed;

        _ = ExplorationRules.Query(session);

        Assert.Equal(
            original,
            AssertExploration(session));
        Assert.Equal(
            randomValuesConsumedBefore,
            session.RandomValuesConsumed);
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

    /// Facing the ordinary door at (3, 1) on the Watchtower's ground floor,
    /// from the traversable ring square at (2, 1).
    private static ApplicationSessionState
        CreateFacingArmoryDoor()
    {
        ApplicationSessionState current =
            CreateExplorationSession();

        current = ExplorationRules.MoveForward(current).State;
        current = ExplorationRules.MoveForward(current).State;
        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Right);
        current = ExplorationRules.MoveForward(current).State;
        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Left);

        return current;
    }

    /// Facing the secret door at (3, 2) on the Watchtower's ground floor,
    /// from the traversable ring square at (2, 2).
    private static ApplicationSessionState
        CreateFacingHiddenVaultDoor()
    {
        ApplicationSessionState current =
            CreateExplorationSession();

        current = ExplorationRules.MoveForward(current).State;
        current = ExplorationRules.MoveForward(current).State;
        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Right);
        current = ExplorationRules.MoveForward(current).State;
        current = ExplorationRules.MoveForward(current).State;
        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Left);

        return current;
    }

    /// Facing the locked door at (1, 1) -- the Watchtower ground floor's own
    /// interior gap -- from the traversable square at (1, 0).
    private static ApplicationSessionState
        CreateFacingSealedPostern()
    {
        ApplicationSessionState current =
            CreateExplorationSession();

        current = ExplorationRules.MoveForward(current).State;
        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Right);

        return current;
    }

    /// Standing on the armory cache treasure at (4, 1), reachable only by
    /// opening the ordinary door at (3, 1) first.
    private static ApplicationSessionState
        CreateAtArmoryCacheTreasure()
    {
        ApplicationSessionState current =
            ExplorationRules.OpenDoor(
                CreateFacingArmoryDoor());

        current = ExplorationRules.MoveForward(current).State;
        current = ExplorationRules.MoveForward(current).State;

        return current;
    }

    private static ApplicationSessionState
        CreateExplorationSession()
    {
        return ExplorationRules.EnterDestination(
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
        return RegionalTravelRules.BeginJourney(
            CreateAcceptedSession());
    }

    private static ApplicationSessionState
        CreateAcceptedSession()
    {
        return OutpostDecisionRules.Resolve(
            CreateMissionNotAcceptedSession() with
            {
                RandomValuesConsumed = 12
            },
            "AcceptMission")
                .State;
    }

    private static ApplicationSessionState
        CreateMissionNotAcceptedSession()
    {
        return ScenarioSessionFactory
            .CreateNew(
                WatchtowerScenarioContent.ScenarioId,
                8675309);
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

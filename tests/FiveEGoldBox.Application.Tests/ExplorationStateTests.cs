using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class ExplorationStateTests
{
    [Fact]
    public void Validate_WithValidGroundFloorState_Accepts()
    {
        ApplicationSessionRules.Validate(
            CreateExplorationSession());
    }

    [Fact]
    public void Validate_WithValidUpperFloorState_Accepts()
    {
        ApplicationSessionState upper =
            ExplorationRules.UseStairs(
                CreateAtGroundFloorStairs());

        ApplicationSessionRules.Validate(upper);
    }

    [Fact]
    public void Validate_ExplorationModeWithoutExplorationState_Throws()
    {
        ApplicationSessionState invalid =
            CreateExplorationSession() with
            {
                Exploration = null
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            ApplicationSessionRules.Validate(invalid));
    }

    [Fact]
    public void Validate_ExplorationModeWithRegionalTravelState_Throws()
    {
        ApplicationSessionState arrived =
            CreateCompletedArrival();
        ApplicationSessionState exploring =
            ExplorationRules.EnterWatchtower(arrived);
        ApplicationSessionState invalid =
            exploring with
            {
                RegionalTravel = arrived.RegionalTravel
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            ApplicationSessionRules.Validate(invalid));
    }

    [Fact]
    public void Validate_OutpostModeWithExplorationState_Throws()
    {
        ApplicationSessionState exploring =
            CreateExplorationSession();
        ApplicationSessionState invalid =
            exploring with
            {
                CurrentMode = ApplicationMode.Outpost
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            ApplicationSessionRules.Validate(invalid));
    }

    [Fact]
    public void Validate_RegionalTravelModeWithExplorationState_Throws()
    {
        ApplicationSessionState traveling =
            CreateTravelingSession();
        ApplicationSessionState exploring =
            CreateExplorationSession();
        ApplicationSessionState invalid =
            traveling with
            {
                Exploration = exploring.Exploration
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            ApplicationSessionRules.Validate(invalid));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithBlankMapId_Throws(
        string? mapId)
    {
        AssertInvalidExplorationThrows(
            exploration => exploration with
            {
                MapId = mapId!
            });
    }

    [Fact]
    public void Validate_WithUnsupportedMapId_Throws()
    {
        AssertInvalidExplorationThrows(
            exploration => exploration with
            {
                MapId = "map.unsupported"
            });
    }

    [Fact]
    public void Validate_WithUndefinedFloor_Throws()
    {
        AssertInvalidExplorationThrows(
            exploration => exploration with
            {
                Floor = (ExplorationFloor)999
            });
    }

    [Fact]
    public void Validate_WithUndefinedFacing_Throws()
    {
        AssertInvalidExplorationThrows(
            exploration => exploration with
            {
                Facing = (ExplorationFacing)999
            });
    }

    [Fact]
    public void Validate_WithPositionOutsideMap_Throws()
    {
        AssertInvalidExplorationThrows(
            exploration => exploration with
            {
                Position = new GridPosition(99, 99)
            });
    }

    [Fact]
    public void Validate_WithPositionOnBlockedTile_Throws()
    {
        AssertInvalidExplorationThrows(
            exploration => exploration with
            {
                Position = new GridPosition(1, 1)
            });
    }

    [Fact]
    public void Validate_WithCurrentLocationOtherThanWatchtower_Throws()
    {
        ApplicationSessionState invalid =
            CreateExplorationSession() with
            {
                CurrentLocationId = "location.outpost"
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            ApplicationSessionRules.Validate(invalid));
    }

    [Theory]
    [InlineData(
        WatchtowerScenarioProgress.MissionNotAccepted)]
    [InlineData(
        WatchtowerScenarioProgress.SignalActivated)]
    [InlineData(
        WatchtowerScenarioProgress.RaidersDefeated)]
    [InlineData(
        WatchtowerScenarioProgress.SuccessReported)]
    [InlineData(
        WatchtowerScenarioProgress.ScenarioCompleted)]
    public void Validate_OutsideMissionAcceptedProgress_Throws(
        WatchtowerScenarioProgress progress)
    {
        ApplicationSessionState invalid =
            CreateExplorationSession() with
            {
                Scenario = new WatchtowerScenarioState
                {
                    Progress = progress
                }
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            ApplicationSessionRules.Validate(invalid));
    }

    private static void AssertInvalidExplorationThrows(
        Func<ExplorationState, ExplorationState>
            changeExploration)
    {
        ApplicationSessionState exploring =
            CreateExplorationSession();
        ExplorationState exploration =
            Assert.IsType<ExplorationState>(
                exploring.Exploration);
        ApplicationSessionState invalid =
            exploring with
            {
                Exploration =
                    changeExploration(exploration)
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            ApplicationSessionRules.Validate(invalid));
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
            CreateMissionNotAcceptedSession(),
            OutpostMissionChoice.AcceptMission)
                .State;
    }

    private static ApplicationSessionState
        CreateMissionNotAcceptedSession()
    {
        PartyMemberState[] members =
        [
            CreateMember(
                "party-member.fighter",
                "character.fighter",
                "Fighter",
                "class.fighter",
                12),
            CreateMember(
                "party-member.barbarian",
                "character.barbarian",
                "Barbarian",
                "class.barbarian",
                14),
            CreateMember(
                "party-member.ranger",
                "character.ranger",
                "Ranger",
                "class.ranger",
                11) with
            {
                Ammunition = new AmmunitionState
                {
                    WeaponId = "weapon.longbow",
                    AmmunitionItemId = "item.arrow",
                    RemainingQuantity = 7
                }
            }
        ];

        return ApplicationSessionRules.CreateNew(
            scenarioId: "scenario.watchtower",
            currentLocationId: "location.outpost",
            party: new PartyState
            {
                PartyId = "party.player",
                Members = members
            },
            randomSeed: 8675309);
    }

    private static PartyMemberState CreateMember(
        string partyMemberId,
        string characterDefinitionId,
        string displayName,
        string classId,
        int maximumHitPoints)
    {
        return new PartyMemberState
        {
            PartyMemberId = partyMemberId,
            CharacterDefinitionId =
                characterDefinitionId,
            DisplayName = displayName,
            ClassId = classId,
            ZeroHitPointPolicy =
                CombatantZeroHitPointPolicy
                    .DeathSavingThrows,
            Health = CombatantHealthRules.Create(
                maximumHitPoints),
            Ammunition = null
        };
    }
}

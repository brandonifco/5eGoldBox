using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class RegionalTravelRulesTests
{
    [Fact]
    public void BeginWatchtowerJourney_WithAcceptedMission_CreatesFixedRouteState()
    {
        ApplicationSessionState session =
            CreateAcceptedSession();

        ApplicationSessionState traveling =
            RegionalTravelRules.BeginWatchtowerJourney(
                session);

        RegionalTravelState travel =
            Assert.IsType<RegionalTravelState>(
                traveling.RegionalTravel);

        Assert.Equal(
            ApplicationMode.RegionalTravel,
            traveling.CurrentMode);
        Assert.Equal(
            session.CurrentLocationId,
            traveling.CurrentLocationId);
        Assert.Equal(
            "route.outpost-watchtower",
            travel.RouteId);
        Assert.Equal(
            session.CurrentLocationId,
            travel.OriginLocationId);
        Assert.Equal(
            "location.ruined-watchtower",
            travel.DestinationLocationId);
        Assert.Equal(0, travel.CurrentStepIndex);
        Assert.True(travel.FinalStepIndex > 1);
        Assert.False(travel.IsComplete);
    }

    [Fact]
    public void BeginWatchtowerJourney_WithAcceptedMission_PreservesPersistentState()
    {
        ApplicationSessionState session =
            CreateAcceptedSession();

        ApplicationSessionState traveling =
            RegionalTravelRules.BeginWatchtowerJourney(
                session);

        Assert.Equal(
            WatchtowerScenarioProgress.MissionAccepted,
            traveling.Scenario.Progress);
        Assert.Equal(session.ScenarioId, traveling.ScenarioId);
        Assert.Equal(session.RandomSeed, traveling.RandomSeed);
        Assert.Equal(
            session.RandomValuesConsumed,
            traveling.RandomValuesConsumed);
        AssertPartyEquivalent(
            session.Party,
            traveling.Party);
    }

    [Fact]
    public void BeginWatchtowerJourney_DoesNotMutateInputSession()
    {
        ApplicationSessionState session =
            CreateAcceptedSession();

        _ = RegionalTravelRules.BeginWatchtowerJourney(
            session);

        Assert.Equal(
            ApplicationMode.Outpost,
            session.CurrentMode);
        Assert.Equal(
            "location.outpost",
            session.CurrentLocationId);
        Assert.Null(session.RegionalTravel);
        Assert.Equal(
            WatchtowerScenarioProgress.MissionAccepted,
            session.Scenario.Progress);
        Assert.Equal(12, session.RandomValuesConsumed);
    }

    [Fact]
    public void BeginWatchtowerJourney_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            RegionalTravelRules.BeginWatchtowerJourney(
                null!));
    }

    [Fact]
    public void BeginWatchtowerJourney_WithInvalidSession_Throws()
    {
        ApplicationSessionState session =
            CreateAcceptedSession() with
            {
                ScenarioId = " "
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            RegionalTravelRules.BeginWatchtowerJourney(
                session));
    }

    [Fact]
    public void BeginWatchtowerJourney_WithBlankOutpostLocation_Throws()
    {
        ApplicationSessionState session =
            CreateAcceptedSession() with
            {
                CurrentLocationId = " "
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            RegionalTravelRules.BeginWatchtowerJourney(
                session));
    }

    [Fact]
    public void BeginWatchtowerJourney_BeforeMissionAcceptance_Throws()
    {
        ApplicationSessionState session =
            CreateMissionNotAcceptedSession();

        Assert.Throws<InvalidOperationException>(() =>
            RegionalTravelRules.BeginWatchtowerJourney(
                session));
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
    public void BeginWatchtowerJourney_AfterMissionAcceptedStage_Throws(
        WatchtowerScenarioProgress progress)
    {
        ApplicationSessionState session =
            CreateAcceptedSession() with
            {
                Scenario = new WatchtowerScenarioState
                {
                    Progress = progress
                }
            };

        Assert.Throws<InvalidOperationException>(() =>
            RegionalTravelRules.BeginWatchtowerJourney(
                session));
    }

    [Fact]
    public void BeginWatchtowerJourney_WhenModeIsNotOutpost_Throws()
    {
        ApplicationSessionState traveling =
            RegionalTravelRules.BeginWatchtowerJourney(
                CreateAcceptedSession());

        Assert.Throws<InvalidOperationException>(() =>
            RegionalTravelRules.BeginWatchtowerJourney(
                traveling));
    }

    [Fact]
    public void BeginWatchtowerJourney_WithExistingTravelState_Throws()
    {
        ApplicationSessionState traveling =
            RegionalTravelRules.BeginWatchtowerJourney(
                CreateAcceptedSession());
        ApplicationSessionState invalid =
            CreateAcceptedSession() with
            {
                RegionalTravel = traveling.RegionalTravel
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            RegionalTravelRules.BeginWatchtowerJourney(
                invalid));
    }

    [Fact]
    public void Advance_WithValidJourney_AdvancesExactlyOneStep()
    {
        ApplicationSessionState traveling =
            CreateTravelingSession();
        RegionalTravelState originalTravel =
            Assert.IsType<RegionalTravelState>(
                traveling.RegionalTravel);

        RegionalTravelAdvanceResult result =
            RegionalTravelRules.Advance(traveling);
        RegionalTravelState advancedTravel =
            Assert.IsType<RegionalTravelState>(
                result.State.RegionalTravel);

        Assert.Equal(
            originalTravel.CurrentStepIndex + 1,
            advancedTravel.CurrentStepIndex);
        Assert.Equal(
            originalTravel.FinalStepIndex,
            advancedTravel.FinalStepIndex);
        Assert.False(result.DidArrive);
    }

    [Fact]
    public void Advance_BeforeFinalStep_PreservesPersistentState()
    {
        ApplicationSessionState traveling =
            CreateTravelingSession();

        ApplicationSessionState advanced =
            RegionalTravelRules.Advance(traveling)
                .State;

        Assert.Equal(
            WatchtowerScenarioProgress.MissionAccepted,
            advanced.Scenario.Progress);
        Assert.Equal(
            traveling.CurrentLocationId,
            advanced.CurrentLocationId);
        Assert.Equal(
            traveling.RandomSeed,
            advanced.RandomSeed);
        Assert.Equal(
            traveling.RandomValuesConsumed,
            advanced.RandomValuesConsumed);
        AssertPartyEquivalent(
            traveling.Party,
            advanced.Party);
    }

    [Fact]
    public void Advance_OnFinalStep_ReportsArrivalAndUpdatesLocation()
    {
        ApplicationSessionState traveling =
            CreateTravelingSession();
        RegionalTravelState travel =
            Assert.IsType<RegionalTravelState>(
                traveling.RegionalTravel);
        ApplicationSessionState beforeArrival =
            AdvanceToStep(
                traveling,
                travel.FinalStepIndex - 1);

        RegionalTravelAdvanceResult result =
            RegionalTravelRules.Advance(beforeArrival);
        RegionalTravelState completedTravel =
            Assert.IsType<RegionalTravelState>(
                result.State.RegionalTravel);

        Assert.True(result.DidArrive);
        Assert.Equal(
            ApplicationMode.RegionalTravel,
            result.State.CurrentMode);
        Assert.Equal(
            completedTravel.DestinationLocationId,
            result.State.CurrentLocationId);
        Assert.Equal(
            completedTravel.FinalStepIndex,
            completedTravel.CurrentStepIndex);
        Assert.True(completedTravel.IsComplete);
        Assert.Equal(
            WatchtowerScenarioProgress.MissionAccepted,
            result.State.Scenario.Progress);
        AssertPartyEquivalent(
            beforeArrival.Party,
            result.State.Party);
        Assert.Equal(
            beforeArrival.RandomValuesConsumed,
            result.State.RandomValuesConsumed);
    }

    [Fact]
    public void Advance_DoesNotMutateInputSession()
    {
        ApplicationSessionState traveling =
            CreateTravelingSession();
        RegionalTravelState originalTravel =
            Assert.IsType<RegionalTravelState>(
                traveling.RegionalTravel);

        _ = RegionalTravelRules.Advance(traveling);

        Assert.Equal(0, originalTravel.CurrentStepIndex);
        Assert.Equal(
            "location.outpost",
            traveling.CurrentLocationId);
        Assert.False(originalTravel.IsComplete);
    }

    [Fact]
    public void Advance_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            RegionalTravelRules.Advance(null!));
    }

    [Fact]
    public void Advance_WhenModeIsNotRegionalTravel_Throws()
    {
        ApplicationSessionState session =
            CreateAcceptedSession();

        Assert.Throws<InvalidOperationException>(() =>
            RegionalTravelRules.Advance(session));
    }

    [Fact]
    public void Advance_WithMissingTravelState_Throws()
    {
        ApplicationSessionState session =
            CreateAcceptedSession() with
            {
                CurrentMode =
                    ApplicationMode.RegionalTravel
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            RegionalTravelRules.Advance(session));
    }

    [Fact]
    public void Advance_WithMissionNotAccepted_Throws()
    {
        ApplicationSessionState traveling =
            CreateTravelingSession() with
            {
                Scenario = new WatchtowerScenarioState
                {
                    Progress =
                        WatchtowerScenarioProgress
                            .MissionNotAccepted
                }
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            RegionalTravelRules.Advance(traveling));
    }

    [Fact]
    public void Advance_WithUnsupportedRouteIdentity_Throws()
    {
        AssertInvalidTravelThrows(travel =>
            travel with
            {
                RouteId = "route.unsupported"
            });
    }

    [Theory]
    [InlineData(
        "location.one",
        "location.two")]
    [InlineData(
        "",
        "location.ruined-watchtower")]
    [InlineData(
        "location.outpost",
        "")]
    [InlineData(
        "location.ruined-watchtower",
        "location.ruined-watchtower")]
    public void Advance_WithInconsistentRouteEndpoints_Throws(
        string originLocationId,
        string destinationLocationId)
    {
        AssertInvalidTravelThrows(travel =>
            travel with
            {
                OriginLocationId = originLocationId,
                DestinationLocationId =
                    destinationLocationId
            });
    }

    [Fact]
    public void Advance_WithNegativeCurrentStep_Throws()
    {
        AssertInvalidTravelThrows(travel =>
            travel with
            {
                CurrentStepIndex = -1
            });
    }

    [Fact]
    public void Advance_WithStepBeyondFinalStep_Throws()
    {
        AssertInvalidTravelThrows(travel =>
            travel with
            {
                CurrentStepIndex =
                    travel.FinalStepIndex + 1
            });
    }

    [Fact]
    public void Advance_WithInconsistentFinalStep_Throws()
    {
        AssertInvalidTravelThrows(travel =>
            travel with
            {
                FinalStepIndex =
                    travel.FinalStepIndex + 1
            });
    }

    [Fact]
    public void Advance_WithLocationInconsistentWithProgress_Throws()
    {
        ApplicationSessionState traveling =
            CreateTravelingSession() with
            {
                CurrentLocationId =
                    "location.ruined-watchtower"
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            RegionalTravelRules.Advance(traveling));
    }

    [Fact]
    public void Advance_AfterArrival_Throws()
    {
        ApplicationSessionState completed =
            AdvanceToCompletion(
                CreateTravelingSession());

        Assert.Throws<InvalidOperationException>(() =>
            RegionalTravelRules.Advance(completed));
    }

    [Fact]
    public void Validate_WithReversedRouteEndpoints_AcceptsBoundedRouteShape()
    {
        ApplicationSessionState outbound =
            CreateTravelingSession();
        RegionalTravelState outboundTravel =
            Assert.IsType<RegionalTravelState>(
                outbound.RegionalTravel);
        ApplicationSessionState reverse =
            outbound with
            {
                CurrentLocationId =
                    outboundTravel.DestinationLocationId,
                RegionalTravel = outboundTravel with
                {
                    OriginLocationId =
                        outboundTravel
                            .DestinationLocationId,
                    DestinationLocationId =
                        outboundTravel.OriginLocationId,
                    CurrentStepIndex = 0
                }
            };

        ApplicationSessionRules.Validate(reverse);
    }

    private static void AssertInvalidTravelThrows(
        Func<RegionalTravelState, RegionalTravelState>
            changeTravel)
    {
        ApplicationSessionState traveling =
            CreateTravelingSession();
        RegionalTravelState travel =
            Assert.IsType<RegionalTravelState>(
                traveling.RegionalTravel);
        ApplicationSessionState invalid =
            traveling with
            {
                RegionalTravel = changeTravel(travel)
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            RegionalTravelRules.Advance(invalid));
    }

    private static ApplicationSessionState
        AdvanceToCompletion(
            ApplicationSessionState session)
    {
        RegionalTravelState travel =
            Assert.IsType<RegionalTravelState>(
                session.RegionalTravel);

        return AdvanceToStep(
            session,
            travel.FinalStepIndex);
    }

    private static ApplicationSessionState AdvanceToStep(
        ApplicationSessionState session,
        int targetStepIndex)
    {
        ApplicationSessionState current = session;

        while (Assert.IsType<RegionalTravelState>(
            current.RegionalTravel).CurrentStepIndex
            < targetStepIndex)
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
        ApplicationSessionState session =
            CreateMissionNotAcceptedSession() with
            {
                RandomValuesConsumed = 12
            };

        return OutpostMissionRules.Resolve(
            session,
            OutpostMissionChoice.AcceptMission)
                .State;
    }

    private static ApplicationSessionState
        CreateMissionNotAcceptedSession()
    {
        PartyMemberState[] members =
        [
            CreateMember(
                partyMemberId:
                    "party-member.fighter",
                characterDefinitionId:
                    "character.fighter",
                displayName: "Fighter",
                classId: "class.fighter",
                maximumHitPoints: 12) with
            {
                Health = CombatantHealthRules.Create(
                    maximumHitPoints: 12) with
                {
                    HitPoints = new HitPointState
                    {
                        MaximumHitPoints = 12,
                        CurrentHitPoints = 8,
                        TemporaryHitPoints = 2
                    }
                }
            },
            CreateMember(
                partyMemberId:
                    "party-member.barbarian",
                characterDefinitionId:
                    "character.barbarian",
                displayName: "Barbarian",
                classId: "class.barbarian",
                maximumHitPoints: 14),
            CreateMember(
                partyMemberId:
                    "party-member.ranger",
                characterDefinitionId:
                    "character.ranger",
                displayName: "Ranger",
                classId: "class.ranger",
                maximumHitPoints: 11) with
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

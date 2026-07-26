using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;

namespace FiveEGoldBox.Application.Tests;

public sealed class RegionalTravelRulesTests
{
    [Fact]
    public void CanBeginJourney_InCanonicalAcceptedState_ReturnsTrue()
    {
        Assert.True(
            RegionalTravelRules.CanBeginJourney(
                CreateAcceptedSession()));
    }

    [Fact]
    public void CanBeginJourney_InOrdinarilyUnavailableStates_ReturnsFalse()
    {
        ApplicationSessionState beforeAcceptance =
            CreateMissionNotAcceptedSession();
        ApplicationSessionState traveling =
            CreateTravelingSession();
        ApplicationSessionState wrongLocation =
            CreateAcceptedSession() with
            {
                CurrentLocationId = "location.other"
            };
        ApplicationSessionState incompatibleProgress =
            CreateAcceptedSession() with
            {
                Scenario = WatchtowerScenario.CreateState(
WatchtowerScenarioProgress
                            .SignalActivated)
            };

        Assert.False(
            RegionalTravelRules.CanBeginJourney(
                beforeAcceptance));
        Assert.False(
            RegionalTravelRules.CanBeginJourney(
                traveling));
        Assert.False(
            RegionalTravelRules.CanBeginJourney(
                wrongLocation));
        Assert.False(
            RegionalTravelRules.CanBeginJourney(
                incompatibleProgress));
    }

    [Fact]
    public void CanBeginJourney_DoesNotMutateOrConsumeRandomness()
    {
        ApplicationSessionState session =
            CreateAcceptedSession();

        _ = RegionalTravelRules.CanBeginJourney(
            session);

        Assert.Equal(ApplicationMode.Outpost, session.CurrentMode);
        Assert.Null(session.RegionalTravel);
        Assert.Equal(8675309, session.RandomSeed);
        Assert.Equal(12, session.RandomValuesConsumed);
    }

    [Fact]
    public void CanBeginJourney_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            RegionalTravelRules.CanBeginJourney(
                null!));
    }

    [Fact]
    public void CanBeginJourney_WithMalformedOutpostState_Throws()
    {
        ApplicationSessionState traveling =
            CreateTravelingSession();
        ApplicationSessionState malformed =
            CreateAcceptedSession() with
            {
                RegionalTravel = traveling.RegionalTravel
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            RegionalTravelRules.CanBeginJourney(
                malformed));
    }

    [Fact]
    public void BeginJourney_WithWrongLocation_StillRejectsAuthoritatively()
    {
        ApplicationSessionState session =
            CreateAcceptedSession() with
            {
                CurrentLocationId = "location.other"
            };

        Assert.False(
            RegionalTravelRules.CanBeginJourney(
                session));
        Assert.Throws<InvalidOperationException>(() =>
            RegionalTravelRules.BeginJourney(
                session));
    }

    [Fact]
    public void CanAdvance_ReturnsTrueAtEveryIncompleteStepAndFalseAfterArrival()
    {
        ApplicationSessionState current =
            CreateTravelingSession();

        while (!Assert.IsType<RegionalTravelState>(
            current.RegionalTravel).IsComplete)
        {
            Assert.True(RegionalTravelRules.CanAdvance(current));
            current = RegionalTravelRules.Advance(current)
                .State;
        }

        Assert.False(RegionalTravelRules.CanAdvance(current));
    }

    [Fact]
    public void CanAdvance_InOrdinarilyUnavailableStates_ReturnsFalse()
    {
        ApplicationSessionState beforeStart =
            CreateAcceptedSession();

        Assert.False(
            RegionalTravelRules.CanAdvance(beforeStart));
    }

    [Fact]
    public void CanAdvance_DoesNotMutateOrConsumeRandomness()
    {
        ApplicationSessionState traveling =
            CreateTravelingSession();
        RegionalTravelState travel =
            Assert.IsType<RegionalTravelState>(
                traveling.RegionalTravel);

        _ = RegionalTravelRules.CanAdvance(traveling);

        Assert.Equal(0, travel.CurrentStepIndex);
        Assert.Equal(8675309, traveling.RandomSeed);
        Assert.Equal(12, traveling.RandomValuesConsumed);
    }

    [Fact]
    public void CanAdvance_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            RegionalTravelRules.CanAdvance(null!));
    }

    [Fact]
    public void CanAdvance_WithMalformedRegionalTravelState_Throws()
    {
        ApplicationSessionState malformed =
            CreateAcceptedSession() with
            {
                CurrentMode = ApplicationMode.RegionalTravel,
                RegionalTravel = null
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            RegionalTravelRules.CanAdvance(malformed));
    }

    /// Holding travel state at progress the route is not open at is a
    /// malformed session rather than an unavailable one: the route gates the
    /// whole journey, so there is no way to have legitimately got here.
    [Fact]
    public void Advance_WithIncompatibleProgress_RejectsSessionAsMalformed()
    {
        ApplicationSessionState traveling =
            CreateTravelingSession() with
            {
                Scenario = WatchtowerScenario.CreateState(
WatchtowerScenarioProgress
                            .SignalActivated)
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            RegionalTravelRules.CanAdvance(traveling));
        Assert.ThrowsAny<ArgumentException>(() =>
            RegionalTravelRules.Advance(traveling));
    }

    [Fact]
    public void BeginJourney_WithAcceptedMission_CreatesFixedRouteState()
    {
        ApplicationSessionState session =
            CreateAcceptedSession();

        ApplicationSessionState traveling =
            RegionalTravelRules.BeginJourney(
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
    public void BeginJourney_WithAcceptedMission_PreservesPersistentState()
    {
        ApplicationSessionState session =
            CreateAcceptedSession();

        ApplicationSessionState traveling =
            RegionalTravelRules.BeginJourney(
                session);

        Assert.Equal(
            WatchtowerScenarioProgress.MissionAccepted,
            WatchtowerScenario.ProgressOf(traveling));
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
    public void BeginJourney_DoesNotMutateInputSession()
    {
        ApplicationSessionState session =
            CreateAcceptedSession();

        _ = RegionalTravelRules.BeginJourney(
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
            WatchtowerScenario.ProgressOf(session));
        Assert.Equal(12, session.RandomValuesConsumed);
    }

    [Fact]
    public void BeginJourney_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            RegionalTravelRules.BeginJourney(
                null!));
    }

    [Fact]
    public void BeginJourney_WithInvalidSession_Throws()
    {
        ApplicationSessionState session =
            CreateAcceptedSession() with
            {
                ScenarioId = " "
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            RegionalTravelRules.BeginJourney(
                session));
    }

    [Fact]
    public void BeginJourney_WithBlankOutpostLocation_Throws()
    {
        ApplicationSessionState session =
            CreateAcceptedSession() with
            {
                CurrentLocationId = " "
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            RegionalTravelRules.BeginJourney(
                session));
    }

    [Fact]
    public void BeginJourney_BeforeMissionAcceptance_Throws()
    {
        ApplicationSessionState session =
            CreateMissionNotAcceptedSession();

        Assert.Throws<InvalidOperationException>(() =>
            RegionalTravelRules.BeginJourney(
                session));
    }

    [Theory]
    [InlineData("SignalActivated")]
    [InlineData("RaidersDefeated")]
    [InlineData("SuccessReported")]
    [InlineData("ScenarioCompleted")]
    public void BeginJourney_AfterMissionAcceptedStage_Throws(
        string progressId)
    {
        ApplicationSessionState session =
            CreateAcceptedSession() with
            {
                Scenario = new ScenarioState
                {
                    ProgressId = progressId
                }
            };

        Assert.Throws<InvalidOperationException>(() =>
            RegionalTravelRules.BeginJourney(
                session));
    }

    [Fact]
    public void BeginJourney_WhenModeIsNotOutpost_Throws()
    {
        ApplicationSessionState traveling =
            RegionalTravelRules.BeginJourney(
                CreateAcceptedSession());

        Assert.Throws<InvalidOperationException>(() =>
            RegionalTravelRules.BeginJourney(
                traveling));
    }

    [Fact]
    public void BeginJourney_WithExistingTravelState_Throws()
    {
        ApplicationSessionState traveling =
            RegionalTravelRules.BeginJourney(
                CreateAcceptedSession());
        ApplicationSessionState invalid =
            CreateAcceptedSession() with
            {
                RegionalTravel = traveling.RegionalTravel
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            RegionalTravelRules.BeginJourney(
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
            WatchtowerScenario.ProgressOf(advanced));
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
            WatchtowerScenario.ProgressOf(result.State));
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
                Scenario = WatchtowerScenario.CreateState(
WatchtowerScenarioProgress
                            .MissionNotAccepted)
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
        return RegionalTravelRules.BeginJourney(
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
        return ScenarioSessionFactory
            .CreateNew(
                WatchtowerScenarioContent.ScenarioId,
                8675309);
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

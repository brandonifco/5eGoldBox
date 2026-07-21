using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;

namespace FiveEGoldBox.Application.Tests;

public sealed class OutpostMissionRulesTests
{
    [Fact]
    public void GetAvailableChoices_InCanonicalDecisionState_ReturnsStableOrderedChoices()
    {
        ApplicationSessionState session =
            CreateValidSession();

        IReadOnlyList<OutpostMissionChoice> choices =
            OutpostMissionRules.GetAvailableChoices(
                session);

        OutpostMissionChoice[] expected =
        [
            OutpostMissionChoice.AcceptMission,
            OutpostMissionChoice.NotYet
        ];

        Assert.Equal(expected, choices);
        Assert.Equal(choices.Count, choices.Distinct().Count());
    }

    [Fact]
    public void GetAvailableChoices_ReturnsReadOnlyCollection()
    {
        IReadOnlyList<OutpostMissionChoice> choices =
            OutpostMissionRules.GetAvailableChoices(
                CreateValidSession());
        IList<OutpostMissionChoice> mutableView =
            Assert.IsAssignableFrom<IList<OutpostMissionChoice>>(
                choices);

        Assert.True(mutableView.IsReadOnly);
        Assert.Throws<NotSupportedException>(() =>
            mutableView.Add(
                OutpostMissionChoice.AcceptMission));
    }

    [Fact]
    public void GetAvailableChoices_RepeatedDiscoveryIsValueEquivalent()
    {
        ApplicationSessionState session =
            CreateValidSession();

        IReadOnlyList<OutpostMissionChoice> first =
            OutpostMissionRules.GetAvailableChoices(
                session);
        IReadOnlyList<OutpostMissionChoice> second =
            OutpostMissionRules.GetAvailableChoices(
                session);

        Assert.Equal(first.ToArray(), second.ToArray());
    }

    [Fact]
    public void GetAvailableChoices_DoesNotMutateOrConsumeRandomness()
    {
        ApplicationSessionState session =
            CreateValidSession();

        _ = OutpostMissionRules.GetAvailableChoices(
            session);

        Assert.Equal(ApplicationMode.Outpost, session.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.MissionNotAccepted,
            session.Scenario.Progress);
        Assert.Null(session.RegionalTravel);
        Assert.Null(session.Exploration);
        Assert.Null(session.ActiveEncounter);
        Assert.Equal(8675309, session.RandomSeed);
        Assert.Equal(12, session.RandomValuesConsumed);
    }

    [Fact]
    public void GetAvailableChoices_WhenDecisionIsUnavailable_ReturnsEmpty()
    {
        ApplicationSessionState accepted =
            OutpostMissionRules.Resolve(
                CreateValidSession(),
                OutpostMissionChoice.AcceptMission)
                .State;
        ApplicationSessionState traveling =
            RegionalTravelRules.BeginWatchtowerJourney(
                accepted);
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

        ApplicationSessionState[] unavailableStates =
        [
            accepted,
            traveling,
            exploring,
            encounter,
            conclusion
        ];

        foreach (ApplicationSessionState state
            in unavailableStates)
        {
            Assert.Empty(
                OutpostMissionRules
                    .GetAvailableChoices(state));
        }
    }

    [Fact]
    public void GetAvailableChoices_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            OutpostMissionRules.GetAvailableChoices(
                null!));
    }

    [Fact]
    public void GetAvailableChoices_WithMalformedOutpostState_Throws()
    {
        ApplicationSessionState traveling =
            RegionalTravelRules.BeginWatchtowerJourney(
                OutpostMissionRules.Resolve(
                    CreateValidSession(),
                    OutpostMissionChoice.AcceptMission)
                    .State);
        ApplicationSessionState malformed =
            CreateValidSession() with
            {
                RegionalTravel = traveling.RegionalTravel
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            OutpostMissionRules.GetAvailableChoices(
                malformed));
    }

    [Fact]
    public void Resolve_WithAcceptMission_AdvancesProgressAndReportsResult()
    {
        ApplicationSessionState session =
            CreateValidSession();

        OutpostMissionResult result =
            OutpostMissionRules.Resolve(
                session,
                OutpostMissionChoice.AcceptMission);

        Assert.Equal(
            OutpostMissionChoice.AcceptMission,
            result.Choice);
        Assert.True(result.DidProgressChange);
        Assert.Equal(
            WatchtowerScenarioProgress.MissionAccepted,
            result.State.Scenario.Progress);
        Assert.Equal(
            ApplicationMode.Outpost,
            result.State.CurrentMode);
    }

    [Fact]
    public void Resolve_WithAcceptMission_PreservesUnrelatedState()
    {
        ApplicationSessionState session =
            CreateValidSession();

        ApplicationSessionState accepted =
            OutpostMissionRules.Resolve(
                session,
                OutpostMissionChoice.AcceptMission)
                .State;

        Assert.Equal(session.ScenarioId, accepted.ScenarioId);
        Assert.Equal(
            session.CurrentLocationId,
            accepted.CurrentLocationId);
        Assert.Equal(
            session.RandomSeed,
            accepted.RandomSeed);
        Assert.Equal(
            session.RandomValuesConsumed,
            accepted.RandomValuesConsumed);
        AssertPartyEquivalent(
            session.Party,
            accepted.Party);
    }

    [Fact]
    public void Resolve_WithAcceptMission_DoesNotMutateInputSession()
    {
        ApplicationSessionState session =
            CreateValidSession();

        _ = OutpostMissionRules.Resolve(
            session,
            OutpostMissionChoice.AcceptMission);

        Assert.Equal(
            WatchtowerScenarioProgress
                .MissionNotAccepted,
            session.Scenario.Progress);
        Assert.Equal(
            ApplicationMode.Outpost,
            session.CurrentMode);
        Assert.Equal(12, session.RandomValuesConsumed);
    }

    [Fact]
    public void Resolve_WithNotYet_LeavesSessionSemanticallyUnchanged()
    {
        ApplicationSessionState session =
            CreateValidSession();

        OutpostMissionResult result =
            OutpostMissionRules.Resolve(
                session,
                OutpostMissionChoice.NotYet);

        Assert.Equal(
            OutpostMissionChoice.NotYet,
            result.Choice);
        Assert.False(result.DidProgressChange);
        AssertSessionEquivalent(
            session,
            result.State);
    }

    [Fact]
    public void Resolve_WithNotYet_AllowsLaterMissionAcceptance()
    {
        ApplicationSessionState session =
            CreateValidSession();
        ApplicationSessionState deferred =
            OutpostMissionRules.Resolve(
                session,
                OutpostMissionChoice.NotYet)
                .State;

        OutpostMissionResult accepted =
            OutpostMissionRules.Resolve(
                deferred,
                OutpostMissionChoice.AcceptMission);

        Assert.Equal(
            WatchtowerScenarioProgress.MissionAccepted,
            accepted.State.Scenario.Progress);
        Assert.True(accepted.DidProgressChange);
    }

    [Fact]
    public void Resolve_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            OutpostMissionRules.Resolve(
                null!,
                OutpostMissionChoice.AcceptMission));
    }

    [Fact]
    public void Resolve_WithUndefinedChoice_Throws()
    {
        ApplicationSessionState session =
            CreateValidSession();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OutpostMissionRules.Resolve(
                session,
                (OutpostMissionChoice)999));
    }

    [Fact]
    public void Resolve_WithInvalidSession_Throws()
    {
        ApplicationSessionState session =
            CreateValidSession() with
            {
                ScenarioId = " "
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            OutpostMissionRules.Resolve(
                session,
                OutpostMissionChoice.AcceptMission));
    }

    [Fact]
    public void Resolve_WithNullScenario_Throws()
    {
        ApplicationSessionState session =
            CreateValidSession() with
            {
                Scenario = null!
            };

        Assert.Throws<ArgumentNullException>(() =>
            OutpostMissionRules.Resolve(
                session,
                OutpostMissionChoice.AcceptMission));
    }

    [Fact]
    public void Resolve_WhenModeIsNotOutpost_Throws()
    {
        ApplicationSessionState session =
            CreateValidSession() with
            {
                CurrentMode = ApplicationMode.Exploration
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            OutpostMissionRules.Resolve(
                session,
                OutpostMissionChoice.AcceptMission));
    }

    [Theory]
    [InlineData(
        OutpostMissionChoice.AcceptMission,
        WatchtowerScenarioProgress.MissionAccepted)]
    [InlineData(
        OutpostMissionChoice.AcceptMission,
        WatchtowerScenarioProgress.SignalActivated)]
    [InlineData(
        OutpostMissionChoice.AcceptMission,
        WatchtowerScenarioProgress.RaidersDefeated)]
    [InlineData(
        OutpostMissionChoice.AcceptMission,
        WatchtowerScenarioProgress.SuccessReported)]
    [InlineData(
        OutpostMissionChoice.AcceptMission,
        WatchtowerScenarioProgress.ScenarioCompleted)]
    [InlineData(
        OutpostMissionChoice.NotYet,
        WatchtowerScenarioProgress.MissionAccepted)]
    [InlineData(
        OutpostMissionChoice.NotYet,
        WatchtowerScenarioProgress.SignalActivated)]
    [InlineData(
        OutpostMissionChoice.NotYet,
        WatchtowerScenarioProgress.RaidersDefeated)]
    [InlineData(
        OutpostMissionChoice.NotYet,
        WatchtowerScenarioProgress.SuccessReported)]
    [InlineData(
        OutpostMissionChoice.NotYet,
        WatchtowerScenarioProgress.ScenarioCompleted)]
    public void Resolve_AfterMissionDecisionAvailability_Throws(
        OutpostMissionChoice choice,
        WatchtowerScenarioProgress progress)
    {
        ApplicationSessionState session =
            CreateValidSession() with
            {
                Scenario = new WatchtowerScenarioState
                {
                    Progress = progress
                }
            };

        Assert.Throws<InvalidOperationException>(() =>
            OutpostMissionRules.Resolve(
                session,
                choice));
    }

    private static void AssertSessionEquivalent(
        ApplicationSessionState expected,
        ApplicationSessionState actual)
    {
        Assert.Equal(expected.ScenarioId, actual.ScenarioId);
        Assert.Equal(expected.CurrentMode, actual.CurrentMode);
        Assert.Equal(
            expected.CurrentLocationId,
            actual.CurrentLocationId);
        Assert.Equal(
            expected.Scenario.Progress,
            actual.Scenario.Progress);
        Assert.Equal(expected.RandomSeed, actual.RandomSeed);
        Assert.Equal(
            expected.RandomValuesConsumed,
            actual.RandomValuesConsumed);
        AssertPartyEquivalent(
            expected.Party,
            actual.Party);
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

    private static ApplicationSessionState
        CreateValidSession()
    {
        return WatchtowerScenarioSessionFactory
            .CreateNew(8675309) with
        {
            RandomValuesConsumed = 12
        };
    }

    private static ApplicationSessionState
        CreateExplorationSession()
    {
        ApplicationSessionState current =
            RegionalTravelRules.BeginWatchtowerJourney(
                OutpostMissionRules.Resolve(
                    CreateValidSession(),
                    OutpostMissionChoice.AcceptMission)
                    .State);

        while (!Assert.IsType<RegionalTravelState>(
            current.RegionalTravel).IsComplete)
        {
            current = RegionalTravelRules.Advance(current)
                .State;
        }

        return ExplorationRules.EnterWatchtower(current);
    }
}

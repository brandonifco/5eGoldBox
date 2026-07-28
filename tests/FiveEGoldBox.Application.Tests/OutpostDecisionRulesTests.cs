using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;

namespace FiveEGoldBox.Application.Tests;

public sealed class OutpostDecisionRulesTests
{
    private const string AcceptMission = "AcceptMission";

    private const string NotYet = "NotYet";

    [Fact]
    public void GetAvailableOptionIds_InCanonicalDecisionState_ReturnsStableOrderedOptions()
    {
        ApplicationSessionState session =
            CreateValidSession();

        IReadOnlyList<string> optionIds =
            OutpostDecisionRules.GetAvailableOptionIds(
                session);

        string[] expected = [AcceptMission, NotYet];

        Assert.Equal(expected, optionIds);
        Assert.Equal(optionIds.Count, optionIds.Distinct().Count());
    }

    [Fact]
    public void GetAvailableOptionIds_ReturnsReadOnlyCollection()
    {
        IReadOnlyList<string> optionIds =
            OutpostDecisionRules.GetAvailableOptionIds(
                CreateValidSession());
        IList<string> mutableView =
            Assert.IsAssignableFrom<IList<string>>(
                optionIds);

        Assert.True(mutableView.IsReadOnly);
        Assert.Throws<NotSupportedException>(() =>
            mutableView.Add(AcceptMission));
    }

    [Fact]
    public void GetAvailableOptionIds_RepeatedDiscoveryIsValueEquivalent()
    {
        ApplicationSessionState session =
            CreateValidSession();

        IReadOnlyList<string> first =
            OutpostDecisionRules.GetAvailableOptionIds(
                session);
        IReadOnlyList<string> second =
            OutpostDecisionRules.GetAvailableOptionIds(
                session);

        Assert.Equal(first.ToArray(), second.ToArray());
    }

    [Fact]
    public void GetAvailableOptionIds_DoesNotMutateOrConsumeRandomness()
    {
        ApplicationSessionState session =
            CreateValidSession();

        _ = OutpostDecisionRules.GetAvailableOptionIds(
            session);

        Assert.Equal(ApplicationMode.Outpost, session.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.MissionNotAccepted,
            WatchtowerScenario.ProgressOf(session));
        Assert.Null(session.RegionalTravel);
        Assert.Null(session.Exploration);
        Assert.Null(session.ActiveEncounter);
        Assert.Equal(8675309, session.RandomSeed);
        Assert.Equal(12, session.RandomValuesConsumed);
    }

    [Fact]
    public void GetAvailableOptionIds_WhenDecisionIsUnavailable_ReturnsEmpty()
    {
        ApplicationSessionState accepted =
            OutpostDecisionRules.Resolve(
                CreateValidSession(),
                AcceptMission)
                .State;
        ApplicationSessionState traveling =
            RegionalTravelRules.BeginJourney(
                accepted);
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
                OutpostDecisionRules
                    .GetAvailableOptionIds(state));
        }
    }

    [Fact]
    public void GetAvailableOptionIds_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            OutpostDecisionRules.GetAvailableOptionIds(
                null!));
    }

    [Fact]
    public void GetAvailableOptionIds_WithMalformedOutpostState_Throws()
    {
        ApplicationSessionState traveling =
            RegionalTravelRules.BeginJourney(
                OutpostDecisionRules.Resolve(
                    CreateValidSession(),
                    AcceptMission)
                    .State);
        ApplicationSessionState malformed =
            CreateValidSession() with
            {
                RegionalTravel = traveling.RegionalTravel
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            OutpostDecisionRules.GetAvailableOptionIds(
                malformed));
    }

    [Fact]
    public void Resolve_WithAcceptMission_AdvancesProgressAndReportsResult()
    {
        ApplicationSessionState session =
            CreateValidSession();

        OutpostDecisionResult result =
            OutpostDecisionRules.Resolve(
                session,
                AcceptMission);

        Assert.Equal(AcceptMission, result.OptionId);
        Assert.True(result.DidProgressChange);
        Assert.Equal(
            WatchtowerScenarioProgress.MissionAccepted,
            WatchtowerScenario.ProgressOf(result.State));
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
            OutpostDecisionRules.Resolve(
                session,
                AcceptMission)
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

        _ = OutpostDecisionRules.Resolve(
            session,
            AcceptMission);

        Assert.Equal(
            WatchtowerScenarioProgress
                .MissionNotAccepted,
            WatchtowerScenario.ProgressOf(session));
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

        OutpostDecisionResult result =
            OutpostDecisionRules.Resolve(
                session,
                NotYet);

        Assert.Equal(NotYet, result.OptionId);
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
            OutpostDecisionRules.Resolve(
                session,
                NotYet)
                .State;

        OutpostDecisionResult accepted =
            OutpostDecisionRules.Resolve(
                deferred,
                AcceptMission);

        Assert.Equal(
            WatchtowerScenarioProgress.MissionAccepted,
            WatchtowerScenario.ProgressOf(accepted.State));
        Assert.True(accepted.DidProgressChange);
    }

    [Fact]
    public void Resolve_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            OutpostDecisionRules.Resolve(
                null!,
                AcceptMission));
    }

    [Fact]
    public void Resolve_WithNullOptionId_Throws()
    {
        ApplicationSessionState session =
            CreateValidSession();

        Assert.Throws<ArgumentException>(() =>
            OutpostDecisionRules.Resolve(
                session,
                null!));
    }

    /// Not an enum with a closed set of members any more — an option ID that
    /// simply isn't on offer right now is rejected the same way a route ID
    /// nothing declared is: authoritatively, not by parsing.
    [Fact]
    public void Resolve_WithUnavailableOptionId_Throws()
    {
        ApplicationSessionState session =
            CreateValidSession();

        Assert.Throws<InvalidOperationException>(() =>
            OutpostDecisionRules.Resolve(
                session,
                "not-a-real-option"));
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
            OutpostDecisionRules.Resolve(
                session,
                AcceptMission));
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
            OutpostDecisionRules.Resolve(
                session,
                AcceptMission));
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
            OutpostDecisionRules.Resolve(
                session,
                AcceptMission));
    }

    [Theory]
    [InlineData(AcceptMission, "MissionAccepted")]
    [InlineData(AcceptMission, "SignalActivated")]
    [InlineData(AcceptMission, "RaidersDefeated")]
    [InlineData(NotYet, "MissionAccepted")]
    [InlineData(NotYet, "SignalActivated")]
    [InlineData(NotYet, "RaidersDefeated")]
    public void Resolve_AfterMissionDecisionAvailability_Throws(
        string optionId,
        string progressId)
    {
        ApplicationSessionState session =
            CreateValidSession() with
            {
                Scenario = new ScenarioState
                {
                    ProgressId = progressId
                }
            };

        Assert.Throws<InvalidOperationException>(() =>
            OutpostDecisionRules.Resolve(
                session,
                optionId));
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
            WatchtowerScenario.ProgressOf(expected),
            WatchtowerScenario.ProgressOf(actual));
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
        return ScenarioSessionFactory
            .CreateNew(
                WatchtowerScenarioContent.ScenarioId,
                8675309) with
        {
            RandomValuesConsumed = 12
        };
    }

    private static ApplicationSessionState
        CreateExplorationSession()
    {
        ApplicationSessionState current =
            RegionalTravelRules.BeginJourney(
                OutpostDecisionRules.Resolve(
                    CreateValidSession(),
                    AcceptMission)
                    .State);

        while (!Assert.IsType<RegionalTravelState>(
            current.RegionalTravel).IsComplete)
        {
            current = RegionalTravelRules.Advance(current)
                .State;
        }

        return ExplorationRules.EnterDestination(current);
    }
}

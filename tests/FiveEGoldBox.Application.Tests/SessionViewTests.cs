using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Application.Views;

namespace FiveEGoldBox.Application.Tests;

/// The view exists so a client does not have to work out which of six rule
/// classes to ask what, and then invent its own words for the answers. These
/// run it against both scenarios without naming either.
public sealed class SessionViewTests
{
    [Theory]
    [InlineData(WatchtowerScenarioContent.ScenarioId)]
    [InlineData(SunkenChapelScenarioDefinitionProvider.ScenarioId)]
    public void Describe_AtTheStart_OffersTheScenariosOwnDecision(
        string scenarioId)
    {
        SessionViewModel view = SessionView.Describe(
            ScenarioSessionFactory.CreateNew(scenarioId, randomSeed: 7));

        Assert.Equal(ApplicationMode.Outpost, view.Mode);
        Assert.False(view.IsConcluded);
        Assert.Null(view.IsSuccess);

        // Every offered option is one the decision authored, named as the
        // scenario names it.
        Assert.All(view.Actions, action =>
            Assert.Equal(
                SessionActionKind.ResolveOutpostDecision,
                action.Kind));
        Assert.All(view.Actions, action =>
            Assert.NotNull(action.MissionChoice));
        Assert.Contains(
            view.Actions,
            action => action.MissionChoice == OutpostMissionChoice.AcceptMission);
        Assert.Contains(
            view.Actions,
            action => action.MissionChoice == OutpostMissionChoice.NotYet);
    }

    /// The two scenarios describe the same moment in their own words, which is
    /// the whole point of reading the names off content.
    [Fact]
    public void Describe_UsesEachScenariosOwnNames()
    {
        SessionViewModel watchtower = SessionView.Describe(
            ScenarioSessionFactory.CreateNew(
                WatchtowerScenarioContent.ScenarioId,
                randomSeed: 7));
        SessionViewModel chapel = SessionView.Describe(
            ScenarioSessionFactory.CreateNew(
                SunkenChapelScenarioDefinitionProvider.ScenarioId,
                randomSeed: 7));

        Assert.Equal("The Ruined Watchtower", watchtower.ScenarioDisplayName);
        Assert.Equal("Frontier Outpost", watchtower.LocationDisplayName);

        Assert.Equal("The Sunken Chapel", chapel.ScenarioDisplayName);
        Assert.Equal("Harbor Town", chapel.LocationDisplayName);

        Assert.NotEqual(
            watchtower.Actions.Select(action => action.DisplayName),
            chapel.Actions.Select(action => action.DisplayName));
    }

    [Fact]
    public void Describe_AfterAcceptingTheCommission_OffersTheRoad()
    {
        SessionViewModel view = SessionView.Describe(Accept(
            ScenarioSessionFactory.CreateNew(
                SunkenChapelScenarioDefinitionProvider.ScenarioId,
                randomSeed: 7)));

        SessionAction journey = Assert.Single(
            view.Actions,
            action => action.Kind == SessionActionKind.BeginJourney);

        // Named from the route's destination rather than from a constant.
        Assert.Equal("Set out for The Sunken Chapel", journey.DisplayName);
    }

    [Fact]
    public void Describe_OnArrival_OffersTheWayIn()
    {
        ApplicationSessionState session = Arrive(
            SunkenChapelScenarioDefinitionProvider.ScenarioId);

        SessionViewModel view = SessionView.Describe(session);

        Assert.Equal(ApplicationMode.RegionalTravel, view.Mode);
        Assert.Equal(
            "Enter The Sunken Chapel",
            Assert.Single(
                view.Actions,
                action => action.Kind == SessionActionKind.EnterDestination)
                .DisplayName);
    }

    [Fact]
    public void Describe_WhileExploring_OffersMovementAndAnyTriggerHere()
    {
        ApplicationSessionState session = ExplorationRules.EnterDestination(
            Arrive(SunkenChapelScenarioDefinitionProvider.ScenarioId));

        SessionViewModel standingAway = SessionView.Describe(session);

        Assert.Contains(
            standingAway.Actions,
            action => action.Kind == SessionActionKind.MoveForward);
        Assert.Contains(
            standingAway.Actions,
            action => action.Kind == SessionActionKind.TurnLeft);
        Assert.DoesNotContain(
            standingAway.Actions,
            action => action.Kind == SessionActionKind.ActivateTrigger);

        SessionViewModel onTheSeal = SessionView.Describe(
            ExplorationRules.MoveForward(session).State);

        // The trigger is named by the scenario, not by the client.
        Assert.Equal(
            "Tide seal",
            Assert.Single(
                onTheSeal.Actions,
                action => action.Kind == SessionActionKind.ActivateTrigger)
                .DisplayName);
    }

    /// A concluded scenario offers nothing further, and says how it ended.
    [Fact]
    public void Describe_AtAWinningConclusion_ReportsSuccessAndOffersNothing()
    {
        SessionViewModel view = SessionView.Describe(
            SecondScenarioTests.RunToConclusion());

        Assert.True(view.IsConcluded);
        Assert.True(view.IsSuccess);
        Assert.Empty(view.Actions);
    }

    [Fact]
    public void Describe_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SessionView.Describe(null!));
    }

    private static ApplicationSessionState Accept(
        ApplicationSessionState session)
    {
        return OutpostMissionRules.Resolve(
            session,
            OutpostMissionChoice.AcceptMission)
            .State;
    }

    private static ApplicationSessionState Arrive(
        string scenarioId)
    {
        ApplicationSessionState session = RegionalTravelRules.BeginJourney(
            Accept(ScenarioSessionFactory.CreateNew(scenarioId, randomSeed: 7)));

        while (RegionalTravelRules.CanAdvance(session))
        {
            session = RegionalTravelRules.Advance(session).State;
        }

        return session;
    }
}

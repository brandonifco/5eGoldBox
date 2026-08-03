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
    [InlineData(SunkenChapelScenarioIds.ScenarioId)]
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
            Assert.NotNull(action.DecisionOptionId));
        Assert.Contains(
            view.Actions,
            action => action.DecisionOptionId == "AcceptMission");
        Assert.Contains(
            view.Actions,
            action => action.DecisionOptionId == "NotYet");
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
                SunkenChapelScenarioIds.ScenarioId,
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
                SunkenChapelScenarioIds.ScenarioId,
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
            SunkenChapelScenarioIds.ScenarioId);

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
            Arrive(SunkenChapelScenarioIds.ScenarioId));

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

    [Fact]
    public void Describe_AtTheStart_ReportsAnEmptyPurse()
    {
        SessionViewModel view = SessionView.Describe(
            ScenarioSessionFactory.CreateNew(
                WatchtowerScenarioContent.ScenarioId,
                randomSeed: 7));

        Assert.Equal(0, view.Party.Currency.CopperPieces);
        Assert.Equal(0, view.Party.Currency.SilverPieces);
        Assert.Equal(0, view.Party.Currency.ElectrumPieces);
        Assert.Equal(0, view.Party.Currency.GoldPieces);
        Assert.Equal(0, view.Party.Currency.PlatinumPieces);
        Assert.Empty(view.Party.InventoryItems);
    }

    /// The roster, class names, and hit points a real Character screen
    /// needs -- resolved from the real campaign roster and ruleset, not a
    /// synthetic fixture.
    [Fact]
    public void Describe_ReportsTheRealPartyRosterWithResolvedClassNamesAndHitPoints()
    {
        SessionViewModel view = SessionView.Describe(
            ScenarioSessionFactory.CreateNew(
                WatchtowerScenarioContent.ScenarioId,
                randomSeed: 7));

        Assert.NotEmpty(view.Party.Members);

        Assert.All(view.Party.Members, member =>
        {
            Assert.False(string.IsNullOrWhiteSpace(member.PartyMemberId));
            Assert.False(string.IsNullOrWhiteSpace(member.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(member.ClassDisplayName));
            Assert.True(member.MaximumHitPoints > 0);
            Assert.InRange(
                member.CurrentHitPoints,
                0,
                member.MaximumHitPoints);
        });

        Assert.Equal(
            view.Party.Members.Count,
            view.Party.Members.Select(member => member.PartyMemberId)
                .Distinct(StringComparer.Ordinal)
                .Count());
    }

    /// The Watchtower's armory cache (25 gold, 10 arrows) is real content
    /// coming through real rules, not a synthetic fixture -- proof this
    /// closes the loop end to end: collect via ExplorationRules, then read
    /// it back through the exact view a client renders from.
    [Fact]
    public void Describe_AfterCollectingTreasure_ReflectsTheGrantedCurrencyAndItem()
    {
        ApplicationSessionState atTreasure = AtArmoryCacheTreasure();

        ApplicationSessionState collected =
            ExplorationRules.CollectTreasure(atTreasure);

        SessionViewModel view = SessionView.Describe(collected);

        Assert.Equal(25, view.Party.Currency.GoldPieces);

        Assert.Equal(
            "Arrow",
            Assert.Single(
                view.Party.InventoryItems,
                item => item.ItemId == "item.arrow")
                .DisplayName);
        Assert.Equal(
            10,
            Assert.Single(
                view.Party.InventoryItems,
                item => item.ItemId == "item.arrow")
                .Quantity);
    }

    /// Mirrors ExplorationRulesTests' own CreateAtArmoryCacheTreasure --
    /// standing on the armory cache treasure at (4, 1), reachable only by
    /// opening the ordinary door at (3, 1) first.
    private static ApplicationSessionState AtArmoryCacheTreasure()
    {
        ApplicationSessionState current = ExplorationRules.EnterDestination(
            Arrive(WatchtowerScenarioContent.ScenarioId));

        current = ExplorationRules.MoveForward(current).State;
        current = ExplorationRules.MoveForward(current).State;
        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Right);
        current = ExplorationRules.MoveForward(current).State;
        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Left);
        current = ExplorationRules.OpenDoor(current);
        current = ExplorationRules.MoveForward(current).State;
        current = ExplorationRules.MoveForward(current).State;

        return current;
    }

    private static ApplicationSessionState Accept(
        ApplicationSessionState session)
    {
        return OutpostDecisionRules.Resolve(
            session,
            "AcceptMission")
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

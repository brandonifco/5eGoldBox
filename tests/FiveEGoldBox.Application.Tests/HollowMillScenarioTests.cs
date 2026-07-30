using FiveEGoldBox.Application.Campaigns;
using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Application.Views;
using FiveEGoldBox.Core.Runtime;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Application.Tests;

/// A third scenario authored to be worth playing, not just to prove the
/// engine runs on content — that case was already made by
/// <see cref="SecondScenarioTests"/>. What this scenario adds is a real
/// branch with mechanical weight, so these tests exist to prove the branch
/// actually works: each path is reachable, they cannot both fire in the
/// same playthrough, and each leads to the encounter and outcome it should.
public sealed class HollowMillScenarioTests
{
    private const int RandomSeed = 99;

    [Fact]
    public void Definition_IsValidAuthoredContent()
    {
        ValidationResult validation =
            ScenarioDefinitionValidator.Validate(
                HollowMillScenarioDefinitionProvider.Create());

        Assert.True(validation.IsValid);
        Assert.Empty(validation.Issues);
    }

    [Fact]
    public void CreateNew_StartsWhereTheScenarioDeclares()
    {
        ApplicationSessionState session = CreateSession();

        Assert.Equal(
            HollowMillScenarioDefinitionProvider.ScenarioId,
            session.ScenarioId);
        Assert.Equal(
            HollowMillScenarioDefinitionProvider.VillageLocationId,
            session.CurrentLocationId);
        Assert.Equal(
            HollowMillScenarioDefinitionProvider.RumorHeard,
            session.Scenario.ProgressId);
        Assert.Equal(ApplicationMode.Outpost, session.CurrentMode);
    }

    /// The two cellar triggers occupy the same square. This is the
    /// structural guarantee that makes that safe: their required-progress
    /// sets share no marker, so at most one of them can ever be the trigger
    /// <see cref="ScenarioTriggerMatcher"/> hands back.
    [Fact]
    public void TheTwoCellarTriggers_HaveDisjointRequiredProgress()
    {
        ScenarioDefinition definition =
            HollowMillScenarioDefinitionProvider.Create();

        ScenarioTriggerDefinition informed = definition.Triggers.Single(
            trigger => trigger.TriggerId == "trigger.cellar-informed-approach");
        ScenarioTriggerDefinition blind = definition.Triggers.Single(
            trigger => trigger.TriggerId == "trigger.cellar-blind-approach");

        Assert.Equal(informed.LocationId, blind.LocationId);
        Assert.Equal(informed.Floor, blind.Floor);
        Assert.Equal(informed.Position, blind.Position);
        Assert.Empty(informed.RequiredProgressIds.Intersect(
            blind.RequiredProgressIds,
            StringComparer.Ordinal));
    }

    /// Stopping to consult the herbalist first is rewarded: only the rat
    /// swarm is roused, and the party carries the fight to a clean success.
    [Fact]
    public void InformedBranch_FacesOnlyTheSwarm_AndReachesSuccess()
    {
        ApplicationSessionState session = CreateSession();

        session = OutpostDecisionRules.Resolve(
            session,
            "AcceptMission")
            .State;
        Assert.Equal(
            HollowMillScenarioDefinitionProvider.CommissionAccepted,
            session.Scenario.ProgressId);

        session = TravelToTheMill(session);
        session = ExplorationRules.EnterDestination(session);
        Assert.Equal(ApplicationMode.Exploration, session.CurrentMode);
        Assert.Equal(
            new GridPosition(1, 0),
            session.Exploration!.Position);

        // West door: consult Old Rosa.
        session = Forward(session);
        Assert.Equal(new GridPosition(1, 1), session.Exploration!.Position);
        session = Turn(session, ExplorationTurnDirection.Right);
        session = Forward(session);
        Assert.Equal(new GridPosition(0, 1), session.Exploration!.Position);

        Assert.True(ScenarioTriggerRules.CanActivate(session));
        session = ScenarioTriggerRules.Activate(session);
        Assert.Equal(
            HollowMillScenarioDefinitionProvider.HerbalistConsulted,
            session.Scenario.ProgressId);
        Assert.Equal(ApplicationMode.Exploration, session.CurrentMode);

        // Down to the stairs.
        session = Turn(session, ExplorationTurnDirection.Left);
        session = Forward(session);
        Assert.Equal(new GridPosition(0, 2), session.Exploration!.Position);
        session = Turn(session, ExplorationTurnDirection.Left);
        session = Forward(session);
        Assert.Equal(new GridPosition(1, 2), session.Exploration!.Position);

        Assert.True(ExplorationRules.CanUseStairs(session));
        session = ExplorationRules.UseStairs(session);
        Assert.Equal("UpperFloor", session.Exploration!.Floor);
        Assert.Equal(new GridPosition(0, 0), session.Exploration!.Position);

        // Face east and cross the cellar to the vault.
        session = FaceEast(session);
        session = Forward(session);
        session = Forward(session);
        Assert.Equal(new GridPosition(2, 0), session.Exploration!.Position);

        Assert.True(ScenarioTriggerRules.CanActivate(session));
        session = ScenarioTriggerRules.Activate(session);
        Assert.Equal(ApplicationMode.Encounter, session.CurrentMode);
        Assert.Equal(
            HollowMillScenarioDefinitionProvider.VerminRoused,
            session.Scenario.ProgressId);

        EncounterState encounter = Assert
            .IsType<ActiveEncounterState>(session.ActiveEncounter)
            .Encounter;
        Assert.Equal(
            HollowMillScenarioDefinitionProvider.InformedEncounterId,
            encounter.EncounterId);
        Assert.Equal(
            2,
            encounter.Participants.Count(participant =>
                participant.SideId
                    == HollowMillScenarioDefinitionProvider.VerminSideId));

        session = WinTheFight(session);
        Assert.Equal(
            HollowMillScenarioDefinitionProvider.VerminRoused,
            session.Scenario.ProgressId);
        Assert.Equal(ApplicationMode.Exploration, session.CurrentMode);
        Assert.Equal(new GridPosition(2, 0), session.Exploration!.Position);

        // One step to the root, and the scenario is won.
        session = Turn(session, ExplorationTurnDirection.Right);
        session = Forward(session);
        Assert.Equal(new GridPosition(2, 1), session.Exploration!.Position);

        Assert.True(ScenarioTriggerRules.CanActivate(session));
        session = ScenarioTriggerRules.Activate(session);
        Assert.Equal(
            HollowMillScenarioDefinitionProvider.BlightSevered,
            session.Scenario.ProgressId);
        Assert.Equal(ApplicationMode.ScenarioConclusion, session.CurrentMode);
    }

    /// Confronting the miller instead rouses both the swarm and his thrall —
    /// a harder fight the party loses here, ending the scenario in defeat.
    [Fact]
    public void BlindBranch_ConfrontingTheMiller_FacesTheHarderFight()
    {
        ApplicationSessionState session = CreateSession();

        session = OutpostDecisionRules.Resolve(
            session,
            "AcceptMission")
            .State;
        session = TravelToTheMill(session);
        session = ExplorationRules.EnterDestination(session);

        // East door: confront the miller directly.
        session = Forward(session);
        Assert.Equal(new GridPosition(1, 1), session.Exploration!.Position);
        session = Turn(session, ExplorationTurnDirection.Left);
        session = Forward(session);
        Assert.Equal(new GridPosition(2, 1), session.Exploration!.Position);

        Assert.True(ScenarioTriggerRules.CanActivate(session));
        session = ScenarioTriggerRules.Activate(session);
        Assert.Equal(
            HollowMillScenarioDefinitionProvider.MillerConfronted,
            session.Scenario.ProgressId);

        // Down to the stairs.
        session = Turn(session, ExplorationTurnDirection.Right);
        session = Forward(session);
        Assert.Equal(new GridPosition(2, 2), session.Exploration!.Position);
        session = Turn(session, ExplorationTurnDirection.Right);
        session = Forward(session);
        Assert.Equal(new GridPosition(1, 2), session.Exploration!.Position);

        Assert.True(ExplorationRules.CanUseStairs(session));
        session = ExplorationRules.UseStairs(session);
        Assert.Equal("UpperFloor", session.Exploration!.Floor);
        Assert.Equal(new GridPosition(0, 0), session.Exploration!.Position);

        session = FaceEast(session);
        session = Forward(session);
        session = Forward(session);
        Assert.Equal(new GridPosition(2, 0), session.Exploration!.Position);

        Assert.True(ScenarioTriggerRules.CanActivate(session));
        session = ScenarioTriggerRules.Activate(session);
        Assert.Equal(ApplicationMode.Encounter, session.CurrentMode);

        EncounterState encounter = Assert
            .IsType<ActiveEncounterState>(session.ActiveEncounter)
            .Encounter;
        Assert.Equal(
            HollowMillScenarioDefinitionProvider.BlindEncounterId,
            encounter.EncounterId);
        Assert.Equal(
            3,
            encounter.Participants.Count(participant =>
                participant.SideId
                    == HollowMillScenarioDefinitionProvider.VerminSideId));
        Assert.Contains(
            encounter.Participants,
            participant => participant.Combatant.CombatantId
                == "combatant.mill-thrall");

        session = LoseTheFight(session);
        Assert.Equal(
            HollowMillScenarioDefinitionProvider.MillersLost,
            session.Scenario.ProgressId);
        Assert.Equal(ApplicationMode.ScenarioConclusion, session.CurrentMode);
    }

    /// The vehicle for proving multi-route travel with real, registered
    /// content: two roads out of the village, same destination, different
    /// lengths. Once the commission is accepted, both are open at once and
    /// the party — not the engine — picks between them.
    [Fact]
    public void MultiRoute_OffersBothRoads_AndTheShortcutArrivesFaster()
    {
        ApplicationSessionState session = CreateSession();
        session = OutpostDecisionRules.Resolve(
            session,
            "AcceptMission")
            .State;

        IReadOnlyList<TravelRouteDefinition> available =
            RegionalTravelRules.GetAvailableRoutes(session);

        Assert.Equal(2, available.Count);
        Assert.Contains(
            available,
            route => route.RouteId
                == HollowMillScenarioDefinitionProvider.RouteId);
        Assert.Contains(
            available,
            route => route.RouteId
                == HollowMillScenarioDefinitionProvider.ShortcutRouteId);

        // Ambiguous without a route ID now that more than one road is open.
        Assert.Throws<InvalidOperationException>(() =>
            RegionalTravelRules.BeginJourney(session));

        ApplicationSessionState shortcut = RegionalTravelRules.BeginJourney(
            session,
            HollowMillScenarioDefinitionProvider.ShortcutRouteId);

        Assert.Equal(
            HollowMillScenarioDefinitionProvider.ShortcutRouteId,
            shortcut.RegionalTravel!.RouteId);
        Assert.Equal(
            HollowMillScenarioDefinitionProvider.MillLocationId,
            shortcut.RegionalTravel!.DestinationLocationId);
        Assert.True(
            shortcut.RegionalTravel!.FinalStepIndex
                < RegionalTravelRules
                    .GetAvailableRoutes(session)
                    .Single(route => route.RouteId
                        == HollowMillScenarioDefinitionProvider.RouteId)
                    .FinalStepIndex);

        while (RegionalTravelRules.CanAdvance(shortcut))
        {
            shortcut = RegionalTravelRules.Advance(shortcut).State;
        }

        Assert.Equal(
            HollowMillScenarioDefinitionProvider.MillLocationId,
            shortcut.CurrentLocationId);
    }

    /// The vehicle for proving a decision can offer more than the old fixed
    /// accept/decline shape with real, registered content. Three options,
    /// each resolved purely by its own string ID — no enum, no built-in
    /// ceiling on how many a decision can carry.
    [Fact]
    public void MultiOptionDecision_OffersAllThree_AndEachResolvesByItsOwnId()
    {
        ApplicationSessionState session = CreateSession();

        IReadOnlyList<SessionAction> decisionActions = SessionView
            .Describe(session)
            .Actions
            .Where(action => action.Kind
                == SessionActionKind.ResolveOutpostDecision)
            .ToArray();

        Assert.Equal(3, decisionActions.Count);
        Assert.Contains(
            decisionActions,
            action => action.DecisionOptionId == "AcceptMission");
        Assert.Contains(
            decisionActions,
            action => action.DecisionOptionId == "AskAboutPay");
        Assert.Contains(
            decisionActions,
            action => action.DecisionOptionId == "NotYet");

        // A non-committal option, distinct from "Not yet", leaves the
        // scenario exactly where it stands and the decision still on offer.
        OutpostDecisionResult asked = OutpostDecisionRules.Resolve(
            session,
            "AskAboutPay");

        Assert.False(asked.DidProgressChange);
        Assert.Equal(
            HollowMillScenarioDefinitionProvider.RumorHeard,
            asked.State.Scenario.ProgressId);
        Assert.Equal(
            3,
            OutpostDecisionRules.GetAvailableOptionIds(asked.State).Count);

        OutpostDecisionResult accepted = OutpostDecisionRules.Resolve(
            asked.State,
            "AcceptMission");

        Assert.True(accepted.DidProgressChange);
        Assert.Equal(
            HollowMillScenarioDefinitionProvider.CommissionAccepted,
            accepted.State.Scenario.ProgressId);
    }

    [Fact]
    public void Registry_ResolvesTheScenario()
    {
        Assert.True(ScenarioDefinitionRegistry.IsRegistered(
            HollowMillScenarioDefinitionProvider.ScenarioId));

        ScenarioDefinition definition = ScenarioDefinitionRegistry.Resolve(
            HollowMillScenarioDefinitionProvider.ScenarioId);

        Assert.Equal(
            HollowMillScenarioDefinitionProvider.ScenarioId,
            definition.ScenarioId);
    }

    [Fact]
    public void Campaign_ClaimsTheScenario()
    {
        CampaignDefinition campaign = CampaignRegistry.ResolveForScenario(
            HollowMillScenarioDefinitionProvider.ScenarioId);

        Assert.Equal(FrontierCampaignContent.CampaignId, campaign.CampaignId);
    }

    private static ApplicationSessionState CreateSession()
    {
        return ScenarioSessionFactory.CreateNew(
            HollowMillScenarioDefinitionProvider.ScenarioId,
            RandomSeed);
    }

    private static ApplicationSessionState TravelToTheMill(
        ApplicationSessionState session)
    {
        Assert.True(RegionalTravelRules.CanBeginJourney(session));

        // Two routes are open here (see MultiRoute_OffersBothRoads below);
        // the cart track is the one every branch test before this one walks.
        session = RegionalTravelRules.BeginJourney(
            session,
            HollowMillScenarioDefinitionProvider.RouteId);

        while (RegionalTravelRules.CanAdvance(session))
        {
            session = RegionalTravelRules.Advance(session).State;
        }

        return session;
    }

    private static ApplicationSessionState Forward(
        ApplicationSessionState session)
    {
        ExplorationMoveResult result = ExplorationRules.MoveForward(session);
        Assert.True(result.DidMove);
        return result.State;
    }

    private static ApplicationSessionState Turn(
        ApplicationSessionState session,
        ExplorationTurnDirection direction)
    {
        return ExplorationRules.Turn(session, direction);
    }

    private static ApplicationSessionState FaceEast(
        ApplicationSessionState session)
    {
        while (session.Exploration!.Facing != ExplorationFacing.East)
        {
            session = Turn(session, ExplorationTurnDirection.Right);
        }

        return session;
    }

    private static ApplicationSessionState WinTheFight(
        ApplicationSessionState session)
    {
        return ResolveFight(session, winningSideId: "side.party");
    }

    private static ApplicationSessionState LoseTheFight(
        ApplicationSessionState session)
    {
        return ResolveFight(
            session,
            winningSideId: HollowMillScenarioDefinitionProvider.VerminSideId);
    }

    private static ApplicationSessionState ResolveFight(
        ApplicationSessionState session,
        string winningSideId)
    {
        ActiveEncounterState active =
            Assert.IsType<ActiveEncounterState>(session.ActiveEncounter);
        EncounterState completed = EncounterRules.Complete(
            active.Encounter,
            winningSideId);

        return CombatOutcomeRules.Finalize(
            session with
            {
                ActiveEncounter = active with
                {
                    Encounter = completed
                }
            })
            .State;
    }
}

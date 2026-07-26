using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Persistence;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Core.Runtime;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Application.Tests;

/// Phase 6 step 7: the proof that the engine runs on content rather than on the
/// Watchtower.
///
/// Every rule these tests call is the generic entry point a client uses. None
/// of them is told which scenario is running, and none of the values here comes
/// from Watchtower content — the scenario's own vocabulary is checked against
/// its own constants throughout.
public sealed class SecondScenarioTests
{
    private const int RandomSeed = 4242;

    [Fact]
    public void Definition_IsValidAuthoredContent()
    {
        ValidationResult validation =
            ScenarioDefinitionValidator.Validate(
                SunkenChapelScenarioDefinitionProvider.Create());

        Assert.True(validation.IsValid);
        Assert.Empty(validation.Issues);
    }

    /// A new session begins where the scenario says it begins, on the marker
    /// the scenario declares — not on the first scenario's starting state.
    [Fact]
    public void CreateNew_StartsWhereTheScenarioDeclares()
    {
        ApplicationSessionState session = CreateSession();

        Assert.Equal(
            SunkenChapelScenarioDefinitionProvider.ScenarioId,
            session.ScenarioId);
        Assert.Equal(
            SunkenChapelScenarioDefinitionProvider.HarborLocationId,
            session.CurrentLocationId);
        Assert.Equal(
            SunkenChapelScenarioDefinitionProvider.RumourHeard,
            session.Scenario.ProgressId);
        Assert.Equal(ApplicationMode.Outpost, session.CurrentMode);
    }

    /// The whole non-combat path, driven entirely by the definition: take the
    /// commission, walk there, go inside, work the seal, lift the relic.
    [Fact]
    public void Traversal_RunsEndToEndOnGenericEntryPoints()
    {
        ApplicationSessionState session = CreateSession();

        Assert.Equal(
            [OutpostMissionChoice.AcceptMission, OutpostMissionChoice.NotYet],
            OutpostMissionRules.GetAvailableChoices(session));

        session = OutpostMissionRules.Resolve(
            session,
            OutpostMissionChoice.AcceptMission)
            .State;
        Assert.Equal(
            SunkenChapelScenarioDefinitionProvider.CharterSigned,
            session.Scenario.ProgressId);

        Assert.True(RegionalTravelRules.CanBeginJourney(session));
        session = RegionalTravelRules.BeginJourney(session);
        Assert.Equal(
            SunkenChapelScenarioDefinitionProvider.RouteId,
            session.RegionalTravel!.RouteId);

        while (RegionalTravelRules.CanAdvance(session))
        {
            session = RegionalTravelRules.Advance(session).State;
        }

        Assert.Equal(
            SunkenChapelScenarioDefinitionProvider.ChapelLocationId,
            session.CurrentLocationId);

        Assert.True(ExplorationRules.CanEnterDestination(session));
        session = ExplorationRules.EnterDestination(session);
        Assert.Equal(ApplicationMode.Exploration, session.CurrentMode);
        Assert.Equal(
            new GridPosition(0, 0),
            session.Exploration!.Position);

        // Nothing to work until the party is standing on the seal.
        Assert.False(ScenarioTriggerRules.CanActivate(session));
        session = ExplorationRules.MoveForward(session).State;
        Assert.Equal(
            new GridPosition(1, 0),
            session.Exploration!.Position);

        Assert.True(ScenarioTriggerRules.CanActivate(session));
        session = ScenarioTriggerRules.Activate(session);

        // A trigger that starts no encounter advances the scenario and leaves
        // the party exploring exactly where it stood.
        Assert.Equal(
            SunkenChapelScenarioDefinitionProvider.SealBroken,
            session.Scenario.ProgressId);
        Assert.Equal(ApplicationMode.Exploration, session.CurrentMode);
        Assert.Equal(
            new GridPosition(1, 0),
            session.Exploration!.Position);
        Assert.Null(session.ActiveEncounter);

        session = ExplorationRules.Turn(
            session,
            ExplorationTurnDirection.Right);
        session = ExplorationRules.MoveForward(session).State;
        Assert.Equal(
            new GridPosition(1, 1),
            session.Exploration!.Position);

        Assert.True(ScenarioTriggerRules.CanActivate(session));
        session = ScenarioTriggerRules.Activate(session);

        // A trigger whose resulting marker is a declared conclusion ends the
        // scenario, the same way winning an encounter does.
        Assert.Equal(
            SunkenChapelScenarioDefinitionProvider.RelicRecovered,
            session.Scenario.ProgressId);
        Assert.Equal(
            ApplicationMode.ScenarioConclusion,
            session.CurrentMode);
        Assert.Null(session.Exploration);
        Assert.Null(session.RegionalTravel);
    }

    /// The scenario never touches combat, so nothing on this path builds an
    /// encounter or consumes randomness.
    [Fact]
    public void Traversal_ConsumesNoRandomness()
    {
        ApplicationSessionState session = RunToConclusion();

        Assert.Equal(RandomSeed, session.RandomSeed);
        Assert.Equal(0, session.RandomValuesConsumed);
    }

    /// The save format carries a marker it cannot interpret. Nothing in it
    /// knows this scenario's vocabulary, so a round trip is the evidence that
    /// it does not need to.
    [Theory]
    [InlineData(TraversalStage.Outpost)]
    [InlineData(TraversalStage.Exploring)]
    [InlineData(TraversalStage.SealBroken)]
    [InlineData(TraversalStage.Concluded)]
    public void Save_RoundTripsAtEveryStage(
        TraversalStage stage)
    {
        ApplicationSessionState session = RunTo(stage);
        string saved = ManualSaveSerializer.Serialize(session);

        ManualSaveLoadResult result =
            ManualSaveSerializer.Deserialize(saved);

        Assert.True(result.IsSuccess);
        ApplicationSessionState loaded = Assert.IsType<ApplicationSessionState>(
            result.Session);

        // Re-serializing to the same document is the round-trip evidence:
        // session records hold their party as an IReadOnlyList, so comparing
        // them directly would compare collection references rather than
        // contents.
        Assert.Equal(saved, ManualSaveSerializer.Serialize(loaded));
        Assert.Equal(session.ScenarioId, loaded.ScenarioId);
        Assert.Equal(session.CurrentMode, loaded.CurrentMode);
        Assert.Equal(session.CurrentLocationId, loaded.CurrentLocationId);
        Assert.Equal(
            session.Scenario.ProgressId,
            loaded.Scenario.ProgressId);
    }

    /// Mid-journey saving is refused for every scenario alike — it is a limit
    /// of this phase of the save format, not something the first scenario is
    /// special about.
    [Fact]
    public void Save_WhileTravelling_IsRefused()
    {
        ApplicationSessionState travelling =
            RunTo(TraversalStage.Travelling);

        Assert.False(ManualSaveSerializer.CanSerialize(travelling));
        Assert.ThrowsAny<ArgumentException>(() =>
            ManualSaveSerializer.Serialize(travelling));
    }

    /// Both scenarios resolve their own content from the same registry, and
    /// neither leaks into the other.
    [Fact]
    public void BothScenariosResolveTheirOwnContent()
    {
        ScenarioDefinition chapel = ScenarioDefinitionRegistry.Resolve(
            SunkenChapelScenarioDefinitionProvider.ScenarioId);
        ScenarioDefinition watchtower = ScenarioDefinitionRegistry.Resolve(
            WatchtowerScenarioContent.ScenarioId);

        Assert.NotEqual(watchtower.ScenarioId, chapel.ScenarioId);
        Assert.NotEqual(
            watchtower.StartingLocationId,
            chapel.StartingLocationId);
        Assert.Empty(chapel.Progress.ProgressIds.Intersect(
            watchtower.Progress.ProgressIds,
            StringComparer.Ordinal));
        Assert.Empty(chapel.Locations
            .Select(location => location.LocationId)
            .Intersect(
                watchtower.Locations.Select(location => location.LocationId),
                StringComparer.Ordinal));
    }

    /// A session cannot carry the other scenario's vocabulary, which is what
    /// stops a scenario being validated against content that is not its own.
    [Fact]
    public void ProgressFromTheOtherScenario_IsRejected()
    {
        ApplicationSessionState session = CreateSession();

        Assert.ThrowsAny<ArgumentException>(() =>
            ApplicationSessionRules.Validate(
                session with
                {
                    Scenario = new ScenarioState
                    {
                        ProgressId = WatchtowerScenario.ToProgressId(
                            WatchtowerScenarioProgress.MissionAccepted)
                    }
                }));
    }

    public enum TraversalStage
    {
        Outpost,
        Travelling,
        Exploring,
        SealBroken,
        Concluded
    }

    private static ApplicationSessionState CreateSession()
    {
        return ScenarioSessionFactory.CreateNew(
            SunkenChapelScenarioDefinitionProvider.ScenarioId,
            RandomSeed);
    }

    private static ApplicationSessionState RunToConclusion()
    {
        return RunTo(TraversalStage.Concluded);
    }

    private static ApplicationSessionState RunTo(
        TraversalStage stage)
    {
        ApplicationSessionState session = CreateSession();

        if (stage == TraversalStage.Outpost)
        {
            return session;
        }

        session = OutpostMissionRules.Resolve(
            session,
            OutpostMissionChoice.AcceptMission)
            .State;
        session = RegionalTravelRules.BeginJourney(session);

        if (stage == TraversalStage.Travelling)
        {
            return session;
        }

        while (RegionalTravelRules.CanAdvance(session))
        {
            session = RegionalTravelRules.Advance(session).State;
        }

        session = ExplorationRules.EnterDestination(session);

        if (stage == TraversalStage.Exploring)
        {
            return session;
        }

        session = ExplorationRules.MoveForward(session).State;
        session = ScenarioTriggerRules.Activate(session);

        if (stage == TraversalStage.SealBroken)
        {
            return session;
        }

        session = ExplorationRules.Turn(
            session,
            ExplorationTurnDirection.Right);
        session = ExplorationRules.MoveForward(session).State;

        return ScenarioTriggerRules.Activate(session);
    }
}

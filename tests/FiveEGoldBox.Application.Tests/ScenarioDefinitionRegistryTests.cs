using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;

namespace FiveEGoldBox.Application.Tests;

/// The registry is the seam that lets rules find their content from the
/// session's scenario ID instead of naming a scenario directly.
public sealed class ScenarioDefinitionRegistryTests
{
    [Fact]
    public void Resolve_FindsContentFromTheSessionsScenarioId()
    {
        ApplicationSessionState session =
            WatchtowerScenarioSessionFactory.CreateNew(randomSeed: 5);

        ScenarioDefinition definition =
            ScenarioDefinitionRegistry.Resolve(session);

        Assert.Equal(session.ScenarioId, definition.ScenarioId);
        Assert.Equal(
            session.CurrentLocationId,
            definition.StartingLocationId);
    }

    /// Resolving is cached, so rules can look content up freely without paying
    /// for validation on every call.
    [Fact]
    public void Resolve_ReturnsTheSameInstanceEachTime()
    {
        Assert.Same(
            ScenarioDefinitionRegistry.Resolve(
                WatchtowerScenarioContent.ScenarioId),
            ScenarioDefinitionRegistry.Resolve(
                WatchtowerScenarioContent.ScenarioId));
    }

    [Theory]
    [InlineData("scenario.not-registered")]
    [InlineData("")]
    public void Resolve_RejectsAnUnknownScenario(string scenarioId)
    {
        Assert.Throws<ArgumentException>(() =>
            ScenarioDefinitionRegistry.Resolve(scenarioId));
    }

    [Fact]
    public void IsRegistered_KnowsWhichScenariosExist()
    {
        Assert.True(ScenarioDefinitionRegistry.IsRegistered(
            WatchtowerScenarioContent.ScenarioId));
        Assert.False(ScenarioDefinitionRegistry.IsRegistered(
            "scenario.not-registered"));
    }

    /// Travel now reads the route from authored content, so the definition and
    /// the state a journey produces have to agree.
    [Fact]
    public void BeginJourney_UsesTheAuthoredRoute()
    {
        ApplicationSessionState accepted =
            WatchtowerSignalTestData.CreateRegionalTravelSession();
        TravelRouteDefinition route = Assert.Single(
            ScenarioDefinitionRegistry.Resolve(accepted).Routes);
        RegionalTravelState travel =
            Assert.IsType<RegionalTravelState>(accepted.RegionalTravel);

        Assert.Equal(route.RouteId, travel.RouteId);
        Assert.Equal(route.FinalStepIndex, travel.FinalStepIndex);
        Assert.Equal(
            route.DestinationLocationId,
            travel.DestinationLocationId);
    }
}

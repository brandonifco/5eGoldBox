using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Core.Runtime;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Application.Tests;

/// The real Watchtower definition, as opposed to the hand-built one in
/// ScenarioDefinitionModelTests. This is content that has to hold up against
/// the running implementation it was derived from.
public sealed class WatchtowerScenarioDefinitionProviderTests
{
    [Fact]
    public void Definition_ValidatesWithoutErrors()
    {
        ValidationResult result = ScenarioDefinitionValidator.Validate(
            ScenarioDefinitionRegistry.Resolve(WatchtowerScenarioContent.ScenarioId));

        Assert.True(
            result.IsValid,
            "Watchtower content should carry no errors, but reported: "
                + string.Join(
                    "; ",
                    result.Issues
                        .Where(issue => issue.Severity == ValidationSeverity.Error)
                        .Select(issue => $"{issue.Code}: {issue.Message}")));
    }

    /// The vocabulary is now exactly what the scenario can reach. It used to
    /// declare two markers - SuccessReported and ScenarioCompleted - that
    /// nothing produced, which the reachability check reported as warnings.
    /// They were deleted rather than wired up: reporting back would need a
    /// return journey, and travel supports one route per scenario.
    [Fact]
    public void Definition_DeclaresNoProgressItCannotReach()
    {
        ValidationResult result = ScenarioDefinitionValidator.Validate(
            ScenarioDefinitionRegistry.Resolve(WatchtowerScenarioContent.ScenarioId));

        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code == "scenario.progress.unreachable");
        Assert.Empty(result.Issues);
    }

    /// The tower's geometry is authored here now, so this pins it: both floors
    /// exist, the party starts somewhere it can stand, and the stairs link the
    /// floors in both directions.
    [Fact]
    public void Definition_DescribesACoherentTower()
    {
        ExplorationMapDefinition map = Assert.IsType<ExplorationMapDefinition>(
            Assert.Single(
                ScenarioDefinitionRegistry.Resolve(WatchtowerScenarioContent.ScenarioId).Locations,
                location => location.LocationId
                    == WatchtowerRegionalRoute.WatchtowerLocationId)
            .ExplorationMap);

        Assert.Equal(2, map.Floors.Count);
        Assert.True(ScenarioExplorationMap.IsTraversable(
            map,
            map.StartingFloor,
            map.StartingPosition,
            Array.Empty<string>()));

        StairDefinition up = Assert.Single(
            Assert.Single(
                map.Floors,
                floor => floor.Floor == "GroundFloor")
            .Stairs);
        StairDefinition down = Assert.Single(
            Assert.Single(
                map.Floors,
                floor => floor.Floor == "UpperFloor")
            .Stairs);

        Assert.Equal("UpperFloor", up.DestinationFloor);
        Assert.Equal("GroundFloor", down.DestinationFloor);

        // Going up and back down returns the party where it started.
        Assert.Equal(up.Position, down.DestinationPosition);
        Assert.Equal(down.Position, up.DestinationPosition);
    }

    [Fact]
    public void Definition_MatchesTheImplementationsAmbush()
    {
        EncounterDefinition encounter = Assert.Single(
            ScenarioDefinitionRegistry.Resolve(WatchtowerScenarioContent.ScenarioId).Encounters);

        Assert.Equal(
            WatchtowerSignalEncounter.EncounterId,
            encounter.EncounterId);
        Assert.Equal(
            WatchtowerSignalEncounter.BattlefieldWidth,
            encounter.Width);
        Assert.Equal(
            WatchtowerSignalEncounter.BattlefieldHeight,
            encounter.Height);
        Assert.Equal(
            WatchtowerSignalEncounter.PartyStartingPositions,
            encounter.PartyStartingPositions);

        Assert.Equal(
            WatchtowerScenario.ToProgressId(
                WatchtowerScenarioProgress.RaidersDefeated),
            encounter.Outcome.VictoryProgressId);
        Assert.Equal(
            WatchtowerScenario.ToProgressId(
                WatchtowerScenarioProgress.PartyDefeated),
            encounter.Outcome.DefeatProgressId);
    }

    /// Accepting the commission is what opens the road; declining leaves the
    /// scenario exactly where it was.
    [Fact]
    public void Definition_ExpressesTheMissionDecision()
    {
        ScenarioDecisionDefinition decision = Assert.Single(
            ScenarioDefinitionRegistry.Resolve(WatchtowerScenarioContent.ScenarioId).Decisions);

        Assert.Equal(
            WatchtowerScenarioContent.OutpostLocationId,
            decision.LocationId);

        ScenarioDecisionOptionDefinition accept = Assert.Single(
            decision.Options,
            option => option.OptionId
                == "AcceptMission");
        ScenarioDecisionOptionDefinition notYet = Assert.Single(
            decision.Options,
            option => option.OptionId
                == "NotYet");

        Assert.Equal(
            WatchtowerScenario.ToProgressId(
                WatchtowerScenarioProgress.MissionAccepted),
            accept.ResultingProgressId);
        Assert.Null(notYet.ResultingProgressId);
    }
}

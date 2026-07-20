using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Tests;

public sealed class ScenarioConclusionStateTests
{
    [Fact]
    public void Validate_WithValidPartyDefeatedConclusion_Accepts()
    {
        ApplicationSessionRules.Validate(
            CreateConclusion());
    }

    [Theory]
    [InlineData(WatchtowerScenarioProgress.MissionNotAccepted)]
    [InlineData(WatchtowerScenarioProgress.MissionAccepted)]
    [InlineData(WatchtowerScenarioProgress.SignalActivated)]
    [InlineData(WatchtowerScenarioProgress.RaidersDefeated)]
    [InlineData(WatchtowerScenarioProgress.SuccessReported)]
    [InlineData(WatchtowerScenarioProgress.ScenarioCompleted)]
    public void Validate_WithNonDefeatProgress_Throws(
        WatchtowerScenarioProgress progress)
    {
        ApplicationSessionState invalid = CreateConclusion()
            with
            {
                Scenario = new WatchtowerScenarioState
                {
                    Progress = progress
                }
            };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(invalid));
    }

    [Fact]
    public void Validate_AtLocationOtherThanRuinedWatchtower_Throws()
    {
        ApplicationSessionState invalid = CreateConclusion()
            with
            {
                CurrentLocationId = "location.outpost"
            };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(invalid));
    }

    [Theory]
    [InlineData("travel")]
    [InlineData("exploration")]
    [InlineData("encounter")]
    public void Validate_WithModeSpecificRuntimeState_Throws(
        string runtimeState)
    {
        ApplicationSessionState conclusion = CreateConclusion();
        ApplicationSessionState invalid = runtimeState switch
        {
            "travel" => conclusion with
            {
                RegionalTravel = WatchtowerSignalTestData
                    .CreateRegionalTravelSession()
                    .RegionalTravel
            },
            "exploration" => conclusion with
            {
                Exploration = WatchtowerSignalTestData
                    .CreateExplorationSession()
                    .Exploration
            },
            "encounter" => conclusion with
            {
                ActiveEncounter = WatchtowerSignalTestData
                    .CreateEncounterSession()
                    .ActiveEncounter
            },
            _ => throw new InvalidOperationException()
        };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(invalid));
    }

    [Theory]
    [InlineData(ApplicationMode.Outpost)]
    [InlineData(ApplicationMode.RegionalTravel)]
    [InlineData(ApplicationMode.Exploration)]
    [InlineData(ApplicationMode.Encounter)]
    public void Validate_PartyDefeatedProgressOutsideScenarioConclusion_Throws(
        ApplicationMode mode)
    {
        ApplicationSessionState source = mode switch
        {
            ApplicationMode.Outpost =>
                WatchtowerSignalTestData.CreateAcceptedSession(),
            ApplicationMode.RegionalTravel =>
                WatchtowerSignalTestData.CreateRegionalTravelSession(),
            ApplicationMode.Exploration =>
                WatchtowerSignalTestData.CreateExplorationSession(),
            ApplicationMode.Encounter =>
                WatchtowerSignalTestData.CreateEncounterSession(),
            _ => throw new InvalidOperationException()
        };
        ApplicationSessionState invalid = source with
        {
            Scenario = source.Scenario with
            {
                Progress = WatchtowerScenarioProgress.PartyDefeated
            }
        };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(invalid));
    }

    private static ApplicationSessionState CreateConclusion()
    {
        return WatchtowerCombatOutcomeRules.Finalize(
            WatchtowerCombatOutcomeTestData
                .CreateRaiderVictorySession())
            .State;
    }
}

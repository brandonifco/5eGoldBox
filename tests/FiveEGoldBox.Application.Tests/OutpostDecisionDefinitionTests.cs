using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Tests;

/// The outpost mission decision is now authored content rather than a rule.
/// These check that what the scenario declares is what the party is offered.
public sealed class OutpostDecisionDefinitionTests
{
    [Fact]
    public void AvailableChoices_ComeFromTheAuthoredDecisionInOrder()
    {
        ApplicationSessionState session = CreateOutpostSession();
        ScenarioDecisionDefinition decision = Assert.Single(
            ScenarioDefinitionRegistry.Resolve(session).Decisions);

        IReadOnlyList<OutpostMissionChoice> choices =
            OutpostMissionRules.GetAvailableChoices(session);

        Assert.Equal(
            decision.Options
                .Select(option => Enum.Parse<OutpostMissionChoice>(option.OptionId))
                .ToArray(),
            choices.ToArray());
    }

    /// Accepting moves the scenario to whatever marker the option names, rather
    /// than to a marker the rule knows about.
    [Fact]
    public void Resolve_AdvancesToTheProgressTheOptionNames()
    {
        ApplicationSessionState session = CreateOutpostSession();
        ScenarioDecisionOptionDefinition accept = Assert.Single(
            ScenarioDefinitionRegistry.Resolve(session).Decisions[0].Options,
            option => option.OptionId
                == OutpostMissionChoice.AcceptMission.ToString());

        OutpostMissionResult result = OutpostMissionRules.Resolve(
            session,
            OutpostMissionChoice.AcceptMission);

        Assert.True(result.DidProgressChange);
        Assert.Equal(
            accept.ResultingProgressId,
            result.State.Scenario.ProgressId);
    }

    /// An option naming no resulting progress leaves the scenario alone, which
    /// is how the definition expresses declining.
    [Fact]
    public void Resolve_LeavesProgressAloneWhenTheOptionNamesNone()
    {
        ApplicationSessionState session = CreateOutpostSession();

        OutpostMissionResult result = OutpostMissionRules.Resolve(
            session,
            OutpostMissionChoice.NotYet);

        Assert.False(result.DidProgressChange);
        Assert.Equal(
            session.Scenario.ProgressId,
            result.State.Scenario.ProgressId);
    }

    /// Once the decision's required progress no longer matches, it stops being
    /// offered — the rule no longer decides that for itself.
    [Fact]
    public void Decision_StopsBeingOfferedOnceItHasBeenTaken()
    {
        ApplicationSessionState accepted = OutpostMissionRules.Resolve(
            CreateOutpostSession(),
            OutpostMissionChoice.AcceptMission)
            .State;

        Assert.Empty(OutpostMissionRules.GetAvailableChoices(accepted));
        Assert.Throws<InvalidOperationException>(() =>
            OutpostMissionRules.Resolve(
                accepted,
                OutpostMissionChoice.AcceptMission));
    }

    private static ApplicationSessionState CreateOutpostSession()
    {
        return ScenarioSessionFactory.CreateNew(WatchtowerScenarioContent.ScenarioId, randomSeed: 11);
    }
}

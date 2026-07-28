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

        IReadOnlyList<string> optionIds =
            OutpostDecisionRules.GetAvailableOptionIds(session);

        Assert.Equal(
            decision.Options
                .Select(option => option.OptionId)
                .ToArray(),
            optionIds.ToArray());
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
                == "AcceptMission");

        OutpostDecisionResult result = OutpostDecisionRules.Resolve(
            session,
            "AcceptMission");

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

        OutpostDecisionResult result = OutpostDecisionRules.Resolve(
            session,
            "NotYet");

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
        ApplicationSessionState accepted = OutpostDecisionRules.Resolve(
            CreateOutpostSession(),
            "AcceptMission")
            .State;

        Assert.Empty(OutpostDecisionRules.GetAvailableOptionIds(accepted));
        Assert.Throws<InvalidOperationException>(() =>
            OutpostDecisionRules.Resolve(
                accepted,
                "AcceptMission"));
    }

    private static ApplicationSessionState CreateOutpostSession()
    {
        return ScenarioSessionFactory.CreateNew(WatchtowerScenarioContent.ScenarioId, randomSeed: 11);
    }
}

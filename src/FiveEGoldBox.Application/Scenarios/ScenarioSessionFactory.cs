using FiveEGoldBox.Application.Campaigns;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Scenarios;

/// Starts a new session for whichever scenario is asked for.
///
/// A client names a scenario and gets a session; it does not need to know
/// which scenario that is or what content backs it.
public static class ScenarioSessionFactory
{
    public static ApplicationSessionState CreateNew(
        string scenarioId,
        int randomSeed)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            throw new ArgumentException(
                "Scenario ID is required.",
                nameof(scenarioId));
        }

        ScenarioDefinition scenario =
            ScenarioDefinitionRegistry.Resolve(scenarioId);

        // The party comes from the campaign the scenario belongs to. Which
        // characters attempt an adventure is a campaign's business, not the
        // adventure's.
        CampaignDefinition campaign =
            CampaignRegistry.ResolveForScenario(scenarioId);

        return ApplicationSessionRules.CreateNew(
            scenario.ScenarioId,
            scenario.StartingLocationId,
            CampaignPartyFactory.CreateStartingParty(campaign),
            randomSeed);
    }
}

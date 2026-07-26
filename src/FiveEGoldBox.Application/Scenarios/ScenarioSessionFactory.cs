using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Scenarios;

/// Starts a new session for whichever scenario is asked for.
///
/// A client names a scenario and gets a session; it does not need to know
/// which scenario that is or what content backs it.
public static class ScenarioSessionFactory
{
    /// Where a new party comes from.
    ///
    /// Party composition is campaign-declared rather than scenario content, so
    /// a ScenarioDefinition deliberately carries no roster. There is no
    /// campaign concept in the code yet, so this stands in for one: the
    /// starting party is looked up alongside the scenario instead of being
    /// authored inside it. When campaigns exist, this lookup is what they
    /// replace.
    private static readonly IReadOnlyDictionary<string, Func<PartyState>>
        StartingParties = new Dictionary<string, Func<PartyState>>(
            StringComparer.Ordinal)
        {
            [WatchtowerScenarioContent.ScenarioId] =
                WatchtowerScenarioContent.CreateStartingParty,
            // The same roster, and deliberately so: one campaign, one party,
            // whichever of its scenarios is being attempted. That both entries
            // point at the same factory is the shape a campaign would take if
            // it existed as a type.
            [SunkenChapelScenarioDefinitionProvider.ScenarioId] =
                WatchtowerScenarioContent.CreateStartingParty
        };

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

        if (!StartingParties.TryGetValue(
            scenarioId,
            out Func<PartyState>? createParty))
        {
            throw new ArgumentException(
                $"No starting party is registered for scenario '{scenarioId}'.",
                nameof(scenarioId));
        }

        return ApplicationSessionRules.CreateNew(
            scenario.ScenarioId,
            scenario.StartingLocationId,
            createParty(),
            randomSeed);
    }
}

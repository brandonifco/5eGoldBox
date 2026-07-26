using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Application.Scenarios;

/// Identifiers the Watchtower scenario's content is authored from.
internal static class WatchtowerScenarioContent
{
    internal const string ScenarioId =
        "scenario.watchtower";

    internal const string OutpostLocationId =
        "location.outpost";

    internal static ValidatedRuleset CreateRuleset()
    {
        return RulesetRegistry.Resolve(
            RulesetRegistry.CampaignRulesetId);
    }
}

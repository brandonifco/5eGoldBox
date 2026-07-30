using System.Collections.Concurrent;
using FiveEGoldBox.Application.Content;
using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Application.Scenarios;

/// Finds the rules a scenario is authored against.
///
/// A scenario has always declared a RulesetId; until now nothing resolved it,
/// so the only ruleset in the code was reached for by name. This is the same
/// seam ScenarioDefinitionRegistry provides for content, and it exists for the
/// same reason: a second scenario should name what it needs rather than
/// inherit whatever the first one used.
///
/// One campaign means one roster and one body of rules, so both current
/// scenarios resolve to the same ruleset. That is a property of this campaign,
/// not of the registry.
internal static class RulesetRegistry
{
    /// The rules this campaign plays by — its classes, races, skills, and the
    /// gear both the party and the opposition draw on.
    internal const string CampaignRulesetId = "ruleset.campaign";

    private static readonly IReadOnlyDictionary<string, Func<ValidatedRuleset>>
        Factories = new Dictionary<string, Func<ValidatedRuleset>>(
            StringComparer.Ordinal)
        {
            [CampaignRulesetId] = LoadCampaignRuleset
        };

    private static readonly ConcurrentDictionary<string, ValidatedRuleset>
        Cache = new(StringComparer.Ordinal);

    internal static ValidatedRuleset Resolve(
        string rulesetId)
    {
        if (string.IsNullOrWhiteSpace(rulesetId))
        {
            throw new ArgumentException(
                "Ruleset ID is required.",
                nameof(rulesetId));
        }

        return Cache.GetOrAdd(rulesetId, CreateValidated);
    }

    private static ValidatedRuleset CreateValidated(
        string rulesetId)
    {
        if (!Factories.TryGetValue(
            rulesetId,
            out Func<ValidatedRuleset>? factory))
        {
            throw new ArgumentException(
                $"No ruleset is registered under '{rulesetId}'.",
                nameof(rulesetId));
        }

        return factory();
    }

    private static ValidatedRuleset LoadCampaignRuleset()
    {
        string packPath = DataDirectoryLocator.ResolveDataFilePath(
            Path.Combine("rulesets", "campaign", "core.json"));

        return RulesetPackLoader.Load([packPath]);
    }
}

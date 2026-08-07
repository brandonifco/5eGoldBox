using System.Collections.Concurrent;
using FiveEGoldBox.Application.Content;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Application.Campaigns;

/// Finds a campaign, and the campaign a scenario belongs to.
///
/// The same seam ScenarioDefinitionRegistry and RulesetRegistry provide, for
/// the third kind of content. A scenario does not name its campaign — the
/// campaign names its scenarios — so the reverse lookup is derived rather than
/// declared twice.
internal static class CampaignRegistry
{
    private static readonly IReadOnlyDictionary<string, Func<CampaignDefinition>>
        Factories = new Dictionary<string, Func<CampaignDefinition>>(
            StringComparer.Ordinal)
        {
            [FrontierCampaignIds.CampaignId] = LoadFrontierCampaign
        };

    private static readonly ConcurrentDictionary<string, CampaignDefinition>
        Cache = new(StringComparer.Ordinal);

    internal static CampaignDefinition Resolve(
        string campaignId)
    {
        if (string.IsNullOrWhiteSpace(campaignId))
        {
            throw new ArgumentException(
                "Campaign ID is required.",
                nameof(campaignId));
        }

        return Cache.GetOrAdd(campaignId, CreateValidated);
    }

    /// The campaign that contains this scenario. There is exactly one, because
    /// a campaign owning a scenario is what makes its roster the right party
    /// to attempt it.
    internal static CampaignDefinition ResolveForScenario(
        string scenarioId)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            throw new ArgumentException(
                "Scenario ID is required.",
                nameof(scenarioId));
        }

        CampaignDefinition[] owners = Factories.Keys
            .Select(Resolve)
            .Where(campaign => campaign.ScenarioIds.Contains(
                scenarioId,
                StringComparer.Ordinal))
            .ToArray();

        return owners.Length switch
        {
            1 => owners[0],
            0 => throw new ArgumentException(
                $"No campaign contains scenario '{scenarioId}'.",
                nameof(scenarioId)),
            _ => throw new ArgumentException(
                $"Scenario '{scenarioId}' is claimed by {owners.Length} campaigns.",
                nameof(scenarioId))
        };
    }

    private static CampaignDefinition CreateValidated(
        string campaignId)
    {
        if (!Factories.TryGetValue(
            campaignId,
            out Func<CampaignDefinition>? factory))
        {
            throw new ArgumentException(
                $"No campaign is registered under '{campaignId}'.",
                nameof(campaignId));
        }

        CampaignDefinition campaign = factory();
        CampaignDefinitionValidator.Validate(campaign, ResolveRulesetOrNull(campaign));

        return campaign;
    }

    /// Same defensive resolution ScenarioDefinitionRegistry and
    /// ContentPackValidation already use: a campaign naming a ruleset that
    /// cannot be resolved is not made *more* valid by that failure, but the
    /// cross-pack checks simply have nothing to check against, so they are
    /// skipped rather than turned into a second, confusing error.
    private static ValidatedRuleset? ResolveRulesetOrNull(
        CampaignDefinition campaign)
    {
        try
        {
            return RulesetRegistry.Resolve(campaign.RulesetId);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static CampaignDefinition LoadFrontierCampaign()
    {
        string packPath = DataDirectoryLocator.ResolveDataFilePath(
            Path.Combine("campaigns", "frontier", "campaign.json"));

        return CampaignPackLoader.Load(packPath);
    }
}

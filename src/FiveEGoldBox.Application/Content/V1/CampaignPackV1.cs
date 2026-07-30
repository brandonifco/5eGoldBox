namespace FiveEGoldBox.Application.Content.V1;

/// One campaign as it exists on disk. A campaign is authored as a whole
/// roster, the same way a scenario is authored as a whole adventure -- no
/// merge semantics, unlike ruleset content packs.
internal sealed record CampaignPackV1
{
    public required int FormatVersion { get; init; }

    public required string CampaignId { get; init; }

    public required string DisplayName { get; init; }

    public required string RulesetId { get; init; }

    public required int ActivePartySize { get; init; }

    public required IReadOnlyList<CampaignCharacterDefinitionV1> Roster { get; init; }

    public required IReadOnlyList<string> ScenarioIds { get; init; }
}

namespace FiveEGoldBox.Application.Campaigns;

/// Named IDs for the Frontier campaign that other code (the party factory,
/// tests) refers to by name rather than by a bare string literal.
///
/// These used to live on FrontierCampaignContent alongside the C# that
/// built the roster itself. That builder is gone --
/// data/campaigns/frontier/campaign.json is authoritative now, loaded
/// through CampaignRegistry -- but the IDs are not hardcoded content, just
/// names for entries the JSON already declares.
internal static class FrontierCampaignIds
{
    internal const string CampaignId = "campaign.frontier";

    internal const string PartyId = "party.player";
}

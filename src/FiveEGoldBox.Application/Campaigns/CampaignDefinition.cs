namespace FiveEGoldBox.Application.Campaigns;

/// A campaign: a party, the rules it plays by, and the adventures it may
/// attempt.
///
/// This is the thing three placeholders were standing in for. Party
/// composition was decided to be campaign-declared rather than scenario or
/// ruleset content, and until now there was nowhere to declare it — so the
/// roster lived in one scenario's content class, the size rule lived in a
/// validator named after that scenario, and the two were bolted together by a
/// lookup keyed on scenario ID.
internal sealed record CampaignDefinition
{
    internal required string CampaignId { get; init; }

    internal required string DisplayName { get; init; }

    /// The rules everything in this campaign is authored against.
    internal required string RulesetId { get; init; }

    /// How many of the roster take the field at once.
    internal required int ActivePartySize { get; init; }

    /// Everyone the campaign starts with. The first ActivePartySize of them
    /// form the starting party; the rest are the reserve.
    internal required IReadOnlyList<CampaignCharacterDefinition> Roster { get; init; }

    /// The adventures this campaign contains.
    internal required IReadOnlyList<string> ScenarioIds { get; init; }
}

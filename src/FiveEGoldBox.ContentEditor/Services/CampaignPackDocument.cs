using System.Text.Json;
using FiveEGoldBox.Application.Content.V1;

namespace FiveEGoldBox.ContentEditor.Services;

/// A campaign file, parsed once alongside its raw bytes -- the campaign
/// equivalent of ScenarioPackDocument, and separate from it for the reason
/// the plan called out: a campaign is a different root shape in a different
/// directory, not another section of a scenario.
///
/// The V1 DTOs are internal to FiveEGoldBox.Application, reachable here only
/// through the existing InternalsVisibleTo grant, which is why everything on
/// this type is internal rather than public.
internal sealed class CampaignPackDocument
{
    private readonly CampaignPackV1 _pack;
    private readonly byte[] _rawBytes;

    private CampaignPackDocument(
        CampaignPackV1 pack,
        byte[] rawBytes)
    {
        _pack = pack;
        _rawBytes = rawBytes;
    }

    internal static CampaignPackDocument Read(
        string campaignFilePath)
    {
        byte[] rawBytes = File.ReadAllBytes(campaignFilePath);

        CampaignPackV1 pack = JsonSerializer.Deserialize<CampaignPackV1>(
            rawBytes,
            ContentSerialization.Options)
            ?? throw new InvalidOperationException(
                $"'{campaignFilePath}' did not deserialize into a campaign pack.");

        return new CampaignPackDocument(pack, rawBytes);
    }

    internal byte[] RawBytes => _rawBytes;

    internal string CampaignId => _pack.CampaignId;

    internal string DisplayName => _pack.DisplayName;

    internal string RulesetId => _pack.RulesetId;

    internal int ActivePartySize => _pack.ActivePartySize;

    internal IReadOnlyList<CampaignCharacterDefinitionV1> Roster => _pack.Roster;

    internal IReadOnlyList<string> ScenarioIds => _pack.ScenarioIds;
}

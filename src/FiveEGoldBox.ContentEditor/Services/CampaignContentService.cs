using FiveEGoldBox.Application.Content;
using FiveEGoldBox.Application.Content.V1;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.ContentEditor.Services;

/// The campaign equivalent of ScenarioContentService: same atomic
/// validate-then-commit discipline (render just the array being changed,
/// splice it into a fresh copy of the file's bytes, validate the temp file
/// through the public ContentPackValidation facade, and only then replace the
/// real file), so a save that fails validation leaves the real file
/// completely untouched.
///
/// Separate from ScenarioContentService rather than an extension of it: a
/// campaign is a different root shape in a different directory, and the two
/// share nothing but the splicer and the layout primitives.
public sealed class CampaignContentService
{
    internal IReadOnlyList<CampaignSummary> LoadCampaigns()
    {
        return RepositoryLocator.ResolveCampaignPackPaths()
            .Select(path =>
            {
                CampaignPackDocument document = CampaignPackDocument.Read(path);

                return new CampaignSummary(
                    document.CampaignId,
                    document.DisplayName,
                    document.RulesetId,
                    document.ActivePartySize,
                    path);
            })
            .ToList();
    }

    internal CampaignSummary? FindCampaign(
        string campaignId)
    {
        return LoadCampaigns()
            .FirstOrDefault(campaign => campaign.CampaignId == campaignId);
    }

    internal IReadOnlyList<CampaignCharacterDefinitionV1> LoadRoster(
        string campaignFilePath)
    {
        return CampaignPackDocument.Read(campaignFilePath).Roster;
    }

    internal CampaignCharacterDefinitionV1? FindRosterMember(
        string campaignFilePath,
        string partyMemberId)
    {
        return LoadRoster(campaignFilePath)
            .FirstOrDefault(member => member.PartyMemberId == partyMemberId);
    }

    internal ValidationResult SaveRosterMember(
        string campaignFilePath,
        CampaignCharacterDefinitionV1 member)
    {
        List<CampaignCharacterDefinitionV1> updated = Upsert(
            LoadRoster(campaignFilePath),
            member,
            m => m.PartyMemberId);

        return WriteAndValidate(campaignFilePath, updated);
    }

    internal ValidationResult DeleteRosterMember(
        string campaignFilePath,
        string partyMemberId)
    {
        List<CampaignCharacterDefinitionV1> updated = LoadRoster(campaignFilePath)
            .Where(member => member.PartyMemberId != partyMemberId)
            .ToList();

        return WriteAndValidate(campaignFilePath, updated);
    }

    /// How many of the roster actually take the field. A roster may hold more
    /// (this campaign keeps a Barbarian and Ranger in reserve), so deleting a
    /// member is only rejected once the roster can no longer field a party.
    internal int LoadActivePartySize(
        string campaignFilePath)
    {
        return CampaignPackDocument.Read(campaignFilePath).ActivePartySize;
    }

    private static List<T> Upsert<T>(
        IReadOnlyList<T> existing,
        T value,
        Func<T, string> idSelector)
    {
        string id = idSelector(value);
        List<T> updated = [.. existing];
        int index = updated.FindIndex(item => idSelector(item) == id);

        if (index >= 0)
        {
            updated[index] = value;
        }
        else
        {
            updated.Add(value);
        }

        return updated;
    }

    private static ValidationResult WriteAndValidate(
        string campaignFilePath,
        IReadOnlyList<CampaignCharacterDefinitionV1> updatedRoster)
    {
        byte[] original = File.ReadAllBytes(campaignFilePath);
        byte[] updated = RulesetJsonSplicer.ReplaceRootPropertyValue(
            original,
            "Roster",
            CampaignJsonFormatting.RenderRoster(updatedRoster));

        string tempPath = campaignFilePath + ".tmp-" + Guid.NewGuid().ToString("N");

        try
        {
            File.WriteAllBytes(tempPath, updated);

            ValidationResult validation = ContentPackValidation.ValidateCampaignPack(tempPath);

            if (validation.IsValid)
            {
                File.Copy(tempPath, campaignFilePath, overwrite: true);
            }

            return validation;
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}

internal sealed record CampaignSummary(
    string CampaignId,
    string DisplayName,
    string RulesetId,
    int ActivePartySize,
    string FilePath);

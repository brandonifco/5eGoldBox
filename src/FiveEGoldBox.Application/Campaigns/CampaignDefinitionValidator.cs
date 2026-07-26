namespace FiveEGoldBox.Application.Campaigns;

/// Checks a campaign is coherent before anything plays it.
internal static class CampaignDefinitionValidator
{
    internal static void Validate(
        CampaignDefinition campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);

        if (campaign.ActivePartySize <= 0)
        {
            throw new ArgumentException(
                $"Campaign '{campaign.CampaignId}' must field at least one character.",
                nameof(campaign));
        }

        if (campaign.Roster.Count < campaign.ActivePartySize)
        {
            throw new ArgumentException(
                $"Campaign '{campaign.CampaignId}' fields {campaign.ActivePartySize} characters but its roster holds {campaign.Roster.Count}.",
                nameof(campaign));
        }

        if (campaign.ScenarioIds.Count == 0)
        {
            throw new ArgumentException(
                $"Campaign '{campaign.CampaignId}' contains no scenarios.",
                nameof(campaign));
        }

        RequireUnique(
            campaign,
            campaign.ScenarioIds,
            "scenario ID");
        RequireUnique(
            campaign,
            campaign.Roster.Select(character => character.PartyMemberId),
            "party-member ID");
        RequireUnique(
            campaign,
            campaign.Roster.Select(character => character.CharacterDefinitionId),
            "character-definition ID");

        foreach (CampaignCharacterDefinition character in campaign.Roster)
        {
            ValidateCharacter(campaign, character);
        }
    }

    private static void ValidateCharacter(
        CampaignDefinition campaign,
        CampaignCharacterDefinition character)
    {
        string subject =
            $"Character '{character.PartyMemberId}' in campaign '{campaign.CampaignId}'";

        if (character.MaximumHitPoints <= 0)
        {
            throw new ArgumentException(
                $"{subject} has no hit points.",
                nameof(campaign));
        }

        if (character.CurrentHitPoints < 0
            || character.CurrentHitPoints > character.MaximumHitPoints)
        {
            throw new ArgumentException(
                $"{subject} starts outside its own hit-point range.",
                nameof(campaign));
        }

        if (character.TemporaryHitPoints < 0)
        {
            throw new ArgumentException(
                $"{subject} starts with negative temporary hit points.",
                nameof(campaign));
        }

        if (character.Ammunition is null)
        {
            return;
        }

        // Ammunition belongs to a weapon the character actually carries,
        // otherwise it is spent by nothing.
        if (!character.EquippedWeaponIds.Contains(
            character.Ammunition.WeaponId,
            StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"{subject} carries ammunition for '{character.Ammunition.WeaponId}', which it does not wield.",
                nameof(campaign));
        }

        if (character.Ammunition.Quantity < 0)
        {
            throw new ArgumentException(
                $"{subject} starts with negative ammunition.",
                nameof(campaign));
        }
    }

    private static void RequireUnique(
        CampaignDefinition campaign,
        IEnumerable<string> values,
        string subject)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (string value in values)
        {
            if (!seen.Add(value))
            {
                throw new ArgumentException(
                    $"Campaign '{campaign.CampaignId}' declares {subject} '{value}' more than once.",
                    nameof(campaign));
            }
        }
    }
}

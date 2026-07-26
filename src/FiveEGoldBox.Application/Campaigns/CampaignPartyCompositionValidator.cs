using FiveEGoldBox.Application.Parties;

namespace FiveEGoldBox.Application.Campaigns;

/// Checks a party against the campaign that raised it.
///
/// This used to be named after a scenario and hold that scenario's roster as
/// constants — three members, one each of three classes, ammunition on exactly
/// one of them. None of that was ever a scenario's business: party composition
/// is campaign-declared, and there was simply nowhere to declare it.
///
/// Now the campaign says who is on its roster and how many take the field, and
/// this checks the party is one that campaign could have produced. The engine
/// still owns whether a party is internally coherent — unique identities, sane
/// health, well-formed ammunition — which ApplicationSessionRules validates
/// for every party regardless of campaign.
internal static class CampaignPartyCompositionValidator
{
    internal static void Validate(
        CampaignDefinition campaign,
        PartyState party)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(party.Members);

        if (party.Members.Count != campaign.ActivePartySize)
        {
            throw new ArgumentException(
                $"Campaign '{campaign.CampaignId}' fields {campaign.ActivePartySize} characters, but the party has {party.Members.Count}.",
                nameof(party));
        }

        foreach (PartyMemberState member in party.Members)
        {
            ArgumentNullException.ThrowIfNull(member);
            ValidateMember(campaign, member);
        }
    }

    private static void ValidateMember(
        CampaignDefinition campaign,
        PartyMemberState member)
    {
        CampaignCharacterDefinition character = campaign.Roster
            .FirstOrDefault(candidate => string.Equals(
                candidate.CharacterDefinitionId,
                member.CharacterDefinitionId,
                StringComparison.Ordinal))
            ?? throw new ArgumentException(
                $"Character '{member.CharacterDefinitionId}' is not on campaign '{campaign.CampaignId}' roster.",
                nameof(member));

        if (!string.Equals(
            member.ClassId,
            character.ClassId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Character '{member.CharacterDefinitionId}' is a {character.ClassId} on the roster but a {member.ClassId} in the party.",
                nameof(member));
        }

        if (member.Health.HitPoints.MaximumHitPoints
            != character.MaximumHitPoints)
        {
            throw new ArgumentException(
                $"Character '{member.CharacterDefinitionId}' has {member.Health.HitPoints.MaximumHitPoints} maximum hit points but its build gives {character.MaximumHitPoints}.",
                nameof(member));
        }

        ValidateAmmunition(character, member);
        ValidateResources(campaign, character, member);
    }

    /// A character has exactly the resources its class grants, at exactly the
    /// ceilings the class sets. How much is left is play.
    private static void ValidateResources(
        CampaignDefinition campaign,
        CampaignCharacterDefinition character,
        PartyMemberState member)
    {
        IReadOnlyDictionary<string, int> granted =
            CampaignResourceGrants.ForCharacter(campaign, character);

        foreach (CharacterResourceState resource in member.Resources)
        {
            if (!granted.TryGetValue(
                    resource.ResourceId,
                    out int maximum))
            {
                throw new ArgumentException(
                    $"Character '{member.CharacterDefinitionId}' holds resource '{resource.ResourceId}', which its build does not grant.",
                    nameof(member));
            }

            if (resource.Maximum != maximum)
            {
                throw new ArgumentException(
                    $"Character '{member.CharacterDefinitionId}' has {resource.Maximum} of '{resource.ResourceId}' but its build grants {maximum}.",
                    nameof(member));
            }
        }

        foreach (string resourceId in granted.Keys
            .Where(resourceId => !member.Resources.Any(resource =>
                string.Equals(
                    resource.ResourceId,
                    resourceId,
                    StringComparison.Ordinal))))
        {
            throw new ArgumentException(
                $"Character '{member.CharacterDefinitionId}' is missing resource '{resourceId}', which its build grants.",
                nameof(member));
        }
    }

    /// A character carries ammunition exactly when its build says so, and for
    /// the weapon its build names. How much is left is play, not composition.
    private static void ValidateAmmunition(
        CampaignCharacterDefinition character,
        PartyMemberState member)
    {
        if (character.Ammunition is null)
        {
            if (member.Ammunition is not null)
            {
                throw new ArgumentException(
                    $"Character '{member.CharacterDefinitionId}' carries ammunition its build does not grant.",
                    nameof(member));
            }

            return;
        }

        AmmunitionState ammunition =
            member.Ammunition
            ?? throw new ArgumentException(
                $"Character '{member.CharacterDefinitionId}' is missing the ammunition its build grants.",
                nameof(member));

        if (!string.Equals(
                ammunition.WeaponId,
                character.Ammunition.WeaponId,
                StringComparison.Ordinal)
            || !string.Equals(
                ammunition.AmmunitionItemId,
                character.Ammunition.AmmunitionItemId,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Character '{member.CharacterDefinitionId}' carries ammunition that does not match its build.",
                nameof(member));
        }
    }
}

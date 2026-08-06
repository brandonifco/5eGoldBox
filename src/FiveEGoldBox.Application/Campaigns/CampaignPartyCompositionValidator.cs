using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Validation;

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
        if (member.CustomBuild is not null)
        {
            ValidateCustomBuildMember(campaign, member);
            return;
        }

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

    /// The custom-build equivalent of ValidateMember above: there is no
    /// roster entry to check against, so the build resolves through the same
    /// CharacterResolver every other consumer of a build uses, and that
    /// resolution is what composition is checked against instead.
    private static void ValidateCustomBuildMember(
        CampaignDefinition campaign,
        PartyMemberState member)
    {
        CharacterDraft build = member.CustomBuild!;

        if (!string.Equals(
            member.ClassId,
            build.ClassId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Character '{member.CharacterDefinitionId}' is a {build.ClassId} in its build but a {member.ClassId} in the party.",
                nameof(member));
        }

        ValidatedRuleset ruleset =
            RulesetRegistry.Resolve(campaign.RulesetId);
        CharacterResolver resolver = new(ruleset);
        ValidationResult validation = resolver.Validate(build);

        if (!validation.IsValid)
        {
            string reasons = string.Join(
                "; ",
                validation.Issues
                    .Where(issue => issue.Severity == ValidationSeverity.Error)
                    .Select(issue => issue.Message));

            throw new ArgumentException(
                $"Character '{member.CharacterDefinitionId}' does not resolve to a legal character: {reasons}",
                nameof(member));
        }

        CharacterSnapshot snapshot = resolver.Resolve(build);

        if (member.Health.HitPoints.MaximumHitPoints
            != snapshot.MaxHitPoints)
        {
            throw new ArgumentException(
                $"Character '{member.CharacterDefinitionId}' has {member.Health.HitPoints.MaximumHitPoints} maximum hit points but its build gives {snapshot.MaxHitPoints}.",
                nameof(member));
        }

        ValidateCustomBuildAmmunition(ruleset, build, member);

        IReadOnlyDictionary<string, int> granted =
            CampaignResourceGrants.ForClass(campaign.RulesetId, build.ClassId!);

        ValidateResourceGrants(granted, member);
    }

    /// Mirrors ValidateAmmunition, but a custom build has no declared
    /// Ammunition field to read -- which of its equipped weapons need
    /// ammunition is derived from the ruleset's own weapon definitions
    /// instead. A build equipping more than one such weapon can't be
    /// represented by PartyMemberState's single Ammunition slot; that's
    /// rejected here rather than silently picking one.
    private static void ValidateCustomBuildAmmunition(
        ValidatedRuleset ruleset,
        CharacterDraft build,
        PartyMemberState member)
    {
        List<WeaponDefinition> ammunitionWeapons = build.EquippedWeaponIds
            .Select(weaponId => ruleset.Definition.Weapons
                .FirstOrDefault(weapon => string.Equals(
                    weapon.Id,
                    weaponId,
                    StringComparison.Ordinal)))
            .Where(weapon => weapon?.AmmunitionItemId is not null)
            .Select(weapon => weapon!)
            .ToList();

        if (ammunitionWeapons.Count > 1)
        {
            throw new ArgumentException(
                $"Character '{member.CharacterDefinitionId}' equips more than one weapon that requires ammunition, which its party state cannot represent.",
                nameof(member));
        }

        if (ammunitionWeapons.Count == 0)
        {
            if (member.Ammunition is not null)
            {
                throw new ArgumentException(
                    $"Character '{member.CharacterDefinitionId}' carries ammunition its build does not grant.",
                    nameof(member));
            }

            return;
        }

        WeaponDefinition ammunitionWeapon = ammunitionWeapons[0];

        AmmunitionState ammunition =
            member.Ammunition
            ?? throw new ArgumentException(
                $"Character '{member.CharacterDefinitionId}' is missing the ammunition its build grants.",
                nameof(member));

        if (!string.Equals(
                ammunition.WeaponId,
                ammunitionWeapon.Id,
                StringComparison.Ordinal)
            || !string.Equals(
                ammunition.AmmunitionItemId,
                ammunitionWeapon.AmmunitionItemId,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Character '{member.CharacterDefinitionId}' carries ammunition that does not match its build.",
                nameof(member));
        }
    }

    /// The shared half of ValidateResources below: given what a class grants,
    /// check a member's live resources match exactly. Both the roster and
    /// custom-build paths compute "granted" differently but check it the
    /// same way once they have it.
    private static void ValidateResourceGrants(
        IReadOnlyDictionary<string, int> granted,
        PartyMemberState member)
    {
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

    /// A character has exactly the resources its class grants, at exactly the
    /// ceilings the class sets. How much is left is play.
    private static void ValidateResources(
        CampaignDefinition campaign,
        CampaignCharacterDefinition character,
        PartyMemberState member)
    {
        IReadOnlyDictionary<string, int> granted =
            CampaignResourceGrants.ForClass(campaign.RulesetId, character.ClassId);

        ValidateResourceGrants(granted, member);
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

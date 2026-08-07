using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Application.Campaigns;

/// Cross-pack checks: every id a roster entry names must resolve in the
/// ruleset the campaign declares.
///
/// Without these a campaign naming a class, race, background, skill, weapon,
/// spell or ammunition item that does not exist loaded completely clean and
/// only failed much later, during character resolution, with an error that
/// pointed at the resolver rather than at the campaign that caused it. The
/// content editor's own roster pickers were the only thing keeping those ids
/// honest, which is not a guarantee -- a hand-edited file has no pickers.
///
/// Deliberately mirrors ScenarioDefinitionValidator's own monster-id check:
/// the ruleset is optional and a null one skips these entirely, since an
/// unresolvable ruleset means there is nothing to check against rather than
/// that every id is wrong.
internal static partial class CampaignDefinitionValidator
{
    /// Matches the nameof(campaign) every other throw in this validator
    /// uses; spelled out here because the shared helper below doesn't take
    /// the campaign itself.
    private const string CampaignParameterName = "campaign";

    private static void ValidateCharacterAgainstRuleset(
        CampaignDefinition campaign,
        CampaignCharacterDefinition character,
        ValidatedRuleset? ruleset)
    {
        if (ruleset is null)
        {
            return;
        }

        string subject =
            $"Character '{character.PartyMemberId}' in campaign '{campaign.CampaignId}'";

        RulesetDefinition definition = ruleset.Definition;

        RequireResolves(
            subject,
            "race",
            character.RaceId,
            definition.Races.Select(race => race.Id));

        RequireResolves(
            subject,
            "class",
            character.ClassId,
            definition.Classes.Select(characterClass => characterClass.Id));

        RequireResolves(
            subject,
            "background",
            character.BackgroundId,
            definition.Backgrounds.Select(background => background.Id));

        foreach (string skillId in character.SelectedSkillIds)
        {
            RequireResolves(
                subject,
                "skill",
                skillId,
                definition.Skills.Select(skill => skill.Id));
        }

        foreach (string weaponId in character.EquippedWeaponIds)
        {
            RequireResolves(
                subject,
                "weapon",
                weaponId,
                definition.Weapons.Select(weapon => weapon.Id));
        }

        foreach (string spellId in character.PreparedSpellIds)
        {
            RequireResolves(
                subject,
                "prepared spell",
                spellId,
                definition.Spells.Select(spell => spell.Id));
        }

        if (character.Ammunition is not null)
        {
            // The weapon is already checked against EquippedWeaponIds by the
            // self-consistency pass; what that cannot catch is the item the
            // ammunition is actually made of.
            RequireResolves(
                subject,
                "ammunition item",
                character.Ammunition.AmmunitionItemId,
                definition.EquipmentItems.Select(item => item.Id));
        }
    }

    private static void RequireResolves(
        string subject,
        string kind,
        string id,
        IEnumerable<string> declaredIds)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException(
                $"{subject} names no {kind}.",
                CampaignParameterName);
        }

        if (!declaredIds.Contains(id, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"{subject} names {kind} '{id}', which its ruleset does not define.",
                CampaignParameterName);
        }
    }
}

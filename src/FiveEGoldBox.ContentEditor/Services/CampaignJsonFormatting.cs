using System.Globalization;
using FiveEGoldBox.Application.Content.V1;
using static FiveEGoldBox.ContentEditor.Services.JsonLayoutPrimitives;

namespace FiveEGoldBox.ContentEditor.Services;

/// Hand-rolled JSON layout for campaign roster content, matching
/// data/campaigns/*/campaign.json's existing hand-authored formatting
/// byte-for-byte (see CampaignNoOpSaveFormattingTests).
///
/// Three properties are omitted rather than written with their default,
/// which is the whole reason this needs a hand-rolled renderer instead of a
/// serializer: TemporaryHitPoints is a non-nullable int that only the
/// Fighter carries (writing "TemporaryHitPoints": 0 for everyone else would
/// silently rewrite five of six roster entries), Ammunition is null for
/// anyone without a ranged weapon, and PreparedSpellIds is empty for every
/// non-caster.
///
/// Skill/weapon/spell lists follow the same "inline at one or two entries,
/// exploded at three or more" convention scenario content uses -- confirmed
/// against real content, where the Wizard's two prepared spells stay inline
/// and the Cleric's four explode -- so they share
/// JsonLayoutPrimitives.RenderLooseStringList rather than redefining it.
internal static class CampaignJsonFormatting
{
    internal static string RenderRoster(
        IReadOnlyList<CampaignCharacterDefinitionV1> roster)
    {
        return RenderTopLevelArray(roster, RenderCharacter);
    }

    private static string RenderCharacter(
        CampaignCharacterDefinitionV1 character,
        int column)
    {
        List<(string Key, string? Value)> fields =
        [
            ("PartyMemberId", JsonString(character.PartyMemberId)),
            ("CharacterDefinitionId", JsonString(character.CharacterDefinitionId)),
            ("DisplayName", JsonString(character.DisplayName)),
            ("RaceId", JsonString(character.RaceId)),
            ("ClassId", JsonString(character.ClassId)),
            ("BackgroundId", JsonString(character.BackgroundId)),
            ("AbilityScores", RenderAbilityScores(character.AbilityScores, column + 4)),
            ("SelectedSkillIds", RenderLooseStringList(character.SelectedSkillIds, column + 8)),
            ("EquippedWeaponIds", RenderLooseStringList(character.EquippedWeaponIds, column + 8)),
            ("ZeroHitPointPolicy", JsonString(character.ZeroHitPointPolicy.ToString())),
            ("MaximumHitPoints", character.MaximumHitPoints.ToString(CultureInfo.InvariantCulture)),
            ("CurrentHitPoints", character.CurrentHitPoints.ToString(CultureInfo.InvariantCulture)),
            ("TemporaryHitPoints", character.TemporaryHitPoints == 0
                ? null
                : character.TemporaryHitPoints.ToString(CultureInfo.InvariantCulture)),
            ("Ammunition", character.Ammunition is null
                ? null
                : RenderAmmunition(character.Ammunition, column + 4)),
            ("PreparedSpellIds", RenderLooseStringList(character.PreparedSpellIds, column + 8))
        ];

        return RenderExplodedObject(fields, column);
    }

    /// Written in the DTO's own declared ability order rather than the
    /// dictionary's enumeration order, so a round trip can't reorder the six
    /// scores according to however they happened to be inserted.
    private static string RenderAbilityScores(
        IReadOnlyDictionary<AbilityV1, int> abilityScores,
        int column)
    {
        List<(string Key, string? Value)> fields = Enum.GetValues<AbilityV1>()
            .Where(abilityScores.ContainsKey)
            .Select(ability => (
                ability.ToString(),
                (string?)abilityScores[ability].ToString(CultureInfo.InvariantCulture)))
            .ToList();

        return RenderExplodedObject(fields, column);
    }

    private static string RenderAmmunition(
        CampaignAmmunitionDefinitionV1 ammunition,
        int column)
    {
        List<(string Key, string? Value)> fields =
        [
            ("WeaponId", JsonString(ammunition.WeaponId)),
            ("AmmunitionItemId", JsonString(ammunition.AmmunitionItemId)),
            ("Quantity", ammunition.Quantity.ToString(CultureInfo.InvariantCulture))
        ];

        return RenderExplodedObject(fields, column);
    }
}

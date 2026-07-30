using FiveEGoldBox.Application.Campaigns;

namespace FiveEGoldBox.Application.Content.V1;

/// Turns one campaign content pack into the internal CampaignDefinition the
/// engine actually loads. Like ScenarioPackMapper, there is no merge step --
/// a campaign is authored as one whole roster, not assembled from packs.
internal static class CampaignPackMapper
{
    internal const int FormatVersion = 1;

    internal static CampaignDefinition ToRuntime(
        CampaignPackV1 pack)
    {
        ArgumentNullException.ThrowIfNull(pack);

        return new CampaignDefinition
        {
            CampaignId = pack.CampaignId,
            DisplayName = pack.DisplayName,
            RulesetId = pack.RulesetId,
            ActivePartySize = pack.ActivePartySize,
            Roster = pack.Roster
                .Select(ToRuntimeCharacter)
                .ToArray(),
            ScenarioIds = pack.ScenarioIds.ToArray()
        };
    }

    private static CampaignCharacterDefinition ToRuntimeCharacter(
        CampaignCharacterDefinitionV1 character)
    {
        return new CampaignCharacterDefinition
        {
            PartyMemberId = character.PartyMemberId,
            CharacterDefinitionId = character.CharacterDefinitionId,
            DisplayName = character.DisplayName,
            RaceId = character.RaceId,
            ClassId = character.ClassId,
            BackgroundId = character.BackgroundId,
            AbilityScores = character.AbilityScores
                .ToDictionary(
                    entry => RulesetPackMapper.ToRuntimeAbility(entry.Key),
                    entry => entry.Value),
            SelectedSkillIds = character.SelectedSkillIds.ToArray(),
            EquippedWeaponIds = character.EquippedWeaponIds.ToArray(),
            ZeroHitPointPolicy = ScenarioPackMapper.ToRuntimeZeroHitPointPolicy(
                character.ZeroHitPointPolicy),
            MaximumHitPoints = character.MaximumHitPoints,
            CurrentHitPoints = character.CurrentHitPoints,
            TemporaryHitPoints = character.TemporaryHitPoints,
            Ammunition = character.Ammunition is null
                ? null
                : ToRuntimeAmmunition(character.Ammunition),
            PreparedSpellIds = character.PreparedSpellIds.ToArray()
        };
    }

    private static CampaignAmmunitionDefinition ToRuntimeAmmunition(
        CampaignAmmunitionDefinitionV1 ammunition)
    {
        return new CampaignAmmunitionDefinition
        {
            WeaponId = ammunition.WeaponId,
            AmmunitionItemId = ammunition.AmmunitionItemId,
            Quantity = ammunition.Quantity
        };
    }
}

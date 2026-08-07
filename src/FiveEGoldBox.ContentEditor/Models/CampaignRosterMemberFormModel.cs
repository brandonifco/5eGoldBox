using FiveEGoldBox.Application.Content.V1;

namespace FiveEGoldBox.ContentEditor.Models;

/// Editable projection of one campaign roster entry.
///
/// The three optional shapes are modelled as explicit presence rather than
/// as "empty means absent", because for two of them empty and absent are
/// genuinely different: TemporaryHitPoints 0 is a real value that must still
/// be omitted, and an ammunition block with a blank weapon id is malformed
/// rather than absent. HasAmmunition makes the choice explicit instead of
/// inferring it from whether the fields happen to be filled in.
internal sealed class CampaignRosterMemberFormModel
{
    public string PartyMemberId { get; set; } = "";

    public string CharacterDefinitionId { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string RaceId { get; set; } = "";

    public string ClassId { get; set; } = "";

    public string BackgroundId { get; set; } = "";

    public Dictionary<AbilityV1, int> AbilityScores { get; set; } = [];

    public List<string> SelectedSkillIds { get; set; } = [];

    public List<string> EquippedWeaponIds { get; set; } = [];

    public CombatantZeroHitPointPolicyV1 ZeroHitPointPolicy { get; set; }

    public int MaximumHitPoints { get; set; }

    public int CurrentHitPoints { get; set; }

    public int TemporaryHitPoints { get; set; }

    public bool HasAmmunition { get; set; }

    public string AmmunitionWeaponId { get; set; } = "";

    public string AmmunitionItemId { get; set; } = "";

    public int AmmunitionQuantity { get; set; }

    public List<string> PreparedSpellIds { get; set; } = [];

    public static CampaignRosterMemberFormModel FromDefinition(
        CampaignCharacterDefinitionV1 member)
    {
        return new CampaignRosterMemberFormModel
        {
            PartyMemberId = member.PartyMemberId,
            CharacterDefinitionId = member.CharacterDefinitionId,
            DisplayName = member.DisplayName,
            RaceId = member.RaceId,
            ClassId = member.ClassId,
            BackgroundId = member.BackgroundId,
            AbilityScores = new Dictionary<AbilityV1, int>(member.AbilityScores),
            SelectedSkillIds = member.SelectedSkillIds.ToList(),
            EquippedWeaponIds = member.EquippedWeaponIds.ToList(),
            ZeroHitPointPolicy = member.ZeroHitPointPolicy,
            MaximumHitPoints = member.MaximumHitPoints,
            CurrentHitPoints = member.CurrentHitPoints,
            TemporaryHitPoints = member.TemporaryHitPoints,
            HasAmmunition = member.Ammunition is not null,
            AmmunitionWeaponId = member.Ammunition?.WeaponId ?? "",
            AmmunitionItemId = member.Ammunition?.AmmunitionItemId ?? "",
            AmmunitionQuantity = member.Ammunition?.Quantity ?? 0,
            PreparedSpellIds = member.PreparedSpellIds.ToList()
        };
    }

    public CampaignCharacterDefinitionV1 ToDefinition()
    {
        return new CampaignCharacterDefinitionV1
        {
            PartyMemberId = PartyMemberId,
            CharacterDefinitionId = CharacterDefinitionId,
            DisplayName = DisplayName,
            RaceId = RaceId,
            ClassId = ClassId,
            BackgroundId = BackgroundId,
            AbilityScores = AbilityScores,
            SelectedSkillIds = SelectedSkillIds,
            EquippedWeaponIds = EquippedWeaponIds,
            ZeroHitPointPolicy = ZeroHitPointPolicy,
            MaximumHitPoints = MaximumHitPoints,
            CurrentHitPoints = CurrentHitPoints,
            TemporaryHitPoints = TemporaryHitPoints,
            Ammunition = HasAmmunition
                ? new CampaignAmmunitionDefinitionV1
                {
                    WeaponId = AmmunitionWeaponId,
                    AmmunitionItemId = AmmunitionItemId,
                    Quantity = AmmunitionQuantity
                }
                : null,
            PreparedSpellIds = PreparedSpellIds
        };
    }

    public int ScoreFor(AbilityV1 ability) =>
        AbilityScores.TryGetValue(ability, out int score) ? score : 10;

    public void SetScore(
        AbilityV1 ability,
        int score)
    {
        AbilityScores[ability] = score;
    }

    public void ToggleId(
        List<string> ids,
        string id,
        bool isChecked)
    {
        if (isChecked && !ids.Contains(id))
        {
            ids.Add(id);
        }
        else if (!isChecked)
        {
            ids.Remove(id);
        }
    }
}

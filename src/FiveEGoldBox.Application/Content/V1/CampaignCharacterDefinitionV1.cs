namespace FiveEGoldBox.Application.Content.V1;

internal sealed record CampaignCharacterDefinitionV1
{
    public required string PartyMemberId { get; init; }

    public required string CharacterDefinitionId { get; init; }

    public required string DisplayName { get; init; }

    public required string RaceId { get; init; }

    public required string ClassId { get; init; }

    public required string BackgroundId { get; init; }

    public required IReadOnlyDictionary<AbilityV1, int> AbilityScores { get; init; }

    public required IReadOnlyList<string> SelectedSkillIds { get; init; }

    public required IReadOnlyList<string> EquippedWeaponIds { get; init; }

    public required CombatantZeroHitPointPolicyV1 ZeroHitPointPolicy { get; init; }

    public required int MaximumHitPoints { get; init; }

    public required int CurrentHitPoints { get; init; }

    public int TemporaryHitPoints { get; init; }

    public CampaignAmmunitionDefinitionV1? Ammunition { get; init; }

    public IReadOnlyList<string> PreparedSpellIds { get; init; }
        = Array.Empty<string>();
}

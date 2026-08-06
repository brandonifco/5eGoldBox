namespace FiveEGoldBox.Application.Persistence.V1;

/// The full build for a character CharacterCreationRules created. A fresh
/// DTO introduced alongside PartyMemberState.CustomBuild, so unlike most
/// other types in this file nothing here needs an old-document fallback --
/// SavePartyMemberV1.CustomBuild itself is what is optional, not any field
/// within this record.
internal sealed record SaveCharacterDraftV1
{
    public string? Name { get; init; }

    public required int Level { get; init; }

    public string? RaceId { get; init; }

    public string? SubraceId { get; init; }

    public string? ClassId { get; init; }

    public string? BackgroundId { get; init; }

    public required SaveAbilityScoreGenerationMethodV1 AbilityScoreGenerationMethod
    { get; init; }

    public required IReadOnlyList<SaveAbilityScoreV1> BaseAbilityScores { get; init; }

    public required IReadOnlyList<string> SelectedSkillIds { get; init; }

    public string? EquippedArmorId { get; init; }

    public string? EquippedShieldId { get; init; }

    public required IReadOnlyList<string> PreparedSpellIds { get; init; }

    public required IReadOnlyList<string> EquippedWeaponIds { get; init; }

    public required IReadOnlyList<SavePartyInventoryItemV1> InventoryItems { get; init; }

    public required SaveCurrencyV1 Currency { get; init; }
}

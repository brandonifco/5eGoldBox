using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Characters;

public sealed record CharacterDraft
{
    public string? Name { get; init; }

    public int Level { get; init; } = 1;

    public string? RaceId { get; init; }

    public string? SubraceId { get; init; }

    public string? ClassId { get; init; }

    public string? BackgroundId { get; init; }

    public AbilityScoreGenerationMethod AbilityScoreGenerationMethod { get; init; }
        = AbilityScoreGenerationMethod.Manual;

    public IReadOnlyDictionary<Ability, int> BaseAbilityScores { get; init; }
        = new Dictionary<Ability, int>();

    public IReadOnlyList<string> SelectedSkillIds { get; init; }
        = Array.Empty<string>();

    public string? EquippedArmorId { get; init; }

    public string? EquippedShieldId { get; init; }

    /// Spells this character has ready to cast. A choice its player made,
    /// so it belongs to the character rather than the class.
    public IReadOnlyList<string> PreparedSpellIds { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> EquippedWeaponIds { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<InventoryItemDraft> InventoryItems { get; init; }
        = Array.Empty<InventoryItemDraft>();

    public CurrencyAmount Currency { get; init; } = new();
}

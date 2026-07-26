using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Definitions;

/// One thing a spell does when it lands.
///
/// A spell may do several: Magic Missile deals its damage three times over,
/// which is three instances of one effect rather than three effects.
public sealed record SpellEffectDefinition
{
    public required SpellEffectKind Kind { get; init; }

    public required DamageDice Dice { get; init; }

    /// How many separate times the dice are rolled and applied. Each instance
    /// is resolved on its own, so resistance and immunity apply to each rather
    /// than to the total.
    public int Instances { get; init; } = 1;

    /// Whether the caster's spellcasting ability modifier is added. Cure
    /// Wounds adds it; Magic Missile's flat +1 per dart is part of its dice.
    public bool AddsSpellcastingModifier { get; init; }

    /// Required for damage, meaningless for healing.
    public string? DamageType { get; init; }
}

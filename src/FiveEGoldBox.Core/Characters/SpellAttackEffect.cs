using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Characters;

/// One resolved thing a spell does, with the caster's modifier already worked
/// out — the same way a weapon's damage bonus is resolved before combat sees
/// it.
public sealed record SpellAttackEffect
{
    public required SpellEffectKind Kind { get; init; }

    public required DamageDice Dice { get; init; }

    public required int Instances { get; init; }

    /// Added to each instance. Already includes the spellcasting modifier
    /// where the spell adds it.
    public required int FlatBonus { get; init; }

    public string? DamageType { get; init; }
}

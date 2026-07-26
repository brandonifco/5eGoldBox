using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Characters;

/// A spell as this caster can cast it.
///
/// The same shape as WeaponAttack and for the same reason: the encounter
/// resolves an attack without consulting a ruleset, because everything that
/// depends on who is casting has already been worked out. A caster's
/// proficiency, ability modifier, attack bonus and save DC are baked in here.
public sealed record SpellAttack
{
    public required string SpellId { get; init; }

    public required string SpellName { get; init; }

    /// Zero for a cantrip, which costs nothing to cast.
    public required int Level { get; init; }

    public required SpellCastingTime CastingTime { get; init; }

    public required SpellRangeKind RangeKind { get; init; }

    public int? RangeFeet { get; init; }

    public required int MaximumTargets { get; init; }

    public required SpellResolutionKind Resolution { get; init; }

    /// Set when the spell is resisted by a saving throw.
    public Ability? SaveAbility { get; init; }

    public required SpellSaveOutcome SaveOutcome { get; init; }

    /// What the caster adds to a spell attack roll.
    public required int AttackBonus { get; init; }

    /// What a target must beat to resist this caster.
    public required int SaveDc { get; init; }

    public IReadOnlyList<SpellAttackEffect> Effects { get; init; }
        = Array.Empty<SpellAttackEffect>();

    public string? AppliedEffectId { get; init; }

    public bool RequiresConcentration { get; init; }

    public int? DurationRounds { get; init; }
}

using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Definitions;

/// A spell, as authored content.
///
/// Deliberately describes what a spell does rather than how it is cast: range,
/// targeting and resolution are stated, and the encounter rules decide
/// legality from them the same way they do for a weapon. Nothing here is
/// specific to who is casting.
public sealed record SpellDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required SpellCostKind Cost { get; init; }

    /// The slot level this spell is cast at. Zero for a cantrip.
    public required int Level { get; init; }

    public required SpellCastingTime CastingTime { get; init; }

    public required SpellRangeKind RangeKind { get; init; }

    /// Required for a ranged spell, meaningless otherwise.
    public int? RangeFeet { get; init; }

    /// How many creatures may be chosen. One for most; Bless takes up to
    /// three. Ignored when the spell only affects its caster.
    public int MaximumTargets { get; init; } = 1;

    public required SpellTargetDisposition Targets { get; init; }

    public required SpellResolutionKind Resolution { get; init; }

    /// Required when the spell is resolved by a saving throw.
    public Ability? SaveAbility { get; init; }

    public SpellSaveOutcome SaveOutcome { get; init; }
        = SpellSaveOutcome.Negates;

    public IReadOnlyList<SpellEffectDefinition> Effects { get; init; }
        = Array.Empty<SpellEffectDefinition>();

    /// An effect the spell installs on its targets for a while, named by ID.
    /// Bless is the committed example; nothing applies these yet.
    public string? AppliedEffectId { get; init; }

    public bool RequiresConcentration { get; init; }

    /// Rounds the spell lasts. Null for one that resolves and is done.
    public int? DurationRounds { get; init; }
}

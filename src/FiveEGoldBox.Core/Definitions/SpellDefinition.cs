using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Definitions;

/// A spell, as authored content.
///
/// Mostly describes what a spell does rather than how it is cast: range,
/// targeting and resolution are stated, and the encounter rules decide
/// legality from them the same way they do for a weapon. ClassIds is the one
/// exception -- without it, any caster could prepare any spell regardless of
/// class, which is why it exists.
public sealed record SpellDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    /// Player-facing prose for a character-creation screen, so a choice is
    /// more than a bare name to someone who does not already know 5e.
    /// Optional: content that predates this loads unchanged. Original
    /// wording, not sourced from the PHB -- that text is fully copyrighted
    /// (unlike the SRD, which the race/class/background prose draws on)
    /// and isn't eligible to ship as game content.
    public string? Description { get; init; }

    public required SpellCostKind Cost { get; init; }

    /// Which classes may prepare this spell, by class ID. Empty means no
    /// class currently may -- not "any class may," the opposite of what an
    /// absent restriction would otherwise silently mean. A spell every
    /// shipped caster can actually use should always name at least one.
    public IReadOnlyList<string> ClassIds { get; init; }
        = Array.Empty<string>();

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
    /// Bless is the committed example.
    public string? AppliedEffectId { get; init; }

    public bool RequiresConcentration { get; init; }

    /// Rounds the spell lasts. Null for one that resolves and is done.
    public int? DurationRounds { get; init; }
}

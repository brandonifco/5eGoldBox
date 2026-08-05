namespace FiveEGoldBox.Application.Views;

/// One spell a party member has prepared, for a client to draw a real
/// Spellbook screen from -- reference only, the same way the rest of the
/// Character screen is; nothing here casts anything.
public sealed record PreparedSpellViewModel
{
    public required string SpellId { get; init; }

    public required string SpellName { get; init; }

    /// Zero for a cantrip.
    public required int Level { get; init; }

    public required string CastingTime { get; init; }

    public required string Range { get; init; }

    /// "Attack +N", "DC N <Ability> save", or "Automatic" -- however this
    /// spell decides whether it lands, already worked out for this caster.
    public required string ResolutionSummary { get; init; }

    public required bool RequiresConcentration { get; init; }
}

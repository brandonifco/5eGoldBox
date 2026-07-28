namespace FiveEGoldBox.Application.Combat;

public sealed record CombatSpellAttackIntent
{
    public required long ExpectedEncounterRevision { get; init; }

    public required string ActorCombatantId { get; init; }

    public required string SpellId { get; init; }

    public required string TargetCombatantId { get; init; }

    /// Everyone else the cast reaches, beyond the named target. Empty for a
    /// single-target cast.
    public IReadOnlyList<string> AdditionalTargetCombatantIds { get; init; }
        = Array.Empty<string>();
}

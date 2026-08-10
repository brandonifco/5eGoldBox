namespace FiveEGoldBox.Application.Combat;

/// A legal set of targets for a spell that can reach more than one — every
/// entry has already passed its own prerequisite evaluation independently,
/// the way ResolveTargets checks each additional target in Core.
internal sealed record EncounterCombatTargetCombinationOption
{
    public required IReadOnlyList<string> TargetCombatantIds { get; init; }
}

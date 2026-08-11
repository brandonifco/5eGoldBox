using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// The outcome of resolving one player command against an encounter, before the
/// session is updated or automatic processing runs.
internal sealed record EncounterPlayerCommandResolution
{
    /// Encounter state after the command was applied.
    internal required EncounterState State { get; init; }

    /// Random cursor after the command. Equal to the cursor before it for
    /// commands that consume no dice.
    internal required int CursorAfter { get; init; }

    /// The step describing what the command did. Null only when the command
    /// produced nothing to describe — a move whose very first square was
    /// stopped by an opportunity attack that dropped the mover.
    internal required EncounterCombatStepResult? PrimaryStep { get; init; }

    /// Anything that reacted to the command, in the order it happened.
    /// Opportunity attacks today; a Ready trigger would land here too.
    /// Reported separately from the automatic steps that follow because
    /// these happened *during* the player's own command, not after it.
    internal IReadOnlyList<EncounterCombatStepResult> ReactionSteps
    { get; init; } = Array.Empty<EncounterCombatStepResult>();

    /// The submitted command echoed back to the caller.
    internal required EncounterCombatIntentReceipt Receipt { get; init; }
}

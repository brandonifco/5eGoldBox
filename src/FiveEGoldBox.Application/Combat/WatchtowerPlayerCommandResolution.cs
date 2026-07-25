using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// The outcome of resolving one player command against an encounter, before the
/// session is updated or automatic processing runs.
internal sealed record WatchtowerPlayerCommandResolution
{
    /// Encounter state after the command was applied.
    internal required EncounterState State { get; init; }

    /// Random cursor after the command. Equal to the cursor before it for
    /// commands that consume no dice.
    internal required int CursorAfter { get; init; }

    /// The step describing what the command did.
    internal required WatchtowerCombatStepResult PrimaryStep { get; init; }

    /// The submitted command echoed back to the caller.
    internal required WatchtowerCombatIntentReceipt Receipt { get; init; }
}

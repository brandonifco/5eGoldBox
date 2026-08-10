using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// What an opposing combatant intends to do with its turn, decided without
/// mutating session state or consuming randomness. The orchestrator applies
/// the plan in order: movement first, then the attack, then turn advancement.
internal sealed record EncounterTacticsTurnPlan
{
    /// Movement to apply before attacking, or null when the combatant holds
    /// position. Already resolved through Core, so it carries the resulting
    /// encounter state.
    internal EncounterMovementResult? Movement { get; init; }

    /// The attack to resolve after any movement, or null when no legal attack
    /// is available. Resolving it is what consumes randomness.
    internal EncounterTacticsAttackPlan? Attack { get; init; }

    /// Reason recorded when the turn advances. Unused when a planned attack
    /// ends the encounter, because combat completion pre-empts advancement.
    internal required EncounterCombatTurnAdvanceReason TurnAdvanceReason { get; init; }
}

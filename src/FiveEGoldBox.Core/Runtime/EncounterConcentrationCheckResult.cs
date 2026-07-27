namespace FiveEGoldBox.Core.Runtime;

/// What happened when a concentrating combatant took damage.
public sealed record EncounterConcentrationCheckResult
{
    public required string CombatantId { get; init; }

    public required string EffectId { get; init; }

    /// True when the damage left the combatant no longer conscious.
    /// Concentration ends automatically then — 5e never asks an incapacitated
    /// creature to roll for it, so SavingThrow is null in this case.
    public required bool BrokenByIncapacitation { get; init; }

    /// Set unless the check was skipped for BrokenByIncapacitation.
    public EncounterSavingThrowResult? SavingThrow { get; init; }

    public required bool EffectDropped { get; init; }
}

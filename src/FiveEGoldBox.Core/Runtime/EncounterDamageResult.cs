namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterDamageResult
{
    public required string TargetCombatantId { get; init; }

    public required CombatantLifecycleState
        PreviousLifecycleState
    { get; init; }

    public required CombatantLifecycleState
        LifecycleState
    { get; init; }

    public required bool
        ClearedPendingDeathSavingThrow
    { get; init; }

    public required CombatantDamageResult
        CombatantDamage
    { get; init; }

    /// Set when the target was concentrating on something before this damage
    /// landed.
    public EncounterConcentrationCheckResult? ConcentrationCheck
    { get; init; }

    public required EncounterState State { get; init; }
}

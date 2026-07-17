namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterHealingResult
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

    public required CombatantHealingResult
        CombatantHealing
    { get; init; }

    public required EncounterState State { get; init; }
}

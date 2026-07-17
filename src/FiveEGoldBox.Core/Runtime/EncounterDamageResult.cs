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

    public required EncounterState State { get; init; }
}

namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterDeathSavingThrowResult
{
    public required string ActorCombatantId { get; init; }

    public required CombatantLifecycleState
        PreviousLifecycleState
    { get; init; }

    public required CombatantLifecycleState
        LifecycleState
    { get; init; }

    public required CombatantDeathSavingThrowResult
        CombatantDeathSavingThrow
    { get; init; }

    public required EncounterState State { get; init; }
}

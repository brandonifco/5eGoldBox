using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Combat;

internal sealed record EncounterCombatResolutionResult
{
    public required EncounterCombatDecision StartingDecision { get; init; }

    public EncounterCombatIntentReceipt? SubmittedIntent { get; init; }

    public required long PriorEncounterRevision { get; init; }

    public required long ResultingEncounterRevision { get; init; }

    public required int RandomValuesConsumedBefore { get; init; }

    public required int RandomValuesConsumedAfter { get; init; }

    public EncounterCombatStepResult? PrimaryStep { get; init; }

    public required IReadOnlyList<EncounterCombatStepResult> AutomaticSteps { get; init; }

    public required EncounterCombatDecision ResultingDecision { get; init; }

    public required ApplicationSessionState State { get; init; }
}

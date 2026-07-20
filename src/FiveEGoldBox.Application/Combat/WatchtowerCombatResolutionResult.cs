using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Combat;

public sealed record WatchtowerCombatResolutionResult
{
    public required WatchtowerCombatDecision StartingDecision { get; init; }

    public WatchtowerCombatIntentReceipt? SubmittedIntent { get; init; }

    public required long PriorEncounterRevision { get; init; }

    public required long ResultingEncounterRevision { get; init; }

    public required int RandomValuesConsumedBefore { get; init; }

    public required int RandomValuesConsumedAfter { get; init; }

    public WatchtowerCombatStepResult? PrimaryStep { get; init; }

    public required IReadOnlyList<WatchtowerCombatStepResult> AutomaticSteps { get; init; }

    public required WatchtowerCombatDecision ResultingDecision { get; init; }

    public required ApplicationSessionState State { get; init; }
}

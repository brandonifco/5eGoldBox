using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Combat;

/// The outcome of one combat operation: what the caller asked for, everything
/// that happened as a result, and the decision now facing them.
///
/// A single operation can produce many steps. Submitting one command may be
/// followed by automatic processing — enemy turns, death saving throws — which
/// runs until a player decision is required or the encounter ends.
public sealed record CombatResolutionResult
{
    internal CombatResolutionResult(
        CombatDecision startingDecision,
        CombatIntentReceipt? submittedIntent,
        long priorEncounterRevision,
        long resultingEncounterRevision,
        int randomValuesConsumedBefore,
        int randomValuesConsumedAfter,
        CombatStepResult? primaryStep,
        IReadOnlyList<CombatStepResult> automaticSteps,
        CombatDecision resultingDecision,
        ApplicationSessionState state)
    {
        ArgumentNullException.ThrowIfNull(startingDecision);
        ArgumentNullException.ThrowIfNull(automaticSteps);
        ArgumentNullException.ThrowIfNull(resultingDecision);
        ArgumentNullException.ThrowIfNull(state);

        StartingDecision = startingDecision;
        SubmittedIntent = submittedIntent;
        PriorEncounterRevision = priorEncounterRevision;
        ResultingEncounterRevision = resultingEncounterRevision;
        RandomValuesConsumedBefore = randomValuesConsumedBefore;
        RandomValuesConsumedAfter = randomValuesConsumedAfter;
        PrimaryStep = primaryStep;
        AutomaticSteps = Array.AsReadOnly(automaticSteps.ToArray());
        ResultingDecision = resultingDecision;
        State = state;
    }

    /// The decision that was open when the operation began.
    public CombatDecision StartingDecision { get; }

    /// The submitted command, or null when the operation only advanced
    /// automatic processing.
    public CombatIntentReceipt? SubmittedIntent { get; }

    public long PriorEncounterRevision { get; }

    public long ResultingEncounterRevision { get; }

    public int RandomValuesConsumedBefore { get; }

    public int RandomValuesConsumedAfter { get; }

    /// What the submitted command itself did, or null when none was submitted.
    public CombatStepResult? PrimaryStep { get; }

    /// Everything that happened after the command, in order.
    public IReadOnlyList<CombatStepResult> AutomaticSteps { get; }

    /// The decision now facing the caller. Never reports that automatic
    /// processing is still pending — an operation always runs to a stable point.
    public CombatDecision ResultingDecision { get; }

    /// The updated session. Callers must continue from this value.
    public ApplicationSessionState State { get; }
}

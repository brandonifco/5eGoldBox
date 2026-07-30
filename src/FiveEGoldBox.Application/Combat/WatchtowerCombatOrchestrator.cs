using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal static class WatchtowerCombatOrchestrator
{
    internal static WatchtowerCombatResolutionResult AdvanceToDecision(
        ApplicationSessionState source)
    {
        ApplicationSessionState state = WatchtowerCombatSessionMapper.Canonicalize(source);
        WatchtowerCombatDecision startingDecision =
            WatchtowerCombatDecisionFactory.Create(state);
        long priorRevision = WatchtowerCombatSessionMapper.GetEncounter(state).Revision;
        int cursorBefore = state.RandomValuesConsumed;

        if (startingDecision.State is
            CombatDecisionState.PlayerDecisionRequired
            or CombatDecisionState.CombatCompleted)
        {
            return CreateResult(
                startingDecision,
                submittedIntent: null,
                priorRevision,
                cursorBefore,
                primaryStep: null,
                automaticSteps: [],
                state);
        }

        List<WatchtowerCombatStepResult> automaticSteps = [];
        state = WatchtowerAutomaticTurnProcessor.ProcessUntilDecision(state, automaticSteps);

        return CreateResult(
            startingDecision,
            submittedIntent: null,
            priorRevision,
            cursorBefore,
            primaryStep: null,
            automaticSteps,
            state);
    }

    internal static WatchtowerCombatResolutionResult Execute(
        ApplicationSessionState source,
        CombatMoveIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);

        return ExecutePlayerCommand(
            source,
            intent.ExpectedEncounterRevision,
            intent.ActorCombatantId,
            (encounter, randomSeed, cursorBefore) =>
                WatchtowerPlayerCommandResolver.Resolve(
                    encounter,
                    randomSeed,
                    cursorBefore,
                    intent));
    }

    internal static WatchtowerCombatResolutionResult Execute(
        ApplicationSessionState source,
        CombatWeaponAttackIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);

        return ExecutePlayerCommand(
            source,
            intent.ExpectedEncounterRevision,
            intent.ActorCombatantId,
            (encounter, randomSeed, cursorBefore) =>
                WatchtowerPlayerCommandResolver.Resolve(
                    encounter,
                    randomSeed,
                    cursorBefore,
                    intent));
    }

    internal static WatchtowerCombatResolutionResult Execute(
        ApplicationSessionState source,
        CombatSpellAttackIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);

        return ExecutePlayerCommand(
            source,
            intent.ExpectedEncounterRevision,
            intent.ActorCombatantId,
            (encounter, randomSeed, cursorBefore) =>
                WatchtowerPlayerCommandResolver.Resolve(
                    encounter,
                    randomSeed,
                    cursorBefore,
                    intent));
    }

    internal static WatchtowerCombatResolutionResult Execute(
        ApplicationSessionState source,
        CombatEndTurnIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);

        return ExecutePlayerCommand(
            source,
            intent.ExpectedEncounterRevision,
            intent.ActorCombatantId,
            (encounter, randomSeed, cursorBefore) =>
                WatchtowerPlayerCommandResolver.Resolve(
                    encounter,
                    randomSeed,
                    cursorBefore,
                    intent));
    }

    /// Shared envelope for every player command: validate that the submitted
    /// command owns the current decision, resolve it, fold the result into the
    /// session, then run automatic processing up to the next decision point.
    private static WatchtowerCombatResolutionResult ExecutePlayerCommand(
        ApplicationSessionState source,
        long expectedEncounterRevision,
        string actorCombatantId,
        Func<EncounterState, int, int, WatchtowerPlayerCommandResolution> resolve)
    {
        ApplicationSessionState state = WatchtowerCombatSessionMapper.Canonicalize(source);
        WatchtowerCombatDecision startingDecision =
            RequirePlayerDecision(state, expectedEncounterRevision, actorCombatantId);

        EncounterState encounter = WatchtowerCombatSessionMapper.GetEncounter(state);
        int cursorBefore = state.RandomValuesConsumed;

        WatchtowerPlayerCommandResolution resolution = resolve(
            encounter,
            state.RandomSeed,
            cursorBefore);

        state = WatchtowerCombatSessionMapper.ReplaceEncounter(
            state,
            resolution.State,
            resolution.CursorAfter);

        List<WatchtowerCombatStepResult> automaticSteps = [];
        state = WatchtowerAutomaticTurnProcessor.ProcessUntilDecision(state, automaticSteps);

        return CreateResult(
            startingDecision,
            resolution.Receipt,
            encounter.Revision,
            cursorBefore,
            resolution.PrimaryStep,
            automaticSteps,
            state);
    }

    private static WatchtowerCombatDecision RequirePlayerDecision(
        ApplicationSessionState state,
        long expectedRevision,
        string actorId)
    {
        if (string.IsNullOrWhiteSpace(actorId))
        {
            throw new ArgumentException(
                "A combat identifier is required.",
                nameof(actorId));
        }

        WatchtowerCombatDecision decision =
            WatchtowerCombatDecisionFactory.Create(state);

        if (decision.State
            != CombatDecisionState.PlayerDecisionRequired)
        {
            throw new InvalidOperationException(
                "A conscious party participant must own the current watchtower combat decision.");
        }

        if (expectedRevision != decision.EncounterRevision)
        {
            throw new InvalidOperationException(
                $"Expected encounter revision '{expectedRevision}', but the current revision is '{decision.EncounterRevision}'.");
        }

        if (!string.Equals(
            actorId,
            decision.ActiveCombatantId,
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The submitted actor does not own the current watchtower combat decision.");
        }

        return decision;
    }

    private static WatchtowerCombatResolutionResult CreateResult(
        WatchtowerCombatDecision startingDecision,
        WatchtowerCombatIntentReceipt? submittedIntent,
        long priorRevision,
        int cursorBefore,
        WatchtowerCombatStepResult? primaryStep,
        IReadOnlyList<WatchtowerCombatStepResult> automaticSteps,
        ApplicationSessionState state)
    {
        WatchtowerCombatStepResult[] protectedAutomaticSteps =
            automaticSteps.ToArray();
        WatchtowerCombatDecision resultingDecision =
            WatchtowerCombatDecisionFactory.Create(state);

        if (resultingDecision.State
            == CombatDecisionState.AutomaticProcessingRequired)
        {
            throw new InvalidOperationException(
                "A successful watchtower combat operation cannot stop at an automatic-processing boundary.");
        }

        return new WatchtowerCombatResolutionResult
        {
            StartingDecision = startingDecision,
            SubmittedIntent = submittedIntent,
            PriorEncounterRevision = priorRevision,
            ResultingEncounterRevision = WatchtowerCombatSessionMapper.GetEncounter(state).Revision,
            RandomValuesConsumedBefore = cursorBefore,
            RandomValuesConsumedAfter = state.RandomValuesConsumed,
            PrimaryStep = primaryStep,
            AutomaticSteps = Array.AsReadOnly(protectedAutomaticSteps),
            ResultingDecision = resultingDecision,
            State = state
        };
    }

}

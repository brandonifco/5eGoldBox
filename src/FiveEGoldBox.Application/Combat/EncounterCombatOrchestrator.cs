using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal static class EncounterCombatOrchestrator
{
    internal static EncounterCombatResolutionResult AdvanceToDecision(
        ApplicationSessionState source)
    {
        ApplicationSessionState state = EncounterCombatSessionMapper.Canonicalize(source);
        EncounterCombatDecision startingDecision =
            EncounterCombatDecisionFactory.Create(state);
        long priorRevision = EncounterCombatSessionMapper.GetEncounter(state).Revision;
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

        List<EncounterCombatStepResult> automaticSteps = [];
        state = EncounterAutomaticTurnProcessor.ProcessUntilDecision(state, automaticSteps);

        return CreateResult(
            startingDecision,
            submittedIntent: null,
            priorRevision,
            cursorBefore,
            primaryStep: null,
            automaticSteps,
            state);
    }

    internal static EncounterCombatResolutionResult Execute(
        ApplicationSessionState source,
        CombatMoveIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);

        return ExecutePlayerCommand(
            source,
            intent.ExpectedEncounterRevision,
            intent.ActorCombatantId,
            (encounter, randomSeed, cursorBefore) =>
                EncounterPlayerCommandResolver.Resolve(
                    encounter,
                    randomSeed,
                    cursorBefore,
                    intent));
    }

    internal static EncounterCombatResolutionResult Execute(
        ApplicationSessionState source,
        CombatWeaponAttackIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);

        return ExecutePlayerCommand(
            source,
            intent.ExpectedEncounterRevision,
            intent.ActorCombatantId,
            (encounter, randomSeed, cursorBefore) =>
                EncounterPlayerCommandResolver.Resolve(
                    encounter,
                    randomSeed,
                    cursorBefore,
                    intent));
    }

    internal static EncounterCombatResolutionResult Execute(
        ApplicationSessionState source,
        CombatSpellAttackIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);

        return ExecutePlayerCommand(
            source,
            intent.ExpectedEncounterRevision,
            intent.ActorCombatantId,
            (encounter, randomSeed, cursorBefore) =>
                EncounterPlayerCommandResolver.Resolve(
                    encounter,
                    randomSeed,
                    cursorBefore,
                    intent));
    }

    internal static EncounterCombatResolutionResult Execute(
        ApplicationSessionState source,
        CombatDisengageIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);

        return ExecutePlayerCommand(
            source,
            intent.ExpectedEncounterRevision,
            intent.ActorCombatantId,
            (encounter, randomSeed, cursorBefore) =>
                EncounterPlayerCommandResolver.Resolve(
                    encounter,
                    randomSeed,
                    cursorBefore,
                    intent));
    }

    internal static EncounterCombatResolutionResult Execute(
        ApplicationSessionState source,
        CombatEndTurnIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);

        return ExecutePlayerCommand(
            source,
            intent.ExpectedEncounterRevision,
            intent.ActorCombatantId,
            (encounter, randomSeed, cursorBefore) =>
                EncounterPlayerCommandResolver.Resolve(
                    encounter,
                    randomSeed,
                    cursorBefore,
                    intent));
    }

    /// Shared envelope for every player command: validate that the submitted
    /// command owns the current decision, resolve it, fold the result into the
    /// session, then run automatic processing up to the next decision point.
    private static EncounterCombatResolutionResult ExecutePlayerCommand(
        ApplicationSessionState source,
        long expectedEncounterRevision,
        string actorCombatantId,
        Func<EncounterState, int, int, EncounterPlayerCommandResolution> resolve)
    {
        ApplicationSessionState state = EncounterCombatSessionMapper.Canonicalize(source);
        EncounterCombatDecision startingDecision =
            RequirePlayerDecision(state, expectedEncounterRevision, actorCombatantId);

        EncounterState encounter = EncounterCombatSessionMapper.GetEncounter(state);
        int cursorBefore = state.RandomValuesConsumed;

        EncounterPlayerCommandResolution resolution = resolve(
            encounter,
            state.RandomSeed,
            cursorBefore);

        state = EncounterCombatSessionMapper.ReplaceEncounter(
            state,
            resolution.State,
            resolution.CursorAfter);

        // Reactions first, then whatever automatic processing follows: an
        // opportunity attack happened during the player's own command, so
        // it belongs before an enemy's subsequent turn, not after it.
        List<EncounterCombatStepResult> automaticSteps =
            [.. resolution.ReactionSteps];
        state = EncounterAutomaticTurnProcessor.ProcessUntilDecision(state, automaticSteps);

        return CreateResult(
            startingDecision,
            resolution.Receipt,
            encounter.Revision,
            cursorBefore,
            resolution.PrimaryStep,
            automaticSteps,
            state);
    }

    private static EncounterCombatDecision RequirePlayerDecision(
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

        EncounterCombatDecision decision =
            EncounterCombatDecisionFactory.Create(state);

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

    private static EncounterCombatResolutionResult CreateResult(
        EncounterCombatDecision startingDecision,
        EncounterCombatIntentReceipt? submittedIntent,
        long priorRevision,
        int cursorBefore,
        EncounterCombatStepResult? primaryStep,
        IReadOnlyList<EncounterCombatStepResult> automaticSteps,
        ApplicationSessionState state)
    {
        EncounterCombatStepResult[] protectedAutomaticSteps =
            automaticSteps.ToArray();
        EncounterCombatDecision resultingDecision =
            EncounterCombatDecisionFactory.Create(state);

        if (resultingDecision.State
            == CombatDecisionState.AutomaticProcessingRequired)
        {
            throw new InvalidOperationException(
                "A successful watchtower combat operation cannot stop at an automatic-processing boundary.");
        }

        return new EncounterCombatResolutionResult
        {
            StartingDecision = startingDecision,
            SubmittedIntent = submittedIntent,
            PriorEncounterRevision = priorRevision,
            ResultingEncounterRevision = EncounterCombatSessionMapper.GetEncounter(state).Revision,
            RandomValuesConsumedBefore = cursorBefore,
            RandomValuesConsumedAfter = state.RandomValuesConsumed,
            PrimaryStep = primaryStep,
            AutomaticSteps = Array.AsReadOnly(protectedAutomaticSteps),
            ResultingDecision = resultingDecision,
            State = state
        };
    }

}

using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Randomness;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal static class WatchtowerCombatOrchestrator
{
    internal static WatchtowerCombatResolutionResult AdvanceToDecision(
        ApplicationSessionState source)
    {
        ApplicationSessionState state = Canonicalize(source);
        WatchtowerCombatDecision startingDecision =
            WatchtowerCombatDecisionFactory.Create(state);
        long priorRevision = GetEncounter(state).Revision;
        int cursorBefore = state.RandomValuesConsumed;

        if (startingDecision.State is
            WatchtowerCombatDecisionState.PlayerDecisionRequired
            or WatchtowerCombatDecisionState.CombatCompleted)
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
        state = Normalize(state, automaticSteps);

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
        WatchtowerCombatMoveIntent intent)
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
        WatchtowerCombatWeaponAttackIntent intent)
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
        WatchtowerCombatEndTurnIntent intent)
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
        ApplicationSessionState state = Canonicalize(source);
        WatchtowerCombatDecision startingDecision =
            RequirePlayerDecision(state, expectedEncounterRevision, actorCombatantId);

        EncounterState encounter = GetEncounter(state);
        int cursorBefore = state.RandomValuesConsumed;

        WatchtowerPlayerCommandResolution resolution = resolve(
            encounter,
            state.RandomSeed,
            cursorBefore);

        state = ReplaceEncounter(
            state,
            resolution.State,
            resolution.CursorAfter);

        List<WatchtowerCombatStepResult> automaticSteps = [];
        state = Normalize(state, automaticSteps);

        return CreateResult(
            startingDecision,
            resolution.Receipt,
            encounter.Revision,
            cursorBefore,
            resolution.PrimaryStep,
            automaticSteps,
            state);
    }

    private static ApplicationSessionState Normalize(
        ApplicationSessionState initialState,
        List<WatchtowerCombatStepResult> steps)
    {
        ApplicationSessionState state = initialState;
        HashSet<(long Revision, int Cursor, string Actor)> visited = [];

        while (true)
        {
            EncounterState encounter = GetEncounter(state);

            if (encounter.LifecycleState
                == EncounterLifecycleState.Completed)
            {
                WatchtowerCombatStepFactory.AppendCompletion(steps, encounter);
                return state;
            }

            string activeId = encounter.ActiveCombatantId;

            if (!visited.Add((
                encounter.Revision,
                state.RandomValuesConsumed,
                activeId)))
            {
                throw new InvalidOperationException(
                    "Automatic watchtower combat processing made no authoritative progress.");
            }

            EncounterParticipantState active =
                WatchtowerCombatDecisionFactory.FindParticipant(
                    encounter,
                    activeId);

            if (string.Equals(
                    active.SideId,
                    WatchtowerSignalEncounter.PartySideId,
                    StringComparison.Ordinal)
                && active.Combatant.LifecycleState
                    == CombatantLifecycleState.Conscious
                && encounter.PendingDeathSavingThrowCombatantId is null)
            {
                return state;
            }

            if (active.Combatant.LifecycleState
                == CombatantLifecycleState.Dying)
            {
                state = ResolveAutomaticDeathSave(state, steps);
                continue;
            }

            if (active.Combatant.LifecycleState
                == CombatantLifecycleState.Stable)
            {
                EncounterTurnAdvancementResult turn = AdvanceTurn(
                    encounter,
                    activeId);

                steps.Add(WatchtowerCombatStepFactory.CreateTurnAdvanced(
                    encounter,
                    turn,
                    WatchtowerCombatTurnAdvanceReason.StableParticipant));

                state = ReplaceEncounter(
                    state,
                    turn.State,
                    state.RandomValuesConsumed);
                continue;
            }

            if (string.Equals(
                active.SideId,
                WatchtowerSignalEncounter.RaiderSideId,
                StringComparison.Ordinal))
            {
                state = ResolveRaiderTurn(state, steps);
                continue;
            }

            EncounterTurnAdvancementResult skippedTurn = AdvanceTurn(
                encounter,
                activeId);

            steps.Add(WatchtowerCombatStepFactory.CreateTurnAdvanced(
                encounter,
                skippedTurn,
                WatchtowerCombatTurnAdvanceReason.NoProductiveEnemyAction));

            state = ReplaceEncounter(
                state,
                skippedTurn.State,
                state.RandomValuesConsumed);
        }
    }

    private static ApplicationSessionState ResolveAutomaticDeathSave(
        ApplicationSessionState state,
        List<WatchtowerCombatStepResult> steps)
    {
        EncounterState encounter = GetEncounter(state);
        string actorId = encounter.ActiveCombatantId;
        ApplicationRandomRoll randomRoll =
            ApplicationRandomSequence.GenerateDie(
                state.RandomSeed,
                state.RandomValuesConsumed,
                sides: 20);

        EncounterDeathSavingThrowResult deathSave =
            EncounterDeathSavingThrowRules.Resolve(
                encounter,
                new EncounterDeathSavingThrowCommand
                {
                    ExpectedRevision = encounter.Revision,
                    ActorCombatantId = actorId,
                    RollMode = D20RollMode.Normal,
                    FirstRoll = randomRoll.Value,
                    SecondRoll = null,
                    SavingThrowBonus = 0
                });

        IReadOnlyList<WatchtowerCombatDieRoll> dice =
            Array.AsReadOnly(
                new[]
                {
                    WatchtowerCombatStepFactory.CreateDie(
                        randomRoll,
                        WatchtowerCombatDiePurpose.DeathSavingThrow)
                });

        steps.Add(WatchtowerCombatStepFactory.CreateDeathSavingThrow(
            encounter,
            deathSave,
            dice));

        state = ReplaceEncounter(
            state,
            deathSave.State,
            randomRoll.UpdatedValuesConsumed);

        if (deathSave.State.LifecycleState
            == EncounterLifecycleState.Completed
            || deathSave.LifecycleState
                == CombatantLifecycleState.Conscious)
        {
            return state;
        }

        EncounterTurnAdvancementResult turn = AdvanceTurn(
            deathSave.State,
            actorId);

        steps.Add(WatchtowerCombatStepFactory.CreateTurnAdvanced(
            deathSave.State,
            turn,
            WatchtowerCombatTurnAdvanceReason.DyingParticipantAfterSave));

        return ReplaceEncounter(
            state,
            turn.State,
            state.RandomValuesConsumed);
    }

    private static ApplicationSessionState ResolveRaiderTurn(
        ApplicationSessionState state,
        List<WatchtowerCombatStepResult> steps)
    {
        EncounterState encounter = GetEncounter(state);
        string actorId = encounter.ActiveCombatantId;
        WatchtowerRaiderTurnPlan plan =
            WatchtowerRaiderTurnPlanner.Plan(
                encounter,
                state.Party);

        if (plan.Movement is not null)
        {
            steps.Add(WatchtowerCombatStepFactory.CreateMovement(
                encounter,
                plan.Movement));

            state = ReplaceEncounter(
                state,
                plan.Movement.State,
                state.RandomValuesConsumed);
            encounter = plan.Movement.State;
        }

        if (plan.Attack is { } attackPlan)
        {
            WatchtowerCombatAttackExecution attack =
                WatchtowerCombatAttackStaging.Resolve(
                    encounter,
                    state.RandomSeed,
                    state.RandomValuesConsumed,
                    actorId,
                    attackPlan.TargetCombatantId,
                    attackPlan.WeaponId);

            steps.Add(WatchtowerCombatStepFactory.CreateWeaponAttack(
                encounter,
                attack.Result,
                attack.Dice));

            state = ReplaceEncounter(
                state,
                attack.Result.State,
                attack.CursorAfter);

            if (attack.Result.State.LifecycleState
                == EncounterLifecycleState.Completed)
            {
                return state;
            }

            encounter = attack.Result.State;
        }

        EncounterTurnAdvancementResult turn = AdvanceTurn(
            encounter,
            actorId);

        steps.Add(WatchtowerCombatStepFactory.CreateTurnAdvanced(
            encounter,
            turn,
            plan.TurnAdvanceReason));

        return ReplaceEncounter(
            state,
            turn.State,
            state.RandomValuesConsumed);
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
            != WatchtowerCombatDecisionState.PlayerDecisionRequired)
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

    private static EncounterTurnAdvancementResult AdvanceTurn(
        EncounterState encounter,
        string actorId)
    {
        return EncounterTurnAdvancementRules.Resolve(
            encounter,
            new EncounterTurnAdvancementCommand
            {
                ExpectedRevision = encounter.Revision,
                ActorCombatantId = actorId
            });
    }

    private static ApplicationSessionState Canonicalize(
        ApplicationSessionState source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return ApplicationSessionRules.CreateCanonical(source);
    }

    private static ApplicationSessionState ReplaceEncounter(
        ApplicationSessionState state,
        EncounterState encounter,
        int randomValuesConsumed)
    {
        ApplicationSessionState replacement = state with
        {
            RandomValuesConsumed = randomValuesConsumed,
            ActiveEncounter = state.ActiveEncounter! with
            {
                Encounter = encounter
            }
        };

        return ApplicationSessionRules.CreateCanonical(replacement);
    }

    private static EncounterState GetEncounter(
        ApplicationSessionState state)
    {
        return state.ActiveEncounter?.Encounter
            ?? throw new InvalidOperationException(
                "The watchtower combat session has no active encounter context.");
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
            == WatchtowerCombatDecisionState.AutomaticProcessingRequired)
        {
            throw new InvalidOperationException(
                "A successful watchtower combat operation cannot stop at an automatic-processing boundary.");
        }

        return new WatchtowerCombatResolutionResult
        {
            StartingDecision = startingDecision,
            SubmittedIntent = submittedIntent,
            PriorEncounterRevision = priorRevision,
            ResultingEncounterRevision = GetEncounter(state).Revision,
            RandomValuesConsumedBefore = cursorBefore,
            RandomValuesConsumedAfter = state.RandomValuesConsumed,
            PrimaryStep = primaryStep,
            AutomaticSteps = Array.AsReadOnly(protectedAutomaticSteps),
            ResultingDecision = resultingDecision,
            State = state
        };
    }

}

using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Randomness;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
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

        ApplicationSessionState state = Canonicalize(source);
        WatchtowerCombatDecision startingDecision =
            RequirePlayerDecision(state, intent.ExpectedEncounterRevision, intent.ActorCombatantId);

        ArgumentNullException.ThrowIfNull(intent.Path);
        GridPosition[] path = intent.Path.ToArray();

        if (path.Length == 0)
        {
            throw new ArgumentException(
                "A watchtower combat movement path must contain at least one position.",
                nameof(intent));
        }

        EncounterState encounter = GetEncounter(state);
        int cursorBefore = state.RandomValuesConsumed;

        EncounterMovementResult movement =
            EncounterMovementRules.Resolve(
                encounter,
                new EncounterMovementCommand
                {
                    ExpectedRevision = intent.ExpectedEncounterRevision,
                    ActorCombatantId = intent.ActorCombatantId,
                    Path = Array.AsReadOnly(path)
                });

        state = ReplaceEncounter(
            state,
            movement.State,
            cursorBefore);

        WatchtowerCombatStepResult primaryStep =
            CreateMovementStep(encounter, movement);

        List<WatchtowerCombatStepResult> automaticSteps = [];
        state = Normalize(state, automaticSteps);

        return CreateResult(
            startingDecision,
            CreateReceipt(intent, path),
            encounter.Revision,
            cursorBefore,
            primaryStep,
            automaticSteps,
            state);
    }

    internal static WatchtowerCombatResolutionResult Execute(
        ApplicationSessionState source,
        WatchtowerCombatWeaponAttackIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);

        ApplicationSessionState state = Canonicalize(source);
        WatchtowerCombatDecision startingDecision =
            RequirePlayerDecision(state, intent.ExpectedEncounterRevision, intent.ActorCombatantId);
        ValidateRequiredId(intent.WeaponId, nameof(intent.WeaponId));
        ValidateRequiredId(intent.TargetCombatantId, nameof(intent.TargetCombatantId));

        EncounterState encounter = GetEncounter(state);
        EncounterWeaponAttackPrerequisiteEvaluation prerequisites =
            EncounterWeaponAttackPrerequisiteRules.Evaluate(
                encounter,
                intent.ActorCombatantId,
                intent.TargetCombatantId,
                intent.WeaponId);

        EnsureLegalPlayerAttack(prerequisites);

        int cursorBefore = state.RandomValuesConsumed;
        AttackExecution attack = ResolveAttack(
            encounter,
            state.RandomSeed,
            cursorBefore,
            intent.ActorCombatantId,
            intent.TargetCombatantId,
            intent.WeaponId,
            prerequisites);

        state = ReplaceEncounter(
            state,
            attack.Result.State,
            attack.CursorAfter);

        WatchtowerCombatStepResult primaryStep =
            CreateWeaponAttackStep(
                encounter,
                attack.Result,
                attack.Dice);

        List<WatchtowerCombatStepResult> automaticSteps = [];
        state = Normalize(state, automaticSteps);

        return CreateResult(
            startingDecision,
            CreateReceipt(intent),
            encounter.Revision,
            cursorBefore,
            primaryStep,
            automaticSteps,
            state);
    }

    internal static WatchtowerCombatResolutionResult Execute(
        ApplicationSessionState source,
        WatchtowerCombatEndTurnIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);

        ApplicationSessionState state = Canonicalize(source);
        WatchtowerCombatDecision startingDecision =
            RequirePlayerDecision(state, intent.ExpectedEncounterRevision, intent.ActorCombatantId);
        EncounterState encounter = GetEncounter(state);
        int cursorBefore = state.RandomValuesConsumed;

        EncounterTurnAdvancementResult turn = AdvanceTurn(
            encounter,
            intent.ActorCombatantId);

        state = ReplaceEncounter(state, turn.State, cursorBefore);

        WatchtowerCombatStepResult primaryStep =
            CreateTurnStep(
                encounter,
                turn,
                WatchtowerCombatTurnAdvanceReason.PlayerEndTurn);

        List<WatchtowerCombatStepResult> automaticSteps = [];
        state = Normalize(state, automaticSteps);

        return CreateResult(
            startingDecision,
            CreateReceipt(intent),
            encounter.Revision,
            cursorBefore,
            primaryStep,
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
                AppendCompletionStep(steps, encounter);
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

                steps.Add(CreateTurnStep(
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

            steps.Add(CreateTurnStep(
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
                    CreateDie(
                        randomRoll,
                        WatchtowerCombatDiePurpose.DeathSavingThrow)
                });

        steps.Add(CreateDeathSaveStep(
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

        steps.Add(CreateTurnStep(
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
        EncounterParticipantState raider =
            WatchtowerCombatDecisionFactory.FindParticipant(
                encounter,
                encounter.ActiveCombatantId);
        WeaponAttack weapon =
            WatchtowerCombatDecisionFactory.GetFixedWeapon(raider);

        if (weapon.AmmunitionItemId is not null
            && weapon.AmmunitionQuantityAvailable <= 0)
        {
            return EndNonproductiveRaiderTurn(state, steps);
        }

        EncounterParticipantState? target =
            WatchtowerRaiderPolicy.SelectTarget(
                encounter,
                state.Party,
                raider);

        if (target is null)
        {
            if (string.Equals(
                raider.Combatant.CombatantId,
                WatchtowerSignalEncounter.MeleeRaiderId,
                StringComparison.Ordinal))
            {
                EncounterParticipantState? progressTarget =
                    WatchtowerRaiderPolicy.SelectProgressTarget(
                        encounter,
                        state.Party,
                        raider);

                if (progressTarget is not null)
                {
                    EncounterMovementResult? movement =
                        WatchtowerCombatPathSearch.FindMovement(
                            encounter,
                            raider.Combatant.CombatantId,
                            progressTarget.Combatant.CombatantId,
                            weapon.WeaponId);

                    if (movement is not null)
                    {
                        steps.Add(CreateMovementStep(
                            encounter,
                            movement));

                        state = ReplaceEncounter(
                            state,
                            movement.State,
                            state.RandomValuesConsumed);
                    }
                }
            }

            return EndNonproductiveRaiderTurn(state, steps);
        }

        string actorId = raider.Combatant.CombatantId;
        string targetId = target.Combatant.CombatantId;

        EncounterWeaponAttackPrerequisiteEvaluation prerequisites =
            EncounterWeaponAttackPrerequisiteRules.Evaluate(
                encounter,
                actorId,
                targetId,
                weapon.WeaponId);

        if (!prerequisites.IsLegal
            && string.Equals(
                actorId,
                WatchtowerSignalEncounter.MeleeRaiderId,
                StringComparison.Ordinal))
        {
            EncounterMovementResult? movement =
                WatchtowerCombatPathSearch.FindMovement(
                    encounter,
                    actorId,
                    targetId,
                    weapon.WeaponId);

            if (movement is not null)
            {
                steps.Add(CreateMovementStep(
                    encounter,
                    movement));

                state = ReplaceEncounter(
                    state,
                    movement.State,
                    state.RandomValuesConsumed);
                encounter = movement.State;

                prerequisites =
                    EncounterWeaponAttackPrerequisiteRules.Evaluate(
                        encounter,
                        actorId,
                        targetId,
                        weapon.WeaponId);
            }
        }

        if (prerequisites.IsLegal)
        {
            AttackExecution attack = ResolveAttack(
                encounter,
                state.RandomSeed,
                state.RandomValuesConsumed,
                actorId,
                targetId,
                weapon.WeaponId,
                prerequisites);

            steps.Add(CreateWeaponAttackStep(
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

        steps.Add(CreateTurnStep(
            encounter,
            turn,
            prerequisites.IsLegal
                ? WatchtowerCombatTurnAdvanceReason.RaiderTurnCompleted
                : WatchtowerCombatTurnAdvanceReason.NoProductiveEnemyAction));

        return ReplaceEncounter(
            state,
            turn.State,
            state.RandomValuesConsumed);
    }

    private static ApplicationSessionState EndNonproductiveRaiderTurn(
        ApplicationSessionState state,
        List<WatchtowerCombatStepResult> steps)
    {
        EncounterState encounter = GetEncounter(state);
        EncounterTurnAdvancementResult turn = AdvanceTurn(
            encounter,
            encounter.ActiveCombatantId);

        steps.Add(CreateTurnStep(
            encounter,
            turn,
            WatchtowerCombatTurnAdvanceReason.NoProductiveEnemyAction));

        return ReplaceEncounter(
            state,
            turn.State,
            state.RandomValuesConsumed);
    }

    private static AttackExecution ResolveAttack(
        EncounterState encounter,
        int seed,
        int cursor,
        string actorId,
        string targetId,
        string weaponId,
        EncounterWeaponAttackPrerequisiteEvaluation prerequisites)
    {
        if (!prerequisites.IsLegal
            || prerequisites.AttackRollMode is null)
        {
            throw new InvalidOperationException(
                $"The weapon attack is unavailable for reason '{prerequisites.UnavailabilityReason}'.");
        }

        List<WatchtowerCombatDieRoll> dice = [];
        int nextCursor = cursor;

        ApplicationRandomRoll first =
            ApplicationRandomSequence.GenerateDie(seed, nextCursor, 20);
        nextCursor = first.UpdatedValuesConsumed;
        dice.Add(CreateDie(first, WatchtowerCombatDiePurpose.AttackRoll));

        int? secondValue = null;

        if (prerequisites.AttackRollMode
            != D20RollMode.Normal)
        {
            ApplicationRandomRoll second =
                ApplicationRandomSequence.GenerateDie(seed, nextCursor, 20);
            nextCursor = second.UpdatedValuesConsumed;
            secondValue = second.Value;
            dice.Add(CreateDie(second, WatchtowerCombatDiePurpose.AttackRoll));
        }

        EncounterWeaponAttackEvaluation evaluation =
            EncounterWeaponAttackRules.Evaluate(
                encounter,
                new EncounterWeaponAttackEvaluationCommand
                {
                    ExpectedRevision = encounter.Revision,
                    ActorCombatantId = actorId,
                    TargetCombatantId = targetId,
                    WeaponId = weaponId,
                    FirstAttackRoll = first.Value,
                    SecondAttackRoll = secondValue
                });

        List<int> damageValues = [];
        DamageDice? requiredDamage = evaluation.RequiredDamageDice;

        if (requiredDamage is not null)
        {
            int sides = (int)requiredDamage.Die;

            for (int index = 0;
                index < requiredDamage.Count;
                index++)
            {
                ApplicationRandomRoll damage =
                    ApplicationRandomSequence.GenerateDie(
                        seed,
                        nextCursor,
                        sides);

                nextCursor = damage.UpdatedValuesConsumed;
                damageValues.Add(damage.Value);
                dice.Add(CreateDie(
                    damage,
                    WatchtowerCombatDiePurpose.DamageRoll));
            }
        }

        EncounterWeaponAttackResult result =
            EncounterWeaponAttackRules.Resolve(
                encounter,
                new EncounterWeaponAttackCommand
                {
                    ExpectedRevision = encounter.Revision,
                    ActorCombatantId = actorId,
                    TargetCombatantId = targetId,
                    WeaponId = weaponId,
                    FirstAttackRoll = first.Value,
                    SecondAttackRoll = secondValue,
                    DamageRolls = Array.AsReadOnly(damageValues.ToArray())
                });

        return new AttackExecution(
            result,
            nextCursor,
            Array.AsReadOnly(dice.ToArray()));
    }

    private static WatchtowerCombatDecision RequirePlayerDecision(
        ApplicationSessionState state,
        long expectedRevision,
        string actorId)
    {
        ValidateRequiredId(actorId, nameof(actorId));
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

    private static void EnsureLegalPlayerAttack(
        EncounterWeaponAttackPrerequisiteEvaluation prerequisites)
    {
        if (!prerequisites.IsLegal)
        {
            throw new InvalidOperationException(
                $"The selected weapon attack is unavailable for reason '{prerequisites.UnavailabilityReason}'.");
        }
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

    private static WatchtowerCombatIntentReceipt CreateReceipt(
        WatchtowerCombatMoveIntent intent,
        GridPosition[] path)
    {
        return new WatchtowerCombatIntentReceipt
        {
            Kind = WatchtowerCombatIntentKind.Move,
            ExpectedEncounterRevision = intent.ExpectedEncounterRevision,
            ActorCombatantId = intent.ActorCombatantId,
            Path = Array.AsReadOnly(path),
            WeaponId = null,
            TargetCombatantId = null
        };
    }

    private static WatchtowerCombatIntentReceipt CreateReceipt(
        WatchtowerCombatWeaponAttackIntent intent)
    {
        return new WatchtowerCombatIntentReceipt
        {
            Kind = WatchtowerCombatIntentKind.WeaponAttack,
            ExpectedEncounterRevision = intent.ExpectedEncounterRevision,
            ActorCombatantId = intent.ActorCombatantId,
            Path = Array.Empty<GridPosition>(),
            WeaponId = intent.WeaponId,
            TargetCombatantId = intent.TargetCombatantId
        };
    }

    private static WatchtowerCombatIntentReceipt CreateReceipt(
        WatchtowerCombatEndTurnIntent intent)
    {
        return new WatchtowerCombatIntentReceipt
        {
            Kind = WatchtowerCombatIntentKind.EndTurn,
            ExpectedEncounterRevision = intent.ExpectedEncounterRevision,
            ActorCombatantId = intent.ActorCombatantId,
            Path = Array.Empty<GridPosition>(),
            WeaponId = null,
            TargetCombatantId = null
        };
    }

    private static WatchtowerCombatStepResult CreateMovementStep(
        EncounterState startingState,
        EncounterMovementResult movement)
    {
        return new WatchtowerCombatStepResult
        {
            Kind = WatchtowerCombatStepKind.Movement,
            StartingEncounterRevision = startingState.Revision,
            ResultingEncounterRevision = movement.State.Revision,
            ActorCombatantId = movement.ActorCombatantId,
            TargetCombatantId = null,
            Dice = Array.Empty<WatchtowerCombatDieRoll>(),
            Movement = movement,
            WeaponAttack = null,
            DeathSavingThrow = null,
            TurnAdvancement = null,
            TurnAdvanceReason = null,
            WinningSideId = movement.State.WinningSideId
        };
    }

    private static WatchtowerCombatStepResult CreateWeaponAttackStep(
        EncounterState startingState,
        EncounterWeaponAttackResult attack,
        IReadOnlyList<WatchtowerCombatDieRoll> dice)
    {
        return new WatchtowerCombatStepResult
        {
            Kind = WatchtowerCombatStepKind.WeaponAttack,
            StartingEncounterRevision = startingState.Revision,
            ResultingEncounterRevision = attack.State.Revision,
            ActorCombatantId = attack.ActorCombatantId,
            TargetCombatantId = attack.TargetCombatantId,
            Dice = Array.AsReadOnly(dice.ToArray()),
            Movement = null,
            WeaponAttack = attack,
            DeathSavingThrow = null,
            TurnAdvancement = null,
            TurnAdvanceReason = null,
            WinningSideId = attack.State.WinningSideId
        };
    }

    private static WatchtowerCombatStepResult CreateDeathSaveStep(
        EncounterState startingState,
        EncounterDeathSavingThrowResult deathSave,
        IReadOnlyList<WatchtowerCombatDieRoll> dice)
    {
        return new WatchtowerCombatStepResult
        {
            Kind = WatchtowerCombatStepKind.DeathSavingThrow,
            StartingEncounterRevision = startingState.Revision,
            ResultingEncounterRevision = deathSave.State.Revision,
            ActorCombatantId = deathSave.ActorCombatantId,
            TargetCombatantId = null,
            Dice = Array.AsReadOnly(dice.ToArray()),
            Movement = null,
            WeaponAttack = null,
            DeathSavingThrow = deathSave,
            TurnAdvancement = null,
            TurnAdvanceReason = null,
            WinningSideId = deathSave.State.WinningSideId
        };
    }

    private static WatchtowerCombatStepResult CreateTurnStep(
        EncounterState startingState,
        EncounterTurnAdvancementResult turn,
        WatchtowerCombatTurnAdvanceReason reason)
    {
        return new WatchtowerCombatStepResult
        {
            Kind = WatchtowerCombatStepKind.TurnAdvanced,
            StartingEncounterRevision = startingState.Revision,
            ResultingEncounterRevision = turn.State.Revision,
            ActorCombatantId = turn.EndedTurnCombatantId,
            TargetCombatantId = null,
            Dice = Array.Empty<WatchtowerCombatDieRoll>(),
            Movement = null,
            WeaponAttack = null,
            DeathSavingThrow = null,
            TurnAdvancement = turn,
            TurnAdvanceReason = reason,
            WinningSideId = turn.State.WinningSideId
        };
    }

    private static void AppendCompletionStep(
        List<WatchtowerCombatStepResult> steps,
        EncounterState encounter)
    {
        if (steps.Count > 0
            && steps[^1].Kind
                == WatchtowerCombatStepKind.CombatCompleted)
        {
            return;
        }

        steps.Add(new WatchtowerCombatStepResult
        {
            Kind = WatchtowerCombatStepKind.CombatCompleted,
            StartingEncounterRevision = encounter.Revision,
            ResultingEncounterRevision = encounter.Revision,
            ActorCombatantId = null,
            TargetCombatantId = null,
            Dice = Array.Empty<WatchtowerCombatDieRoll>(),
            Movement = null,
            WeaponAttack = null,
            DeathSavingThrow = null,
            TurnAdvancement = null,
            TurnAdvanceReason = null,
            WinningSideId = encounter.WinningSideId
        });
    }

    private static WatchtowerCombatDieRoll CreateDie(
        ApplicationRandomRoll roll,
        WatchtowerCombatDiePurpose purpose)
    {
        return new WatchtowerCombatDieRoll
        {
            Ordinal = roll.Ordinal,
            Sides = roll.Sides,
            Value = roll.Value,
            Purpose = purpose
        };
    }

    private static void ValidateRequiredId(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A combat identifier is required.",
                parameterName);
        }
    }

    private sealed record AttackExecution(
        EncounterWeaponAttackResult Result,
        int CursorAfter,
        IReadOnlyList<WatchtowerCombatDieRoll> Dice);
}

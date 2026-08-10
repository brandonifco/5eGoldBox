using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Randomness;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// Runs everything that happens between player decisions: death saving throws
/// for dying party members, skipped turns for stable ones, and raider turns.
/// It stops as soon as a conscious party member owns the turn, or the encounter
/// completes.
///
/// Raider tactics are decided by EncounterTacticsTurnPlanner; this type only
/// applies plans, threads the random cursor, and records steps.
internal static class EncounterAutomaticTurnProcessor
{
    internal static ApplicationSessionState ProcessUntilDecision(
        ApplicationSessionState initialState,
        List<EncounterCombatStepResult> steps)
    {
        ApplicationSessionState state = initialState;
        HashSet<(long Revision, int Cursor, string Actor)> visited = [];
        string partySideId = EncounterPartySideResolver.Resolve(
            state,
            EncounterCombatSessionMapper.GetEncounter(state));

        while (true)
        {
            EncounterState encounter =
                EncounterCombatSessionMapper.GetEncounter(state);

            if (encounter.LifecycleState
                == EncounterLifecycleState.Completed)
            {
                EncounterCombatStepFactory.AppendCompletion(steps, encounter);
                return state;
            }

            string activeId = encounter.ActiveCombatantId;

            // Every iteration must consume a die or advance the encounter.
            // Revisiting a state means it did neither, so stop rather than spin.
            if (!visited.Add((
                encounter.Revision,
                state.RandomValuesConsumed,
                activeId)))
            {
                throw new InvalidOperationException(
                    "Automatic watchtower combat processing made no authoritative progress.");
            }

            EncounterParticipantState active =
                EncounterCombatDecisionFactory.FindParticipant(
                    encounter,
                    activeId);

            if (string.Equals(
                    active.SideId,
                    partySideId,
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
                state = ResolveDeathSave(state, steps);
                continue;
            }

            if (active.Combatant.LifecycleState
                == CombatantLifecycleState.Stable)
            {
                state = SkipTurn(
                    state,
                    steps,
                    encounter,
                    activeId,
                    EncounterCombatTurnAdvanceReason.StableParticipant);
                continue;
            }

            // Anything conscious and not on the party's side is opposition,
            // whatever the scenario calls it — a raider here, a chapel
            // guardian elsewhere. Two-sided combat is all this engine
            // resolves, so "not the party" already means "the opposition".
            if (!string.Equals(
                active.SideId,
                partySideId,
                StringComparison.Ordinal))
            {
                state = ResolveRaiderTurn(state, steps);
                continue;
            }

            // Unreachable given the branches above: a conscious party
            // combatant returned early, and everything else is either dying,
            // stable, or opposition. Left as a defensive fallback rather than
            // removed without mutation-testing confirmation.
            state = SkipTurn(
                state,
                steps,
                encounter,
                activeId,
                EncounterCombatTurnAdvanceReason.NoProductiveEnemyAction);
        }
    }

    private static ApplicationSessionState ResolveDeathSave(
        ApplicationSessionState state,
        List<EncounterCombatStepResult> steps)
    {
        EncounterState encounter =
            EncounterCombatSessionMapper.GetEncounter(state);
        string actorId = encounter.ActiveCombatantId;
        ApplicationRandomRoll randomRoll =
            ApplicationRandomSequence.GenerateDie(
                state.RandomSeed,
                state.RandomValuesConsumed,
                DieType.D20);

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

        IReadOnlyList<EncounterCombatDieRoll> dice =
            Array.AsReadOnly(
                new[]
                {
                    EncounterCombatStepFactory.CreateDie(
                        randomRoll,
                        CombatDiePurpose.DeathSavingThrow)
                });

        steps.Add(EncounterCombatStepFactory.CreateDeathSavingThrow(
            encounter,
            deathSave,
            dice));

        state = EncounterCombatSessionMapper.ReplaceEncounter(
            state,
            deathSave.State,
            randomRoll.UpdatedValuesConsumed);

        // A save that ended the encounter or revived the combatant leaves the
        // turn where it is; only a still-dying combatant yields its turn.
        if (deathSave.State.LifecycleState
            == EncounterLifecycleState.Completed
            || deathSave.LifecycleState
                == CombatantLifecycleState.Conscious)
        {
            return state;
        }

        return SkipTurn(
            state,
            steps,
            deathSave.State,
            actorId,
            EncounterCombatTurnAdvanceReason.DyingParticipantAfterSave);
    }

    private static ApplicationSessionState ResolveRaiderTurn(
        ApplicationSessionState state,
        List<EncounterCombatStepResult> steps)
    {
        EncounterState encounter =
            EncounterCombatSessionMapper.GetEncounter(state);
        string actorId = encounter.ActiveCombatantId;
        EncounterTacticsTurnPlan plan =
            EncounterTacticsTurnPlanner.Plan(
                encounter,
                state.Party);

        if (plan.Movement is not null)
        {
            steps.Add(EncounterCombatStepFactory.CreateMovement(
                encounter,
                plan.Movement));

            state = EncounterCombatSessionMapper.ReplaceEncounter(
                state,
                plan.Movement.State,
                state.RandomValuesConsumed);
            encounter = plan.Movement.State;
        }

        if (plan.Attack is { } attackPlan)
        {
            EncounterCombatAttackExecution attack =
                EncounterCombatAttackStaging.Resolve(
                    encounter,
                    state.RandomSeed,
                    state.RandomValuesConsumed,
                    actorId,
                    attackPlan.TargetCombatantId,
                    attackPlan.WeaponId);

            steps.Add(EncounterCombatStepFactory.CreateWeaponAttack(
                encounter,
                attack.Result,
                attack.Dice));

            state = EncounterCombatSessionMapper.ReplaceEncounter(
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

        return SkipTurn(
            state,
            steps,
            encounter,
            actorId,
            plan.TurnAdvanceReason);
    }

    /// Advances past the active combatant, recording why.
    private static ApplicationSessionState SkipTurn(
        ApplicationSessionState state,
        List<EncounterCombatStepResult> steps,
        EncounterState encounter,
        string actorId,
        EncounterCombatTurnAdvanceReason reason)
    {
        EncounterTurnAdvancementResult turn =
            EncounterTurnAdvancementRules.Resolve(
                encounter,
                new EncounterTurnAdvancementCommand
                {
                    ExpectedRevision = encounter.Revision,
                    ActorCombatantId = actorId
                });

        steps.Add(EncounterCombatStepFactory.CreateTurnAdvanced(
            encounter,
            turn,
            reason));

        return EncounterCombatSessionMapper.ReplaceEncounter(
            state,
            turn.State,
            state.RandomValuesConsumed);
    }
}

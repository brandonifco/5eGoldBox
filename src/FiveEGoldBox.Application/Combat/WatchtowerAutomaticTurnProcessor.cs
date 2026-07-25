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
/// Raider tactics are decided by WatchtowerRaiderTurnPlanner; this type only
/// applies plans, threads the random cursor, and records steps.
internal static class WatchtowerAutomaticTurnProcessor
{
    internal static ApplicationSessionState ProcessUntilDecision(
        ApplicationSessionState initialState,
        List<WatchtowerCombatStepResult> steps)
    {
        ApplicationSessionState state = initialState;
        HashSet<(long Revision, int Cursor, string Actor)> visited = [];

        while (true)
        {
            EncounterState encounter =
                WatchtowerCombatSessionMapper.GetEncounter(state);

            if (encounter.LifecycleState
                == EncounterLifecycleState.Completed)
            {
                WatchtowerCombatStepFactory.AppendCompletion(steps, encounter);
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
                    WatchtowerCombatTurnAdvanceReason.StableParticipant);
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

            state = SkipTurn(
                state,
                steps,
                encounter,
                activeId,
                WatchtowerCombatTurnAdvanceReason.NoProductiveEnemyAction);
        }
    }

    private static ApplicationSessionState ResolveDeathSave(
        ApplicationSessionState state,
        List<WatchtowerCombatStepResult> steps)
    {
        EncounterState encounter =
            WatchtowerCombatSessionMapper.GetEncounter(state);
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

        state = WatchtowerCombatSessionMapper.ReplaceEncounter(
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
            WatchtowerCombatTurnAdvanceReason.DyingParticipantAfterSave);
    }

    private static ApplicationSessionState ResolveRaiderTurn(
        ApplicationSessionState state,
        List<WatchtowerCombatStepResult> steps)
    {
        EncounterState encounter =
            WatchtowerCombatSessionMapper.GetEncounter(state);
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

            state = WatchtowerCombatSessionMapper.ReplaceEncounter(
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

            state = WatchtowerCombatSessionMapper.ReplaceEncounter(
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
        List<WatchtowerCombatStepResult> steps,
        EncounterState encounter,
        string actorId,
        WatchtowerCombatTurnAdvanceReason reason)
    {
        EncounterTurnAdvancementResult turn =
            EncounterTurnAdvancementRules.Resolve(
                encounter,
                new EncounterTurnAdvancementCommand
                {
                    ExpectedRevision = encounter.Revision,
                    ActorCombatantId = actorId
                });

        steps.Add(WatchtowerCombatStepFactory.CreateTurnAdvanced(
            encounter,
            turn,
            reason));

        return WatchtowerCombatSessionMapper.ReplaceEncounter(
            state,
            turn.State,
            state.RandomValuesConsumed);
    }
}

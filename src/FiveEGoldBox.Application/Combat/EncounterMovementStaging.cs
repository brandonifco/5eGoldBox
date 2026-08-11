using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// Applies a movement path, letting opportunity attacks interrupt it.
///
/// Movement in Core is atomic on purpose — one call validates and applies a
/// whole path in a single revision, which is the right shape for a pure rule
/// and the reason the frozen transcripts are as stable as they are. But an
/// opportunity attack has to resolve *between* two squares, while the mover
/// is still in the square it is leaving (otherwise the attack's own reach
/// check would reject it, since the mover is by then out of reach), and a hit
/// that drops the mover has to stop the rest of the path. Stepwise-ness
/// therefore lives here, in the layer that already owns dice and sequencing,
/// rather than pushing randomness down into Core.
///
/// **A path that provokes nothing is still resolved in exactly one call**,
/// producing exactly one revision — the same state, the same step, the same
/// transcript as before this existed. That is deliberate rather than an
/// optimisation: the overwhelming majority of movement in a fight is closing
/// distance, and a feature that silently multiplied every move's revision
/// count would rewrite every frozen fixture for nothing.
internal static class EncounterMovementStaging
{
    internal static EncounterMovementStagingResult Resolve(
        EncounterState encounter,
        int seed,
        int cursor,
        string actorCombatantId,
        IReadOnlyList<GridPosition> path)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(path);

        GridPosition[] steps = path.ToArray();

        if (steps.Length == 0)
        {
            throw new ArgumentException(
                "A combat movement path must contain at least one position.",
                nameof(path));
        }

        GridPosition startingPosition =
            FindPosition(encounter, actorCombatantId);

        if (!PathProvokes(encounter, actorCombatantId, startingPosition, steps))
        {
            EncounterMovementResult movement =
                EncounterMovementRules.Resolve(
                    encounter,
                    new EncounterMovementCommand
                    {
                        ExpectedRevision = encounter.Revision,
                        ActorCombatantId = actorCombatantId,
                        Path = Array.AsReadOnly(steps)
                    });

            return new EncounterMovementStagingResult(
                movement.State,
                cursor,
                EncounterCombatStepFactory.CreateMovement(
                    encounter,
                    movement),
                Array.Empty<EncounterCombatStepResult>());
        }

        return ResolveStepwise(
            encounter,
            seed,
            cursor,
            actorCombatantId,
            startingPosition,
            steps);
    }

    /// Whether anything at all reacts to any step of this path, asked before
    /// a single square is applied. Cheap (no dice, no state change) and the
    /// whole basis of keeping the common case atomic.
    ///
    /// Evaluated against the *starting* state rather than square by square,
    /// which is a real approximation and worth naming: an attacker whose
    /// reaction is spent by an earlier provocation on the same path would be
    /// counted here even though it cannot actually swing again. That only
    /// ever produces a false positive — the path resolves stepwise, and the
    /// spent reaction is then correctly declined by the per-step check — so
    /// the cost is one extra revision on a rare path, never a wrong attack.
    private static bool PathProvokes(
        EncounterState encounter,
        string actorCombatantId,
        GridPosition startingPosition,
        IReadOnlyList<GridPosition> steps)
    {
        GridPosition previous = startingPosition;

        foreach (GridPosition next in steps)
        {
            if (EncounterOpportunityAttackRules.FindProvocations(
                encounter,
                actorCombatantId,
                previous,
                next).Count > 0)
            {
                return true;
            }

            previous = next;
        }

        return false;
    }

    private static EncounterMovementStagingResult ResolveStepwise(
        EncounterState encounter,
        int seed,
        int cursor,
        string actorCombatantId,
        GridPosition startingPosition,
        IReadOnlyList<GridPosition> steps)
    {
        EncounterState state = encounter;
        int nextCursor = cursor;
        List<EncounterCombatStepResult> reactionSteps = [];
        List<GridPosition> travelled = [];
        GridPosition previous = startingPosition;
        int movementSpentFeet = 0;

        foreach (GridPosition next in steps)
        {
            // Before the square is left, not after: the attack resolves
            // against the mover where it still stands, which is both what
            // 5e describes and the only position the attacker's own reach
            // check would accept.
            foreach (EncounterOpportunityAttack provocation
                in EncounterOpportunityAttackRules.FindProvocations(
                    state,
                    actorCombatantId,
                    previous,
                    next))
            {
                EncounterCombatAttackExecution attack =
                    EncounterCombatAttackStaging.Resolve(
                        state,
                        seed,
                        nextCursor,
                        provocation.AttackerCombatantId,
                        actorCombatantId,
                        provocation.WeaponId,
                        EncounterWeaponAttackTiming.Reaction);

                reactionSteps.Add(
                    EncounterCombatStepFactory.CreateWeaponAttack(
                        state,
                        attack.Result,
                        attack.Dice,
                        isOpportunityAttack: true));

                state = attack.Result.State;
                nextCursor = attack.CursorAfter;
            }

            // A free hit that drops the mover ends the move where it stands.
            // Checked against the encounter too, because the same hit can
            // finish the fight outright — resolving another square after
            // that would be movement inside a completed encounter.
            if (state.LifecycleState != EncounterLifecycleState.Active
                || !IsConscious(state, actorCombatantId))
            {
                break;
            }

            EncounterMovementResult step =
                EncounterMovementRules.Resolve(
                    state,
                    new EncounterMovementCommand
                    {
                        ExpectedRevision = state.Revision,
                        ActorCombatantId = actorCombatantId,
                        Path = Array.AsReadOnly(new[] { next })
                    });

            state = step.State;
            movementSpentFeet =
                checked(movementSpentFeet + step.MovementSpentFeet);
            travelled.Add(next);
            previous = next;
        }

        // Dropped before taking a single square: there is no movement to
        // describe, only the attacks that stopped it. CreateResult's own
        // primary step is nullable for exactly this kind of case.
        EncounterCombatStepResult? movementStep = travelled.Count == 0
            ? null
            : EncounterCombatStepFactory.CreateMovement(
                encounter,
                new EncounterMovementResult
                {
                    ActorCombatantId = actorCombatantId,
                    StartingPosition = startingPosition,
                    EndingPosition = travelled[^1],
                    Path = Array.AsReadOnly(travelled.ToArray()),
                    MovementSpentFeet = movementSpentFeet,
                    State = state
                });

        return new EncounterMovementStagingResult(
            state,
            nextCursor,
            movementStep,
            Array.AsReadOnly(reactionSteps.ToArray()));
    }

    private static bool IsConscious(
        EncounterState state,
        string combatantId)
    {
        return state.Participants.Any(participant =>
            string.Equals(
                participant.Combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal)
            && participant.Combatant.LifecycleState
                == CombatantLifecycleState.Conscious);
    }

    private static GridPosition FindPosition(
        EncounterState encounter,
        string combatantId)
    {
        EncounterParticipantState? participant = encounter.Participants
            .FirstOrDefault(candidate => string.Equals(
                candidate.Combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal));

        return participant is null
            ? throw new ArgumentException(
                $"Actor '{combatantId}' is not an encounter participant.",
                nameof(combatantId))
            : participant.Position;
    }
}

/// What a movement path actually did, once anything that reacted to it has
/// been resolved. The movement step is null when the mover was dropped
/// before it managed a single square.
internal sealed record EncounterMovementStagingResult(
    EncounterState State,
    int CursorAfter,
    EncounterCombatStepResult? MovementStep,
    IReadOnlyList<EncounterCombatStepResult> ReactionSteps);

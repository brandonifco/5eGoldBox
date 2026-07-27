using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public static class EncounterTurnAdvancementRules
{
    public static EncounterTurnAdvancementResult Resolve(
        EncounterState state,
        EncounterTurnAdvancementCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        EncounterRules.ValidateState(state);
        ValidateCommand(command);

        if (state.LifecycleState
            != EncounterLifecycleState.Active)
        {
            throw new InvalidOperationException(
                "A completed encounter cannot advance turns.");
        }

        if (command.ExpectedRevision != state.Revision)
        {
            throw new InvalidOperationException(
                $"Expected encounter revision '{command.ExpectedRevision}', but the current revision is '{state.Revision}'.");
        }

        int currentParticipantIndex =
            FindParticipantIndex(
                state.Participants,
                command.ActorCombatantId);

        if (currentParticipantIndex < 0)
        {
            throw new ArgumentException(
                $"Actor '{command.ActorCombatantId}' is not an encounter participant.",
                nameof(command));
        }

        if (!string.Equals(
            command.ActorCombatantId,
            state.ActiveCombatantId,
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Only the active combatant can end the current turn.");
        }

        if (string.Equals(
            state.PendingDeathSavingThrowCombatantId,
            command.ActorCombatantId,
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The active combatant must resolve its pending death saving throw before ending the turn.");
        }

        long resolvedRevision =
            checked(state.Revision + 1);

        CombatTurnState resolvedTurnState =
            state.TurnState;

        List<string> skippedCombatantIds = [];

        EncounterParticipantState?
            nextParticipant = null;

        int nextParticipantIndex = -1;

        for (int offset = 1;
            offset < state.Participants.Count;
            offset++)
        {
            resolvedTurnState =
                AdvanceTurnChecked(
                    resolvedTurnState);

            string candidateCombatantId =
                CombatTurnRules.GetActiveCombatant(
                    resolvedTurnState)
                .CombatantId;

            int candidateParticipantIndex =
                FindParticipantIndex(
                    state.Participants,
                    candidateCombatantId);

            if (candidateParticipantIndex < 0)
            {
                throw new ArgumentException(
                    $"Initiative combatant '{candidateCombatantId}' is not an encounter participant.",
                    nameof(state));
            }

            EncounterParticipantState candidate =
                state.Participants[
                    candidateParticipantIndex];

            if (candidate.Combatant.IsTerminal)
            {
                skippedCombatantIds.Add(
                    candidateCombatantId);

                continue;
            }

            nextParticipant = candidate;
            nextParticipantIndex =
                candidateParticipantIndex;

            break;
        }

        if (nextParticipant is null)
        {
            throw new InvalidOperationException(
                "No other nonterminal combatant is available to take the next turn. Resolve the encounter outcome before advancing.");
        }

        CombatTurnResources refreshedResources =
            CombatTurnResourceRules.StartTurn(
                nextParticipant.TurnResources
                    .MovementSpeedFeet);

        EncounterParticipantState[] participants =
            state.Participants.ToArray();

        participants[nextParticipantIndex] =
            nextParticipant with
            {
                TurnResources = refreshedResources
            };

        bool startedNewRound =
            resolvedTurnState.RoundNumber
                > state.TurnState.RoundNumber;

        if (startedNewRound)
        {
            participants =
                DecrementActiveEffects(participants);
        }

        IReadOnlyList<string>
            protectedSkippedCombatantIds =
                Array.AsReadOnly(
                    skippedCombatantIds.ToArray());

        string? pendingDeathSavingThrowCombatantId =
            nextParticipant.Combatant.LifecycleState
                == CombatantLifecycleState.Dying
            ? nextParticipant.Combatant.CombatantId
            : null;

        EncounterState resolvedState = state with
        {
            Revision = resolvedRevision,
            TurnState = resolvedTurnState,
            Participants =
                Array.AsReadOnly(participants),
            PendingDeathSavingThrowCombatantId =
                pendingDeathSavingThrowCombatantId
        };

        EncounterRules.ValidateState(resolvedState);

        return new EncounterTurnAdvancementResult
        {
            EndedTurnCombatantId =
                command.ActorCombatantId,
            ActiveCombatantId =
                nextParticipant.Combatant
                    .CombatantId,
            PreviousRoundNumber =
                state.TurnState.RoundNumber,
            RoundNumber =
                resolvedTurnState.RoundNumber,
            PreviousActivePosition =
                state.TurnState.ActivePosition,
            ActivePosition =
                resolvedTurnState.ActivePosition,
            SkippedCombatantIds =
                protectedSkippedCombatantIds,
            State = resolvedState
        };
    }

    private static void ValidateCommand(
        EncounterTurnAdvancementCommand command)
    {
        if (command.ExpectedRevision < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                command.ExpectedRevision,
                "Expected encounter revision must be at least 1.");
        }

        if (string.IsNullOrWhiteSpace(
            command.ActorCombatantId))
        {
            throw new ArgumentException(
                "Actor combatant ID is required.",
                nameof(command));
        }
    }

    private static CombatTurnState AdvanceTurnChecked(
        CombatTurnState state)
    {
        if (state.ActivePosition
                == state.InitiativeOrder.Count
            && state.RoundNumber == int.MaxValue)
        {
            throw new OverflowException(
                "Encounter round number cannot be incremented.");
        }

        return CombatTurnRules.AdvanceTurn(state);
    }

    /// Ticks every active effect down by one round and drops what runs out.
    ///
    /// Ticked once per round rather than once per the sustaining caster's
    /// turn — this engine has no notion of "whose turn started the effect,"
    /// and every effect on a target was created at the same encounter moment
    /// as any other copy of it, so ticking them all in lockstep at round start
    /// keeps a spell affecting several targets expiring together.
    private static EncounterParticipantState[] DecrementActiveEffects(
        EncounterParticipantState[] participants)
    {
        List<(string SourceCombatantId, string EffectId)> expired = [];

        for (int index = 0; index < participants.Length; index++)
        {
            EncounterParticipantState participant =
                participants[index];

            if (participant.ActiveEffects.Count == 0)
            {
                continue;
            }

            List<ActiveEffect> remaining = [];

            foreach (ActiveEffect effect in participant.ActiveEffects)
            {
                int remainingRounds =
                    effect.RemainingRounds - 1;

                if (remainingRounds > 0)
                {
                    remaining.Add(effect with
                    {
                        RemainingRounds = remainingRounds
                    });
                }
                else
                {
                    expired.Add((
                        effect.SourceCombatantId,
                        effect.EffectId));
                }
            }

            participants[index] = participant with
            {
                ActiveEffects =
                    Array.AsReadOnly(remaining.ToArray())
            };
        }

        foreach ((string sourceCombatantId, string effectId)
            in expired)
        {
            participants =
                ActiveEffectRules.ClearConcentrationIfMatching(
                    participants,
                    sourceCombatantId,
                    effectId);
        }

        return participants;
    }

    private static int FindParticipantIndex(
        IReadOnlyList<EncounterParticipantState> participants,
        string combatantId)
    {
        for (int index = 0;
            index < participants.Count;
            index++)
        {
            if (string.Equals(
                participants[index]
                    .Combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }
}

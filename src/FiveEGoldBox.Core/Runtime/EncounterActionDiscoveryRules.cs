namespace FiveEGoldBox.Core.Runtime;

public static class EncounterActionDiscoveryRules
{
    public static EncounterActionDiscoveryResult Discover(
        EncounterState state,
        IReadOnlyList<EncounterActionCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(candidates);

        EncounterRules.ValidateState(state);
        ValidateCandidates(candidates);

        EncounterActionEvaluation[] evaluations =
            candidates
                .Select(candidate =>
                    EvaluateCandidate(state, candidate))
                .ToArray();

        return new EncounterActionDiscoveryResult
        {
            EncounterId = state.EncounterId,
            EncounterRevision = state.Revision,
            Evaluations =
                Array.AsReadOnly(evaluations)
        };
    }

    private static EncounterActionEvaluation
        EvaluateCandidate(
            EncounterState state,
            EncounterActionCandidate candidate)
    {
        if (state.LifecycleState
            != EncounterLifecycleState.Active)
        {
            return CreateUnavailableEvaluation(
                state,
                candidate,
                EncounterActionUnavailabilityReason
                    .EncounterCompleted);
        }

        EncounterParticipantState? participant =
            state.Participants.FirstOrDefault(
                participant =>
                    participant.Combatant.CombatantId
                    == candidate.ActorCombatantId);

        if (participant is null)
        {
            return CreateUnavailableEvaluation(
                state,
                candidate,
                EncounterActionUnavailabilityReason
                    .ActorNotParticipant);
        }

        if (participant.Combatant.LifecycleState
            != CombatantLifecycleState.Conscious)
        {
            return CreateUnavailableEvaluation(
                state,
                candidate,
                EncounterActionUnavailabilityReason
                    .ActorCannotAct);
        }

        return candidate.Timing switch
        {
            EncounterActionTiming.Action =>
                EvaluateAction(
                    state,
                    participant,
                    candidate),

            EncounterActionTiming.BonusAction =>
                EvaluateBonusAction(
                    state,
                    participant,
                    candidate),

            EncounterActionTiming.Reaction =>
                EvaluateReaction(
                    state,
                    participant,
                    candidate),

            EncounterActionTiming.Movement =>
                EvaluateMovement(
                    state,
                    participant,
                    candidate),

            EncounterActionTiming.TurnBoundary =>
                CreateUnavailableEvaluation(
                    state,
                    candidate,
                    EncounterActionUnavailabilityReason
                        .UnsupportedTiming),

            _ => throw new ArgumentOutOfRangeException(
                nameof(candidate),
                candidate.Timing,
                "Unsupported encounter action timing.")
        };
    }

    private static EncounterActionEvaluation
        EvaluateAction(
            EncounterState state,
            EncounterParticipantState participant,
            EncounterActionCandidate candidate)
    {
        if (candidate.ActorCombatantId
            != state.ActiveCombatantId)
        {
            return CreateUnavailableEvaluation(
                state,
                candidate,
                EncounterActionUnavailabilityReason
                    .ActorNotActive);
        }

        if (!participant.TurnResources.HasActionAvailable)
        {
            return CreateUnavailableEvaluation(
                state,
                candidate,
                EncounterActionUnavailabilityReason
                    .ActionUnavailable);
        }

        return CreateLegalEvaluation(state, candidate);
    }

    private static EncounterActionEvaluation
        EvaluateBonusAction(
            EncounterState state,
            EncounterParticipantState participant,
            EncounterActionCandidate candidate)
    {
        if (candidate.ActorCombatantId
            != state.ActiveCombatantId)
        {
            return CreateUnavailableEvaluation(
                state,
                candidate,
                EncounterActionUnavailabilityReason
                    .ActorNotActive);
        }

        if (!participant.TurnResources
            .HasBonusActionAvailable)
        {
            return CreateUnavailableEvaluation(
                state,
                candidate,
                EncounterActionUnavailabilityReason
                    .BonusActionUnavailable);
        }

        return CreateLegalEvaluation(state, candidate);
    }

    private static EncounterActionEvaluation
        EvaluateReaction(
            EncounterState state,
            EncounterParticipantState participant,
            EncounterActionCandidate candidate)
    {
        if (!participant.TurnResources.HasReactionAvailable)
        {
            return CreateUnavailableEvaluation(
                state,
                candidate,
                EncounterActionUnavailabilityReason
                    .ReactionUnavailable);
        }

        return CreateUnavailableEvaluation(
            state,
            candidate,
            EncounterActionUnavailabilityReason
                .ReactionWindowRequired);
    }

    private static EncounterActionEvaluation
        EvaluateMovement(
            EncounterState state,
            EncounterParticipantState participant,
            EncounterActionCandidate candidate)
    {
        if (candidate.ActorCombatantId
            != state.ActiveCombatantId)
        {
            return CreateUnavailableEvaluation(
                state,
                candidate,
                EncounterActionUnavailabilityReason
                    .ActorNotActive);
        }

        if (participant.TurnResources
            .MovementRemainingFeet <= 0)
        {
            return CreateUnavailableEvaluation(
                state,
                candidate,
                EncounterActionUnavailabilityReason
                    .MovementUnavailable);
        }

        return CreateLegalEvaluation(state, candidate);
    }

    private static EncounterActionEvaluation
        CreateLegalEvaluation(
            EncounterState state,
            EncounterActionCandidate candidate)
    {
        return new EncounterActionEvaluation
        {
            ActionOptionId = candidate.ActionOptionId,
            ActorCombatantId =
                candidate.ActorCombatantId,
            EncounterRevision = state.Revision,
            IsCommonlyLegal = true,
            UnavailabilityReason =
                EncounterActionUnavailabilityReason.None
        };
    }

    private static EncounterActionEvaluation
        CreateUnavailableEvaluation(
            EncounterState state,
            EncounterActionCandidate candidate,
            EncounterActionUnavailabilityReason reason)
    {
        return new EncounterActionEvaluation
        {
            ActionOptionId = candidate.ActionOptionId,
            ActorCombatantId =
                candidate.ActorCombatantId,
            EncounterRevision = state.Revision,
            IsCommonlyLegal = false,
            UnavailabilityReason = reason
        };
    }

    private static void ValidateCandidates(
        IReadOnlyList<EncounterActionCandidate> candidates)
    {
        HashSet<string> actionOptionIds =
            new(StringComparer.Ordinal);

        foreach (EncounterActionCandidate candidate
            in candidates)
        {
            ArgumentNullException.ThrowIfNull(candidate);

            if (string.IsNullOrWhiteSpace(
                candidate.ActionOptionId))
            {
                throw new ArgumentException(
                    "Action option ID is required.",
                    nameof(candidates));
            }

            if (string.IsNullOrWhiteSpace(
                candidate.ActorCombatantId))
            {
                throw new ArgumentException(
                    "Actor combatant ID is required.",
                    nameof(candidates));
            }

            if (!Enum.IsDefined(candidate.Timing))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(candidates),
                    candidate.Timing,
                    "Unsupported encounter action timing.");
            }

            if (!actionOptionIds.Add(
                candidate.ActionOptionId))
            {
                throw new ArgumentException(
                    $"Duplicate action option ID '{candidate.ActionOptionId}' is not allowed.",
                    nameof(candidates));
            }
        }
    }
}

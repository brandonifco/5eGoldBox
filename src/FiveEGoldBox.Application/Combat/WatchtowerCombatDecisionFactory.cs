using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal static class WatchtowerCombatDecisionFactory
{
    internal static WatchtowerCombatDecision Create(
        ApplicationSessionState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        ApplicationSessionRules.Validate(state);

        EncounterState encounter = state.ActiveEncounter!.Encounter;

        if (encounter.LifecycleState
            == EncounterLifecycleState.Completed)
        {
            return new WatchtowerCombatDecision
            {
                State = WatchtowerCombatDecisionState.CombatCompleted,
                EncounterRevision = encounter.Revision,
                ActiveCombatantId = null,
                PendingDeathSavingThrowCombatantId = null,
                Movement = null,
                WeaponAttack = null,
                EndTurn = null,
                WinningSideId = encounter.WinningSideId
            };
        }

        EncounterParticipantState activeParticipant =
            FindParticipant(encounter, encounter.ActiveCombatantId);

        bool isConsciousPartyParticipant =
            string.Equals(
                activeParticipant.SideId,
                WatchtowerSignalEncounter.PartySideId,
                StringComparison.Ordinal)
            && activeParticipant.Combatant.LifecycleState
                == CombatantLifecycleState.Conscious
            && encounter.PendingDeathSavingThrowCombatantId is null;

        if (!isConsciousPartyParticipant)
        {
            return new WatchtowerCombatDecision
            {
                State = WatchtowerCombatDecisionState.AutomaticProcessingRequired,
                EncounterRevision = encounter.Revision,
                ActiveCombatantId = encounter.ActiveCombatantId,
                PendingDeathSavingThrowCombatantId =
                    encounter.PendingDeathSavingThrowCombatantId,
                Movement = null,
                WeaponAttack = null,
                EndTurn = null,
                WinningSideId = null
            };
        }

        WeaponAttack weapon = GetFixedWeapon(activeParticipant);
        WatchtowerCombatTargetOption[] targets =
            encounter.Participants
                .Where(participant => !string.Equals(
                    participant.SideId,
                    activeParticipant.SideId,
                    StringComparison.Ordinal))
                .Select(participant => CreateTargetOption(
                    encounter,
                    activeParticipant,
                    participant,
                    weapon))
                .ToArray();

        bool hasLegalTarget = targets.Any(target => target.IsAvailable);
        EncounterActionUnavailabilityReason weaponReason =
            hasLegalTarget
                ? EncounterActionUnavailabilityReason.None
                : targets.FirstOrDefault()?.UnavailabilityReason
                    ?? EncounterActionUnavailabilityReason.TargetNotParticipant;

        int movementRemaining =
            activeParticipant.TurnResources.MovementRemainingFeet;
        IReadOnlyList<WatchtowerCombatMovementDestinationOption>
            movementDestinations = CreateMovementDestinationOptions(
                encounter,
                activeParticipant.Combatant.CombatantId);
        bool hasMovementDestination =
            movementDestinations.Count > 0;

        return new WatchtowerCombatDecision
        {
            State = WatchtowerCombatDecisionState.PlayerDecisionRequired,
            EncounterRevision = encounter.Revision,
            ActiveCombatantId = encounter.ActiveCombatantId,
            PendingDeathSavingThrowCombatantId = null,
            Movement = new WatchtowerCombatMovementOption
            {
                IsAvailable = hasMovementDestination,
                MovementRemainingFeet = movementRemaining,
                UnavailabilityReason = hasMovementDestination
                    ? EncounterActionUnavailabilityReason.None
                    : EncounterActionUnavailabilityReason.MovementUnavailable,
                DestinationOptions = movementDestinations
            },
            WeaponAttack = new WatchtowerCombatWeaponAttackOption
            {
                WeaponId = weapon.WeaponId,
                IsAvailable = hasLegalTarget,
                UnavailabilityReason = weaponReason,
                Targets = Array.AsReadOnly(targets)
            },
            EndTurn = new WatchtowerCombatEndTurnOption
            {
                IsAvailable = true,
                UnavailabilityReason = EncounterActionUnavailabilityReason.None
            },
            WinningSideId = null
        };
    }

    private static IReadOnlyList<WatchtowerCombatMovementDestinationOption>
        CreateMovementDestinationOptions(
            EncounterState encounter,
            string actorCombatantId)
    {
        IReadOnlyList<EncounterMovementResult> movements =
            WatchtowerCombatPathSearch.EnumerateReachableMovements(
                encounter,
                actorCombatantId);
        WatchtowerCombatMovementDestinationOption[] options =
            new WatchtowerCombatMovementDestinationOption[movements.Count];

        for (int index = 0; index < movements.Count; index++)
        {
            EncounterMovementResult movement = movements[index];
            GridPosition[] path = movement.Path.ToArray();

            options[index] =
                new WatchtowerCombatMovementDestinationOption
                {
                    Destination = movement.EndingPosition,
                    Path = Array.AsReadOnly(path),
                    MovementSpentFeet = movement.MovementSpentFeet
                };
        }

        return Array.AsReadOnly(options);
    }

    private static WatchtowerCombatTargetOption CreateTargetOption(
        EncounterState encounter,
        EncounterParticipantState actor,
        EncounterParticipantState target,
        WeaponAttack weapon)
    {
        EncounterWeaponAttackPrerequisiteEvaluation evaluation =
            EncounterWeaponAttackPrerequisiteRules.Evaluate(
                encounter,
                actor.Combatant.CombatantId,
                target.Combatant.CombatantId,
                weapon.WeaponId);

        return new WatchtowerCombatTargetOption
        {
            TargetCombatantId = target.Combatant.CombatantId,
            IsAvailable = evaluation.IsLegal,
            UnavailabilityReason = evaluation.UnavailabilityReason,
            AttackRollMode = evaluation.AttackRollMode,
            DistanceFeet = evaluation.DistanceFeet
        };
    }

    internal static EncounterParticipantState FindParticipant(
        EncounterState encounter,
        string combatantId)
    {
        return encounter.Participants.Single(participant =>
            string.Equals(
                participant.Combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal));
    }

    internal static WeaponAttack GetFixedWeapon(
        EncounterParticipantState participant)
    {
        if (participant.CombatProfile.WeaponAttacks.Count != 1)
        {
            throw new InvalidOperationException(
                $"Combatant '{participant.Combatant.CombatantId}' must have exactly one bounded weapon attack.");
        }

        return participant.CombatProfile.WeaponAttacks[0];
    }
}

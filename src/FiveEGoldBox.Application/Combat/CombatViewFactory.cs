using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal static class CombatViewFactory
{
    internal static CombatView Create(
        EncounterState encounter,
        IReadOnlySet<string> clientControlledCombatantIds)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(clientControlledCombatantIds);

        CombatantView[] combatants = encounter.Participants
            .Select(CreateCombatantView)
            .ToArray();

        string? activeCombatantId =
            encounter.LifecycleState == EncounterLifecycleState.Completed
                ? null
                : encounter.ActiveCombatantId;
        string? pendingDeathSavingThrowCombatantId =
            encounter.LifecycleState == EncounterLifecycleState.Completed
                ? null
                : encounter.PendingDeathSavingThrowCombatantId;
        string? winningSideId =
            encounter.LifecycleState == EncounterLifecycleState.Completed
                ? encounter.WinningSideId
                : null;

        CombatDecision decision = CreateDecision(
            encounter,
            clientControlledCombatantIds,
            activeCombatantId,
            pendingDeathSavingThrowCombatantId,
            winningSideId);

        return new CombatView(
            encounter.EncounterId,
            encounter.Revision,
            encounter.Battlefield.BattlefieldId,
            encounter.Battlefield.Width,
            encounter.Battlefield.Height,
            encounter.LifecycleState,
            encounter.TurnState.RoundNumber,
            activeCombatantId,
            pendingDeathSavingThrowCombatantId,
            winningSideId,
            combatants,
            decision);
    }

    private static CombatantView CreateCombatantView(
        EncounterParticipantState participant)
    {
        return new CombatantView(
            participant.Combatant.CombatantId,
            participant.SideId,
            participant.Position,
            participant.Combatant.LifecycleState,
            participant.Combatant.Health,
            participant.CombatProfile.ArmorClass,
            participant.TurnResources.MovementSpeedFeet,
            participant.TurnResources.MovementSpentFeet,
            participant.TurnResources.MovementRemainingFeet,
            participant.TurnResources.HasActionAvailable,
            participant.TurnResources.HasBonusActionAvailable,
            participant.TurnResources.HasReactionAvailable);
    }

    private static CombatDecision CreateDecision(
        EncounterState encounter,
        IReadOnlySet<string> clientControlledCombatantIds,
        string? activeCombatantId,
        string? pendingDeathSavingThrowCombatantId,
        string? winningSideId)
    {
        if (encounter.LifecycleState == EncounterLifecycleState.Completed)
        {
            return new CombatDecision(
                CombatDecisionState.CombatCompleted,
                encounter.Revision,
                null,
                null,
                null,
                Array.Empty<CombatWeaponAttackOption>(),
                null,
                winningSideId);
        }

        EncounterParticipantState activeParticipant =
            FindParticipant(encounter, activeCombatantId!);

        bool playerDecisionRequired =
            clientControlledCombatantIds.Contains(activeCombatantId!)
            && activeParticipant.Combatant.LifecycleState
                == CombatantLifecycleState.Conscious
            && pendingDeathSavingThrowCombatantId is null;

        if (!playerDecisionRequired)
        {
            return new CombatDecision(
                CombatDecisionState.AutomaticProcessingRequired,
                encounter.Revision,
                activeCombatantId,
                pendingDeathSavingThrowCombatantId,
                null,
                Array.Empty<CombatWeaponAttackOption>(),
                null,
                null);
        }

        CombatMovementOption movement = CreateMovementOption(
            encounter,
            activeParticipant);
        IReadOnlyList<CombatWeaponAttackOption> weaponAttacks =
            CreateWeaponAttackOptions(
                encounter,
                activeParticipant);
        CombatEndTurnOption endTurn = new(
            true,
            EncounterActionUnavailabilityReason.None);

        return new CombatDecision(
            CombatDecisionState.PlayerDecisionRequired,
            encounter.Revision,
            activeCombatantId,
            null,
            movement,
            weaponAttacks,
            endTurn,
            null);
    }

    private static CombatMovementOption CreateMovementOption(
        EncounterState encounter,
        EncounterParticipantState activeParticipant)
    {
        IReadOnlyList<EncounterMovementResult> movements =
            CombatPathSearch.EnumerateReachableMovements(
                encounter,
                activeParticipant.Combatant.CombatantId);
        CombatMovementDestinationOption[] destinations =
            new CombatMovementDestinationOption[movements.Count];

        for (int index = 0; index < movements.Count; index++)
        {
            EncounterMovementResult movement = movements[index];

            destinations[index] = new CombatMovementDestinationOption(
                movement.EndingPosition,
                movement.Path,
                movement.MovementSpentFeet);
        }

        bool isAvailable = destinations.Length > 0;

        return new CombatMovementOption(
            isAvailable,
            activeParticipant.TurnResources.MovementRemainingFeet,
            isAvailable
                ? EncounterActionUnavailabilityReason.None
                : EncounterActionUnavailabilityReason.MovementUnavailable,
            destinations);
    }

    private static IReadOnlyList<CombatWeaponAttackOption>
        CreateWeaponAttackOptions(
            EncounterState encounter,
            EncounterParticipantState activeParticipant)
    {
        List<CombatWeaponAttackOption> weaponOptions = [];
        EncounterParticipantState[] hostileParticipants =
            encounter.Participants
                .Where(participant => !string.Equals(
                    participant.SideId,
                    activeParticipant.SideId,
                    StringComparison.Ordinal))
                .ToArray();

        foreach (WeaponAttack weapon
            in activeParticipant.CombatProfile.WeaponAttacks)
        {
            CombatTargetOption[] targets =
                new CombatTargetOption[hostileParticipants.Length];

            for (int index = 0;
                index < hostileParticipants.Length;
                index++)
            {
                EncounterParticipantState target =
                    hostileParticipants[index];
                CombatAttackAvailability evaluation =
                    CombatAttackStaging.EvaluateAvailability(
                        encounter,
                        activeParticipant.Combatant.CombatantId,
                        target.Combatant.CombatantId,
                        weapon.WeaponId);

                targets[index] = new CombatTargetOption(
                    target.Combatant.CombatantId,
                    evaluation.IsLegal,
                    evaluation.UnavailabilityReason,
                    evaluation.AttackRollMode,
                    evaluation.DistanceFeet);
            }

            weaponOptions.Add(
                new CombatWeaponAttackOption(
                    weapon.WeaponId,
                    targets.Any(target => target.IsAvailable),
                    targets));
        }

        return Array.AsReadOnly(weaponOptions.ToArray());
    }

    private static EncounterParticipantState FindParticipant(
        EncounterState encounter,
        string combatantId)
    {
        return encounter.Participants.Single(participant =>
            string.Equals(
                participant.Combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal));
    }
}

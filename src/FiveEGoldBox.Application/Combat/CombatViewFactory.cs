using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal static class CombatViewFactory
{
    internal static CombatView Create(
        EncounterState encounter,
        IReadOnlySet<string> clientControlledCombatantIds,
        IReadOnlyDictionary<string, string>? combatantDisplayNames = null,
        IReadOnlyDictionary<string, string>? combatantMonsterIds = null)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(clientControlledCombatantIds);

        CombatantView[] combatants = encounter.Participants
            .Select(participant => CreateCombatantView(
                participant,
                combatantDisplayNames,
                combatantMonsterIds))
            .ToArray();

        // The same fact EncounterPartySideResolver resolves from the
        // scenario's own EncounterDefinition.PartySideId, reached here
        // without that lookup: every party member's participant carries the
        // side ID it was placed on, and clientControlledCombatantIds is
        // already exactly the party's own member IDs (Query's caller), so
        // the first controlled participant found names the party's side.
        string partySideId = encounter.Participants
            .Where(participant => clientControlledCombatantIds.Contains(
                participant.Combatant.CombatantId))
            .Select(participant => participant.SideId)
            .FirstOrDefault()
            ?? throw new InvalidOperationException(
                "No client-controlled combatant is part of this encounter.");

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
            partySideId,
            combatants,
            decision,
            encounter.InitiativeOrder
                .OrderBy(entry => entry.Position)
                .Select(entry => entry.CombatantId)
                .ToArray());
    }

    private static CombatantView CreateCombatantView(
        EncounterParticipantState participant,
        IReadOnlyDictionary<string, string>? combatantDisplayNames,
        IReadOnlyDictionary<string, string>? combatantMonsterIds)
    {
        string combatantId = participant.Combatant.CombatantId;
        string displayName =
            combatantDisplayNames is not null
                && combatantDisplayNames.TryGetValue(
                    combatantId,
                    out string? resolvedName)
                ? resolvedName
                : combatantId;
        string? monsterId =
            combatantMonsterIds is not null
                && combatantMonsterIds.TryGetValue(
                    combatantId,
                    out string? resolvedMonsterId)
                ? resolvedMonsterId
                : null;

        return new CombatantView(
            combatantId,
            displayName,
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
            participant.TurnResources.HasReactionAvailable,
            monsterId);
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
                Array.Empty<CombatSpellAttackOption>(),
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
                Array.Empty<CombatSpellAttackOption>(),
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
        IReadOnlyList<CombatSpellAttackOption> spellAttacks =
            CreateSpellAttackOptions(
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
            spellAttacks,
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
                    evaluation.DistanceFeet,
                    evaluation.Cover);
            }

            bool hasLegalTarget = targets.Any(target => target.IsAvailable);

            weaponOptions.Add(
                new CombatWeaponAttackOption(
                    weapon.WeaponId,
                    hasLegalTarget,
                    hasLegalTarget
                        ? EncounterActionUnavailabilityReason.None
                        : targets.FirstOrDefault()?.UnavailabilityReason
                            ?? EncounterActionUnavailabilityReason
                                .TargetNotParticipant,
                    targets));
        }

        return Array.AsReadOnly(weaponOptions.ToArray());
    }

    /// Every other participant is offered as a candidate, including the
    /// caster — an ally-only spell like Bless can land on the caster alone,
    /// which a hostile-only candidate list (as weapons use) would rule out
    /// before the prerequisite evaluation ever got the chance to say so.
    private static IReadOnlyList<CombatSpellAttackOption>
        CreateSpellAttackOptions(
            EncounterState encounter,
            EncounterParticipantState activeParticipant)
    {
        List<CombatSpellAttackOption> spellOptions = [];

        foreach (SpellAttack spell
            in activeParticipant.CombatProfile.SpellAttacks)
        {
            CombatTargetOption[] targets =
                new CombatTargetOption[encounter.Participants.Count];

            for (int index = 0;
                index < encounter.Participants.Count;
                index++)
            {
                EncounterParticipantState target =
                    encounter.Participants[index];
                CombatSpellAttackAvailability evaluation =
                    CombatSpellAttackStaging.EvaluateAvailability(
                        encounter,
                        activeParticipant.Combatant.CombatantId,
                        target.Combatant.CombatantId,
                        spell.SpellId);

                targets[index] = new CombatTargetOption(
                    target.Combatant.CombatantId,
                    evaluation.IsLegal,
                    evaluation.UnavailabilityReason,
                    evaluation.AttackRollMode,
                    evaluation.DistanceFeet,
                    evaluation.Cover,
                    spell.SaveAbility,
                    spell.Resolution == SpellResolutionKind.SavingThrow
                        ? spell.SaveDc
                        : null);
            }

            bool hasLegalTarget = targets.Any(target => target.IsAvailable);

            spellOptions.Add(
                new CombatSpellAttackOption(
                    spell.SpellId,
                    spell.SpellName,
                    hasLegalTarget,
                    hasLegalTarget
                        ? EncounterActionUnavailabilityReason.None
                        : targets.FirstOrDefault()?.UnavailabilityReason
                            ?? EncounterActionUnavailabilityReason
                                .TargetNotParticipant,
                    targets,
                    CreateSpellTargetCombinations(spell, targets)));
        }

        return Array.AsReadOnly(spellOptions.ToArray());
    }

    /// See SpellTargetCombinationRules — EncounterCombatDecisionFactory
    /// builds the same options independently for the write path, so the
    /// combination algorithm itself lives there rather than here twice.
    private static IReadOnlyList<CombatTargetCombinationOption>
        CreateSpellTargetCombinations(
            SpellAttack spell,
            IReadOnlyList<CombatTargetOption> targets)
    {
        string[] legalTargetIds = targets
            .Where(target => target.IsAvailable)
            .Select(target => target.TargetCombatantId)
            .ToArray();

        return SpellTargetCombinationRules
            .Create(spell, legalTargetIds)
            .Select(combination =>
                new CombatTargetCombinationOption(combination))
            .ToArray();
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

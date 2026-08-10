using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal static class EncounterCombatDecisionFactory
{
    internal static EncounterCombatDecision Create(
        ApplicationSessionState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        ApplicationSessionRules.Validate(state);

        EncounterState encounter = state.ActiveEncounter!.Encounter;

        if (encounter.LifecycleState
            == EncounterLifecycleState.Completed)
        {
            return new EncounterCombatDecision
            {
                State = CombatDecisionState.CombatCompleted,
                EncounterRevision = encounter.Revision,
                ActiveCombatantId = null,
                PendingDeathSavingThrowCombatantId = null,
                Movement = null,
                WeaponAttacks = Array.Empty<EncounterCombatWeaponAttackOption>(),
                SpellAttacks = Array.Empty<EncounterCombatSpellAttackOption>(),
                EndTurn = null,
                WinningSideId = encounter.WinningSideId
            };
        }

        EncounterParticipantState activeParticipant =
            FindParticipant(encounter, encounter.ActiveCombatantId);
        string partySideId =
            EncounterPartySideResolver.Resolve(state, encounter);

        bool isConsciousPartyParticipant =
            string.Equals(
                activeParticipant.SideId,
                partySideId,
                StringComparison.Ordinal)
            && activeParticipant.Combatant.LifecycleState
                == CombatantLifecycleState.Conscious
            && encounter.PendingDeathSavingThrowCombatantId is null;

        if (!isConsciousPartyParticipant)
        {
            return new EncounterCombatDecision
            {
                State = CombatDecisionState.AutomaticProcessingRequired,
                EncounterRevision = encounter.Revision,
                ActiveCombatantId = encounter.ActiveCombatantId,
                PendingDeathSavingThrowCombatantId =
                    encounter.PendingDeathSavingThrowCombatantId,
                Movement = null,
                WeaponAttacks = Array.Empty<EncounterCombatWeaponAttackOption>(),
                SpellAttacks = Array.Empty<EncounterCombatSpellAttackOption>(),
                EndTurn = null,
                WinningSideId = null
            };
        }

        EncounterCombatWeaponAttackOption[] weaponAttacks =
            GetWeapons(activeParticipant)
                .Select(weapon => CreateWeaponAttackOption(
                    encounter,
                    activeParticipant,
                    weapon))
                .ToArray();

        EncounterCombatSpellAttackOption[] spellAttacks =
            activeParticipant.CombatProfile.SpellAttacks
                .Select(spell => CreateSpellAttackOption(
                    encounter,
                    activeParticipant,
                    spell))
                .ToArray();

        int movementRemaining =
            activeParticipant.TurnResources.MovementRemainingFeet;
        IReadOnlyList<EncounterCombatMovementDestinationOption>
            movementDestinations = CreateMovementDestinationOptions(
                encounter,
                activeParticipant.Combatant.CombatantId);
        bool hasMovementDestination =
            movementDestinations.Count > 0;

        return new EncounterCombatDecision
        {
            State = CombatDecisionState.PlayerDecisionRequired,
            EncounterRevision = encounter.Revision,
            ActiveCombatantId = encounter.ActiveCombatantId,
            PendingDeathSavingThrowCombatantId = null,
            Movement = new EncounterCombatMovementOption
            {
                IsAvailable = hasMovementDestination,
                MovementRemainingFeet = movementRemaining,
                UnavailabilityReason = hasMovementDestination
                    ? EncounterActionUnavailabilityReason.None
                    : EncounterActionUnavailabilityReason.MovementUnavailable,
                DestinationOptions = movementDestinations
            },
            WeaponAttacks = Array.AsReadOnly(weaponAttacks),
            SpellAttacks = Array.AsReadOnly(spellAttacks),
            EndTurn = new EncounterCombatEndTurnOption
            {
                IsAvailable = true,
                UnavailabilityReason = EncounterActionUnavailabilityReason.None
            },
            WinningSideId = null
        };
    }

    private static IReadOnlyList<EncounterCombatMovementDestinationOption>
        CreateMovementDestinationOptions(
            EncounterState encounter,
            string actorCombatantId)
    {
        IReadOnlyList<EncounterMovementResult> movements =
            EncounterCombatPathSearch.EnumerateReachableMovements(
                encounter,
                actorCombatantId);
        EncounterCombatMovementDestinationOption[] options =
            new EncounterCombatMovementDestinationOption[movements.Count];

        for (int index = 0; index < movements.Count; index++)
        {
            EncounterMovementResult movement = movements[index];
            GridPosition[] path = movement.Path.ToArray();

            options[index] =
                new EncounterCombatMovementDestinationOption
                {
                    Destination = movement.EndingPosition,
                    Path = Array.AsReadOnly(path),
                    MovementSpentFeet = movement.MovementSpentFeet
                };
        }

        return Array.AsReadOnly(options);
    }

    /// One weapon's whole offer: every opposing combatant it could name as a
    /// target, and whether any of them is actually reachable right now.
    /// Called once per weapon a combatant carries, so a character with a bow
    /// and a dagger is offered both rather than whichever GetFixedWeapon used
    /// to pick for it.
    private static EncounterCombatWeaponAttackOption CreateWeaponAttackOption(
        EncounterState encounter,
        EncounterParticipantState actor,
        WeaponAttack weapon)
    {
        EncounterCombatTargetOption[] targets =
            encounter.Participants
                .Where(participant => !string.Equals(
                    participant.SideId,
                    actor.SideId,
                    StringComparison.Ordinal))
                .Select(participant => CreateTargetOption(
                    encounter,
                    actor,
                    participant,
                    weapon))
                .ToArray();

        bool hasLegalTarget = targets.Any(target => target.IsAvailable);
        EncounterActionUnavailabilityReason weaponReason =
            hasLegalTarget
                ? EncounterActionUnavailabilityReason.None
                : targets.FirstOrDefault()?.UnavailabilityReason
                    ?? EncounterActionUnavailabilityReason.TargetNotParticipant;

        return new EncounterCombatWeaponAttackOption
        {
            WeaponId = weapon.WeaponId,
            IsAvailable = hasLegalTarget,
            UnavailabilityReason = weaponReason,
            Targets = Array.AsReadOnly(targets)
        };
    }

    private static EncounterCombatTargetOption CreateTargetOption(
        EncounterState encounter,
        EncounterParticipantState actor,
        EncounterParticipantState target,
        WeaponAttack weapon)
    {
        EncounterCombatAttackAvailability evaluation =
            EncounterCombatAttackStaging.EvaluateAvailability(
                encounter,
                actor.Combatant.CombatantId,
                target.Combatant.CombatantId,
                weapon.WeaponId);

        return new EncounterCombatTargetOption
        {
            TargetCombatantId = target.Combatant.CombatantId,
            IsAvailable = evaluation.IsLegal,
            UnavailabilityReason = evaluation.UnavailabilityReason,
            AttackRollMode = evaluation.AttackRollMode,
            DistanceFeet = evaluation.DistanceFeet,
            Cover = evaluation.Cover
        };
    }

    /// One spell's whole offer: every other participant it could name as a
    /// target — an ally-only spell and an enemy-only one both offer the same
    /// candidates, and the prerequisite evaluation is what actually tells
    /// each apart. A caster is offered as its own candidate too, since a
    /// spell like Bless can land on the caster alone.
    private static EncounterCombatSpellAttackOption CreateSpellAttackOption(
        EncounterState encounter,
        EncounterParticipantState actor,
        SpellAttack spell)
    {
        EncounterCombatTargetOption[] targets =
            encounter.Participants
                .Select(participant => CreateSpellTargetOption(
                    encounter,
                    actor,
                    participant,
                    spell))
                .ToArray();

        bool hasLegalTarget = targets.Any(target => target.IsAvailable);
        EncounterActionUnavailabilityReason spellReason =
            hasLegalTarget
                ? EncounterActionUnavailabilityReason.None
                : targets.FirstOrDefault()?.UnavailabilityReason
                    ?? EncounterActionUnavailabilityReason.TargetNotParticipant;

        return new EncounterCombatSpellAttackOption
        {
            SpellId = spell.SpellId,
            SpellName = spell.SpellName,
            IsAvailable = hasLegalTarget,
            UnavailabilityReason = spellReason,
            Targets = Array.AsReadOnly(targets),
            TargetCombinations = CreateSpellTargetCombinations(
                spell,
                targets)
        };
    }

    /// See SpellTargetCombinationRules — CombatViewFactory builds the same
    /// options independently for the read-only Query() path, so the
    /// combination algorithm itself lives there rather than here twice.
    private static IReadOnlyList<EncounterCombatTargetCombinationOption>
        CreateSpellTargetCombinations(
            SpellAttack spell,
            IReadOnlyList<EncounterCombatTargetOption> targets)
    {
        string[] legalTargetIds = targets
            .Where(target => target.IsAvailable)
            .Select(target => target.TargetCombatantId)
            .ToArray();

        return SpellTargetCombinationRules
            .Create(spell, legalTargetIds)
            .Select(combination =>
                new EncounterCombatTargetCombinationOption
                {
                    TargetCombatantIds = combination
                })
            .ToArray();
    }

    private static EncounterCombatTargetOption CreateSpellTargetOption(
        EncounterState encounter,
        EncounterParticipantState actor,
        EncounterParticipantState target,
        SpellAttack spell)
    {
        EncounterCombatSpellAttackAvailability evaluation =
            EncounterCombatSpellAttackStaging.EvaluateAvailability(
                encounter,
                actor.Combatant.CombatantId,
                target.Combatant.CombatantId,
                spell.SpellId);

        return new EncounterCombatTargetOption
        {
            TargetCombatantId = target.Combatant.CombatantId,
            IsAvailable = evaluation.IsLegal,
            UnavailabilityReason = evaluation.UnavailabilityReason,
            AttackRollMode = evaluation.AttackRollMode,
            DistanceFeet = evaluation.DistanceFeet,
            Cover = evaluation.Cover,
            SaveAbility = spell.SaveAbility,
            SaveDc = spell.Resolution == SpellResolutionKind.SavingThrow
                ? spell.SaveDc
                : null
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

    /// The one weapon an NPC fights with. Tactics never has to choose between
    /// weapons — nothing authored carries more than one — so this stays exact
    /// rather than falling back to "the first one" if that ever changes.
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

    /// Every weapon a combatant carries, for building the decision a player
    /// actually gets to choose from.
    internal static IReadOnlyList<WeaponAttack> GetWeapons(
        EncounterParticipantState participant)
    {
        return participant.CombatProfile.WeaponAttacks;
    }

    /// One specific weapon, named by ID rather than assumed. For a caller
    /// that already knows which weapon it means — a submitted attack, a
    /// movement search toward a target with a particular weapon in mind — and
    /// needs the mismatch caught rather than silently resolved against
    /// whichever weapon happens to be first.
    internal static WeaponAttack FindWeaponAttack(
        EncounterParticipantState participant,
        string weaponId)
    {
        return participant.CombatProfile.WeaponAttacks
            .SingleOrDefault(weapon => string.Equals(
                weapon.WeaponId,
                weaponId,
                StringComparison.Ordinal))
            ?? throw new ArgumentException(
                $"Weapon '{weaponId}' is not carried by combatant '{participant.Combatant.CombatantId}'.",
                nameof(weaponId));
    }
}

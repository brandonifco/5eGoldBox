using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
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
                State = CombatDecisionState.CombatCompleted,
                EncounterRevision = encounter.Revision,
                ActiveCombatantId = null,
                PendingDeathSavingThrowCombatantId = null,
                Movement = null,
                WeaponAttacks = Array.Empty<WatchtowerCombatWeaponAttackOption>(),
                SpellAttacks = Array.Empty<WatchtowerCombatSpellAttackOption>(),
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
            return new WatchtowerCombatDecision
            {
                State = CombatDecisionState.AutomaticProcessingRequired,
                EncounterRevision = encounter.Revision,
                ActiveCombatantId = encounter.ActiveCombatantId,
                PendingDeathSavingThrowCombatantId =
                    encounter.PendingDeathSavingThrowCombatantId,
                Movement = null,
                WeaponAttacks = Array.Empty<WatchtowerCombatWeaponAttackOption>(),
                SpellAttacks = Array.Empty<WatchtowerCombatSpellAttackOption>(),
                EndTurn = null,
                WinningSideId = null
            };
        }

        WatchtowerCombatWeaponAttackOption[] weaponAttacks =
            GetWeapons(activeParticipant)
                .Select(weapon => CreateWeaponAttackOption(
                    encounter,
                    activeParticipant,
                    weapon))
                .ToArray();

        WatchtowerCombatSpellAttackOption[] spellAttacks =
            activeParticipant.CombatProfile.SpellAttacks
                .Select(spell => CreateSpellAttackOption(
                    encounter,
                    activeParticipant,
                    spell))
                .ToArray();

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
            State = CombatDecisionState.PlayerDecisionRequired,
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
            WeaponAttacks = Array.AsReadOnly(weaponAttacks),
            SpellAttacks = Array.AsReadOnly(spellAttacks),
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

    /// One weapon's whole offer: every opposing combatant it could name as a
    /// target, and whether any of them is actually reachable right now.
    /// Called once per weapon a combatant carries, so a character with a bow
    /// and a dagger is offered both rather than whichever GetFixedWeapon used
    /// to pick for it.
    private static WatchtowerCombatWeaponAttackOption CreateWeaponAttackOption(
        EncounterState encounter,
        EncounterParticipantState actor,
        WeaponAttack weapon)
    {
        WatchtowerCombatTargetOption[] targets =
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

        return new WatchtowerCombatWeaponAttackOption
        {
            WeaponId = weapon.WeaponId,
            IsAvailable = hasLegalTarget,
            UnavailabilityReason = weaponReason,
            Targets = Array.AsReadOnly(targets)
        };
    }

    private static WatchtowerCombatTargetOption CreateTargetOption(
        EncounterState encounter,
        EncounterParticipantState actor,
        EncounterParticipantState target,
        WeaponAttack weapon)
    {
        WatchtowerCombatAttackAvailability evaluation =
            WatchtowerCombatAttackStaging.EvaluateAvailability(
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

    /// One spell's whole offer: every other participant it could name as a
    /// target — an ally-only spell and an enemy-only one both offer the same
    /// candidates, and the prerequisite evaluation is what actually tells
    /// each apart. A caster is offered as its own candidate too, since a
    /// spell like Bless can land on the caster alone.
    private static WatchtowerCombatSpellAttackOption CreateSpellAttackOption(
        EncounterState encounter,
        EncounterParticipantState actor,
        SpellAttack spell)
    {
        WatchtowerCombatTargetOption[] targets =
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

        return new WatchtowerCombatSpellAttackOption
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
    private static IReadOnlyList<WatchtowerCombatTargetCombinationOption>
        CreateSpellTargetCombinations(
            SpellAttack spell,
            IReadOnlyList<WatchtowerCombatTargetOption> targets)
    {
        string[] legalTargetIds = targets
            .Where(target => target.IsAvailable)
            .Select(target => target.TargetCombatantId)
            .ToArray();

        return SpellTargetCombinationRules
            .Create(spell, legalTargetIds)
            .Select(combination =>
                new WatchtowerCombatTargetCombinationOption
                {
                    TargetCombatantIds = combination
                })
            .ToArray();
    }

    private static WatchtowerCombatTargetOption CreateSpellTargetOption(
        EncounterState encounter,
        EncounterParticipantState actor,
        EncounterParticipantState target,
        SpellAttack spell)
    {
        WatchtowerCombatSpellAttackAvailability evaluation =
            WatchtowerCombatSpellAttackStaging.EvaluateAvailability(
                encounter,
                actor.Combatant.CombatantId,
                target.Combatant.CombatantId,
                spell.SpellId);

        return new WatchtowerCombatTargetOption
        {
            TargetCombatantId = target.Combatant.CombatantId,
            IsAvailable = evaluation.IsLegal,
            UnavailabilityReason = evaluation.UnavailabilityReason,
            AttackRollMode = evaluation.AttackRollMode,
            DistanceFeet = evaluation.DistanceFeet,
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

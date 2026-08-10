using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// How an opposing combatant picks a target — one shape for whichever
/// scenario is fighting, decided by what the combatant is rather than who it
/// is.
///
/// Used to be named for the Watchtower raiders and to single one of them out
/// by combatant ID. Two chapel guardians proved that was never really about
/// being a raider: it was always "a melee combatant closes distance and
/// attacks; a ranged one holds position and attacks in range", read off the
/// weapon the combatant already carries.
internal static class EncounterTacticsPolicy
{
    internal static EncounterParticipantState? SelectTarget(
        EncounterState encounter,
        PartyState party,
        EncounterParticipantState raider)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(raider);

        WeaponAttack weapon =
            EncounterCombatDecisionFactory.GetFixedWeapon(raider);

        IEnumerable<EncounterParticipantState> legalTargets =
            GetOpposingTargets(encounter, raider)
                .Where(target => CanAttack(
                    encounter,
                    raider,
                    target,
                    weapon));

        return OrderTargets(
                legalTargets,
                party,
                raider)
            .FirstOrDefault();
    }

    /// With no attackable target, a melee combatant still closes on whoever
    /// it could eventually reach. A ranged one holds position instead.
    internal static EncounterParticipantState? SelectProgressTarget(
        EncounterState encounter,
        PartyState party,
        EncounterParticipantState raider)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(raider);

        WeaponAttack weapon =
            EncounterCombatDecisionFactory.GetFixedWeapon(raider);

        if (weapon.AttackKind != WeaponAttackKind.Melee)
        {
            return null;
        }

        IEnumerable<EncounterParticipantState> progressTargets =
            GetOpposingTargets(encounter, raider)
                .Where(target =>
                    EncounterCombatPathSearch.FindMovement(
                        encounter,
                        raider.Combatant.CombatantId,
                        target.Combatant.CombatantId,
                        weapon.WeaponId)
                    is not null);

        return OrderTargets(
                progressTargets,
                party,
                raider)
            .FirstOrDefault();
    }

    private static bool CanAttack(
        EncounterState encounter,
        EncounterParticipantState raider,
        EncounterParticipantState target,
        WeaponAttack weapon)
    {
        string actorId =
            raider.Combatant.CombatantId;
        string targetId =
            target.Combatant.CombatantId;

        EncounterCombatAttackAvailability prerequisites =
            EncounterCombatAttackStaging.EvaluateAvailability(
                encounter,
                actorId,
                targetId,
                weapon.WeaponId);

        if (prerequisites.IsLegal)
        {
            return true;
        }

        if (weapon.AttackKind != WeaponAttackKind.Melee)
        {
            return false;
        }

        EncounterMovementResult? movement =
            EncounterCombatPathSearch.FindMovement(
                encounter,
                actorId,
                targetId,
                weapon.WeaponId);

        if (movement is null)
        {
            return false;
        }

        return EncounterCombatAttackStaging.EvaluateAvailability(
            movement.State,
            actorId,
            targetId,
            weapon.WeaponId).IsLegal;
    }

    /// Conscious combatants on any side but the actor's own. Correct for
    /// whichever two sides are actually fighting, without needing to know
    /// which one the content calls "the party".
    private static IEnumerable<EncounterParticipantState>
        GetOpposingTargets(
            EncounterState encounter,
            EncounterParticipantState raider)
    {
        return encounter.Participants
            .Where(participant =>
                !string.Equals(
                    participant.SideId,
                    raider.SideId,
                    StringComparison.Ordinal)
                && participant.Combatant.LifecycleState
                    == CombatantLifecycleState.Conscious);
    }

    private static IEnumerable<EncounterParticipantState> OrderTargets(
        IEnumerable<EncounterParticipantState> targets,
        PartyState party,
        EncounterParticipantState raider)
    {
        Dictionary<string, int> partyOrder =
            party.Members
                .Select((member, index) =>
                    new KeyValuePair<string, int>(
                        member.PartyMemberId,
                        index))
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value,
                    StringComparer.Ordinal);

        return targets
            .OrderBy(participant => DistanceFeet(
                raider.Position,
                participant.Position))
            .ThenBy(participant =>
                partyOrder.TryGetValue(
                    participant.Combatant.CombatantId,
                    out int order)
                    ? order
                    : int.MaxValue)
            .ThenBy(
                participant => participant.Combatant.CombatantId,
                StringComparer.Ordinal);
    }

    private static int DistanceFeet(
        GridPosition first,
        GridPosition second)
    {
        return checked(
            Math.Max(
                Math.Abs(first.X - second.X),
                Math.Abs(first.Y - second.Y))
            * 5);
    }
}

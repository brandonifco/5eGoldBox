using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal static class WatchtowerRaiderPolicy
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
            WatchtowerCombatDecisionFactory.GetFixedWeapon(raider);

        IEnumerable<EncounterParticipantState> legalTargets =
            GetConsciousTargets(encounter)
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

    internal static EncounterParticipantState? SelectProgressTarget(
        EncounterState encounter,
        PartyState party,
        EncounterParticipantState raider)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(raider);

        if (!string.Equals(
            raider.Combatant.CombatantId,
            WatchtowerSignalEncounter.MeleeRaiderId,
            StringComparison.Ordinal))
        {
            return null;
        }

        WeaponAttack weapon =
            WatchtowerCombatDecisionFactory.GetFixedWeapon(raider);

        IEnumerable<EncounterParticipantState> progressTargets =
            GetConsciousTargets(encounter)
                .Where(target =>
                    WatchtowerCombatPathSearch.FindMovement(
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

        EncounterWeaponAttackPrerequisiteEvaluation prerequisites =
            EncounterWeaponAttackPrerequisiteRules.Evaluate(
                encounter,
                actorId,
                targetId,
                weapon.WeaponId);

        if (prerequisites.IsLegal)
        {
            return true;
        }

        if (!string.Equals(
            actorId,
            WatchtowerSignalEncounter.MeleeRaiderId,
            StringComparison.Ordinal))
        {
            return false;
        }

        EncounterMovementResult? movement =
            WatchtowerCombatPathSearch.FindMovement(
                encounter,
                actorId,
                targetId,
                weapon.WeaponId);

        if (movement is null)
        {
            return false;
        }

        return EncounterWeaponAttackPrerequisiteRules.Evaluate(
            movement.State,
            actorId,
            targetId,
            weapon.WeaponId).IsLegal;
    }

    private static IEnumerable<EncounterParticipantState>
        GetConsciousTargets(
            EncounterState encounter)
    {
        return encounter.Participants
            .Where(participant =>
                string.Equals(
                    participant.SideId,
                    WatchtowerSignalEncounter.PartySideId,
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

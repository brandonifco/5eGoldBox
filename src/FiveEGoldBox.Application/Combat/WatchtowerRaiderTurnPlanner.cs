using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// Decides how a raider spends its turn. Pure and deterministic: it reads the
/// encounter, defers target choice to WatchtowerRaiderPolicy and reachability to
/// WatchtowerCombatPathSearch, and returns a plan. It never mutates session
/// state, appends steps, or consumes randomness — the orchestrator does that
/// when it applies the plan.
internal static class WatchtowerRaiderTurnPlanner
{
    internal static WatchtowerRaiderTurnPlan Plan(
        EncounterState encounter,
        PartyState party)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(party);

        EncounterParticipantState raider =
            WatchtowerCombatDecisionFactory.FindParticipant(
                encounter,
                encounter.ActiveCombatantId);
        WeaponAttack weapon =
            WatchtowerCombatDecisionFactory.GetFixedWeapon(raider);

        if (weapon.AmmunitionItemId is not null
            && weapon.AmmunitionQuantityAvailable <= 0)
        {
            return Unproductive(movement: null);
        }

        EncounterParticipantState? target =
            WatchtowerRaiderPolicy.SelectTarget(
                encounter,
                party,
                raider);

        if (target is null)
        {
            return Unproductive(
                PlanProgressMovement(
                    encounter,
                    party,
                    raider,
                    weapon));
        }

        string actorId = raider.Combatant.CombatantId;
        string targetId = target.Combatant.CombatantId;

        WatchtowerCombatAttackAvailability prerequisites =
            WatchtowerCombatAttackStaging.EvaluateAvailability(
                encounter,
                actorId,
                targetId,
                weapon.WeaponId);
        EncounterMovementResult? movement = null;

        if (!prerequisites.IsLegal
            && IsMeleeRaider(actorId))
        {
            movement = WatchtowerCombatPathSearch.FindMovement(
                encounter,
                actorId,
                targetId,
                weapon.WeaponId);

            if (movement is not null)
            {
                prerequisites =
                    WatchtowerCombatAttackStaging.EvaluateAvailability(
                        movement.State,
                        actorId,
                        targetId,
                        weapon.WeaponId);
            }
        }

        if (!prerequisites.IsLegal)
        {
            return Unproductive(movement);
        }

        return new WatchtowerRaiderTurnPlan
        {
            Movement = movement,
            Attack = new WatchtowerRaiderAttackPlan
            {
                TargetCombatantId = targetId,
                WeaponId = weapon.WeaponId
            },
            TurnAdvanceReason =
                WatchtowerCombatTurnAdvanceReason.RaiderTurnCompleted
        };
    }

    /// With no attackable target the melee raider still closes on whoever it
    /// could eventually reach. The ranged raider holds position.
    private static EncounterMovementResult? PlanProgressMovement(
        EncounterState encounter,
        PartyState party,
        EncounterParticipantState raider,
        WeaponAttack weapon)
    {
        if (!IsMeleeRaider(raider.Combatant.CombatantId))
        {
            return null;
        }

        EncounterParticipantState? progressTarget =
            WatchtowerRaiderPolicy.SelectProgressTarget(
                encounter,
                party,
                raider);

        return progressTarget is null
            ? null
            : WatchtowerCombatPathSearch.FindMovement(
                encounter,
                raider.Combatant.CombatantId,
                progressTarget.Combatant.CombatantId,
                weapon.WeaponId);
    }

    private static WatchtowerRaiderTurnPlan Unproductive(
        EncounterMovementResult? movement)
    {
        return new WatchtowerRaiderTurnPlan
        {
            Movement = movement,
            Attack = null,
            TurnAdvanceReason =
                WatchtowerCombatTurnAdvanceReason.NoProductiveEnemyAction
        };
    }

    private static bool IsMeleeRaider(
        string combatantId)
    {
        return string.Equals(
            combatantId,
            WatchtowerSignalEncounter.MeleeRaiderId,
            StringComparison.Ordinal);
    }
}

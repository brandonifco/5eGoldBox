using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal static class WatchtowerCombatPathSearch
{
    private const int FeetPerStep = 5;

    internal static IReadOnlyList<EncounterMovementResult>
        EnumerateReachableMovements(
            EncounterState state,
            string actorCombatantId)
    {
        return CombatPathSearch.EnumerateReachableMovements(
            state,
            actorCombatantId);
    }

    internal static EncounterMovementResult? FindMovement(
        EncounterState state,
        string actorCombatantId,
        string targetCombatantId,
        string weaponId)
    {
        ArgumentNullException.ThrowIfNull(state);

        EncounterParticipantState actor =
            WatchtowerCombatDecisionFactory.FindParticipant(
                state,
                actorCombatantId);
        EncounterParticipantState target =
            WatchtowerCombatDecisionFactory.FindParticipant(
                state,
                targetCombatantId);
        WeaponAttack weapon =
            WatchtowerCombatDecisionFactory.GetFixedWeapon(actor);

        if (!string.Equals(
            weapon.WeaponId,
            weaponId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Weapon '{weaponId}' is not the authored weapon for actor '{actorCombatantId}'.",
                nameof(weaponId));
        }

        List<EncounterMovementResult> attackEnabled = [];
        List<EncounterMovementResult> progress = [];
        int startingTargetDistance = DistanceFeet(
            actor.Position,
            target.Position);

        foreach (EncounterMovementResult movement
            in EnumerateReachableMovements(
                state,
                actorCombatantId))
        {
            WatchtowerCombatAttackAvailability prerequisites =
                WatchtowerCombatAttackStaging.EvaluateAvailability(
                    movement.State,
                    actorCombatantId,
                    targetCombatantId,
                    weaponId);

            if (prerequisites.IsLegal)
            {
                attackEnabled.Add(movement);
            }
            else if (DistanceFeet(
                movement.EndingPosition,
                target.Position) < startingTargetDistance)
            {
                progress.Add(movement);
            }
        }

        IEnumerable<EncounterMovementResult> candidates =
            attackEnabled.Count > 0
                ? attackEnabled
                    .OrderBy(result => result.MovementSpentFeet)
                    .ThenBy(result => DistanceFeet(
                        result.EndingPosition,
                        target.Position))
                    .ThenBy(result => result.EndingPosition.Y)
                    .ThenBy(result => result.EndingPosition.X)
                : progress
                    .OrderBy(result => DistanceFeet(
                        result.EndingPosition,
                        target.Position))
                    .ThenBy(result => result.EndingPosition.Y)
                    .ThenBy(result => result.EndingPosition.X);

        return candidates.FirstOrDefault();
    }

    private static int DistanceFeet(
        GridPosition first,
        GridPosition second)
    {
        return checked(
            Math.Max(
                Math.Abs(first.X - second.X),
                Math.Abs(first.Y - second.Y))
            * FeetPerStep);
    }
}

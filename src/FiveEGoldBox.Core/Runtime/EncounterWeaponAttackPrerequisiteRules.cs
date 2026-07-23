using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

internal static class EncounterWeaponAttackPrerequisiteRules
{
    private const int FeetPerGridSquare = 5;
    private const int DefaultMeleeReachFeet = 5;

    public static EncounterWeaponAttackPrerequisiteEvaluation Evaluate(
        EncounterState state,
        string actorCombatantId,
        string targetCombatantId,
        string weaponId)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (string.IsNullOrWhiteSpace(actorCombatantId))
        {
            throw new ArgumentException(
                "Actor combatant ID is required.",
                nameof(actorCombatantId));
        }

        if (string.IsNullOrWhiteSpace(targetCombatantId))
        {
            throw new ArgumentException(
                "Target combatant ID is required.",
                nameof(targetCombatantId));
        }

        if (string.IsNullOrWhiteSpace(weaponId))
        {
            throw new ArgumentException(
                "Weapon ID is required.",
                nameof(weaponId));
        }

        EncounterRules.ValidateState(state);

        if (state.LifecycleState
            != EncounterLifecycleState.Active)
        {
            return CreateUnavailable(
                EncounterActionUnavailabilityReason
                    .EncounterCompleted);
        }

        if (string.Equals(
            actorCombatantId,
            targetCombatantId,
            StringComparison.Ordinal))
        {
            return CreateUnavailable(
                EncounterActionUnavailabilityReason
                    .SelfTargetNotAllowed);
        }

        EncounterParticipantState? actor =
            FindParticipant(
                state,
                actorCombatantId);

        if (actor is null)
        {
            return CreateUnavailable(
                EncounterActionUnavailabilityReason
                    .ActorNotParticipant);
        }

        EncounterParticipantState? target =
            FindParticipant(
                state,
                targetCombatantId);

        if (target is null)
        {
            return CreateUnavailable(
                EncounterActionUnavailabilityReason
                    .TargetNotParticipant);
        }

        if (!string.Equals(
            actorCombatantId,
            state.ActiveCombatantId,
            StringComparison.Ordinal))
        {
            return CreateUnavailable(
                EncounterActionUnavailabilityReason
                    .ActorNotActive);
        }

        if (actor.Combatant.LifecycleState
            != CombatantLifecycleState.Conscious)
        {
            return CreateUnavailable(
                EncounterActionUnavailabilityReason
                    .ActorCannotAct);
        }

        if (!actor.TurnResources.HasActionAvailable)
        {
            return CreateUnavailable(
                EncounterActionUnavailabilityReason
                    .ActionUnavailable);
        }

        if (string.Equals(
            actor.SideId,
            target.SideId,
            StringComparison.Ordinal))
        {
            return CreateUnavailable(
                EncounterActionUnavailabilityReason
                    .TargetNotHostile);
        }

        if (target.Combatant.IsTerminal)
        {
            return CreateUnavailable(
                EncounterActionUnavailabilityReason
                    .TargetCannotBeAttacked);
        }

        WeaponAttack? weapon =
            FindWeaponAttack(
                actor,
                weaponId);

        if (weapon is null)
        {
            return CreateUnavailable(
                EncounterActionUnavailabilityReason
                    .WeaponUnavailable);
        }

        ValidateWeaponAttack(weapon);

        int distanceFeet =
            CalculateDistanceFeet(
                actor.Position,
                target.Position);

        EncounterLineOfSightResult lineOfSight =
            EncounterLineOfSightRules.Evaluate(
                state.Battlefield,
                actor.Position,
                target.Position);

        if (!lineOfSight.HasLineOfSight)
        {
            return CreateUnavailable(
                EncounterActionUnavailabilityReason
                    .LineOfSightBlocked,
                distanceFeet,
                lineOfSight);
        }

        EncounterCoverEvaluation cover =
            EncounterCoverRules.Evaluate(
                state.Battlefield,
                lineOfSight);

        D20RollMode? attackRollMode =
            ResolveAttackRollMode(
                state,
                actor,
                weapon,
                distanceFeet);

        if (attackRollMode is null)
        {
            return CreateUnavailable(
                EncounterActionUnavailabilityReason
                    .TargetOutOfRange,
                distanceFeet,
                lineOfSight);
        }

        if (!HasAmmunitionAvailable(weapon))
        {
            return CreateUnavailable(
                EncounterActionUnavailabilityReason
                    .AmmunitionUnavailable,
                distanceFeet,
                lineOfSight);
        }

        return new EncounterWeaponAttackPrerequisiteEvaluation
        {
            IsLegal = true,
            UnavailabilityReason =
                EncounterActionUnavailabilityReason.None,
            AttackRollMode = attackRollMode,
            DistanceFeet = distanceFeet,
            LineOfSight = lineOfSight,
            Cover = cover
        };
    }

    private static EncounterParticipantState? FindParticipant(
        EncounterState state,
        string combatantId)
    {
        return state.Participants.FirstOrDefault(
            participant =>
                string.Equals(
                    participant.Combatant.CombatantId,
                    combatantId,
                    StringComparison.Ordinal));
    }

    private static WeaponAttack? FindWeaponAttack(
        EncounterParticipantState actor,
        string weaponId)
    {
        List<WeaponAttack> matches = [];

        foreach (WeaponAttack weapon
            in actor.CombatProfile.WeaponAttacks)
        {
            ArgumentNullException.ThrowIfNull(weapon);

            if (string.Equals(
                weapon.WeaponId,
                weaponId,
                StringComparison.Ordinal))
            {
                matches.Add(weapon);
            }
        }

        if (matches.Count > 1)
        {
            throw new ArgumentException(
                $"Actor '{actor.Combatant.CombatantId}' has duplicate weapon attack ID '{weaponId}'.",
                nameof(weaponId));
        }

        return matches.Count == 0
            ? null
            : matches[0];
    }

    private static void ValidateWeaponAttack(
        WeaponAttack weapon)
    {
        if (string.IsNullOrWhiteSpace(
            weapon.WeaponId))
        {
            throw new ArgumentException(
                "Weapon attack ID is required.",
                nameof(weapon));
        }

        if (!Enum.IsDefined(weapon.AttackKind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(weapon),
                weapon.AttackKind,
                "Unsupported weapon attack kind.");
        }

        if (!Enum.IsDefined(weapon.AttackRollMode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(weapon),
                weapon.AttackRollMode,
                "Unsupported attack roll mode.");
        }

        ArgumentNullException.ThrowIfNull(
            weapon.Damage);

        if (weapon.Damage.Count < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weapon),
                weapon.Damage.Count,
                "Weapon damage dice count must be at least 1.");
        }

        if (!Enum.IsDefined(weapon.Damage.Die))
        {
            throw new ArgumentOutOfRangeException(
                nameof(weapon),
                weapon.Damage.Die,
                "Unsupported weapon damage die.");
        }

        if (string.IsNullOrWhiteSpace(
            weapon.DamageType))
        {
            throw new ArgumentException(
                "Weapon damage type is required.",
                nameof(weapon));
        }

        if (weapon.ReachFeet is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weapon),
                weapon.ReachFeet,
                "Weapon reach must be greater than 0.");
        }

        if (weapon.AttackKind
            != WeaponAttackKind.Ranged)
        {
            return;
        }

        if (weapon.NormalRangeFeet is null
            or <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weapon),
                weapon.NormalRangeFeet,
                "A ranged weapon's normal range must be greater than 0.");
        }

        if (weapon.LongRangeFeet is null
            or <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weapon),
                weapon.LongRangeFeet,
                "A ranged weapon's long range must be greater than 0.");
        }

        if (weapon.LongRangeFeet
            < weapon.NormalRangeFeet)
        {
            throw new ArgumentException(
                "A ranged weapon's long range cannot be shorter than its normal range.",
                nameof(weapon));
        }

        bool hasAmmunitionItemId =
            weapon.AmmunitionItemId is not null;

        bool hasAmmunitionQuantity =
            weapon.AmmunitionQuantityAvailable
                is not null;

        if (hasAmmunitionItemId
            && string.IsNullOrWhiteSpace(
                weapon.AmmunitionItemId))
        {
            throw new ArgumentException(
                "Ammunition item ID cannot be blank.",
                nameof(weapon));
        }

        if (hasAmmunitionItemId
            != hasAmmunitionQuantity)
        {
            throw new ArgumentException(
                "A ranged weapon must provide both an ammunition item ID and an available quantity, or neither.",
                nameof(weapon));
        }

        if (weapon.AmmunitionQuantityAvailable
            is < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weapon),
                weapon.AmmunitionQuantityAvailable,
                "Available ammunition quantity cannot be negative.");
        }
    }

    private static D20RollMode? ResolveAttackRollMode(
        EncounterState state,
        EncounterParticipantState actor,
        WeaponAttack weapon,
        int distanceFeet)
    {
        bool isLongRange = false;

        if (weapon.AttackKind
            == WeaponAttackKind.Melee)
        {
            int reachFeet =
                weapon.ReachFeet
                ?? DefaultMeleeReachFeet;

            if (distanceFeet > reachFeet)
            {
                return null;
            }
        }
        else
        {
            int normalRangeFeet =
                weapon.NormalRangeFeet!.Value;

            int longRangeFeet =
                weapon.LongRangeFeet!.Value;

            if (distanceFeet > longRangeFeet)
            {
                return null;
            }

            isLongRange =
                distanceFeet > normalRangeFeet;
        }

        bool hasAdvantage =
            weapon.AttackRollMode
            == D20RollMode.Advantage;

        bool hasDisadvantage =
            weapon.AttackRollMode
                == D20RollMode.Disadvantage
            || isLongRange
            || HasAdjacentConsciousHostile(
                state,
                actor,
                weapon);

        return D20Rules.ResolveRollMode(
            hasAdvantage,
            hasDisadvantage);
    }

    private static bool HasAdjacentConsciousHostile(
        EncounterState state,
        EncounterParticipantState actor,
        WeaponAttack weapon)
    {
        if (weapon.AttackKind
            != WeaponAttackKind.Ranged)
        {
            return false;
        }

        foreach (EncounterParticipantState participant
            in state.Participants)
        {
            if (string.Equals(
                participant.SideId,
                actor.SideId,
                StringComparison.Ordinal)
                || participant.Combatant.LifecycleState
                    != CombatantLifecycleState.Conscious
                || CalculateDistanceFeet(
                    participant.Position,
                    actor.Position) > 5)
            {
                continue;
            }

            EncounterLineOfSightResult lineOfSight =
                EncounterLineOfSightRules.Evaluate(
                    state.Battlefield,
                    participant.Position,
                    actor.Position);

            if (lineOfSight.HasLineOfSight)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAmmunitionAvailable(
        WeaponAttack weapon)
    {
        return weapon.AttackKind
                != WeaponAttackKind.Ranged
            || weapon.AmmunitionItemId is null
            || weapon.AmmunitionQuantityAvailable
                > 0;
    }

    private static int CalculateDistanceFeet(
        GridPosition first,
        GridPosition second)
    {
        int horizontalSquares =
            Math.Abs(first.X - second.X);

        int verticalSquares =
            Math.Abs(first.Y - second.Y);

        int distanceSquares =
            Math.Max(
                horizontalSquares,
                verticalSquares);

        return checked(
            distanceSquares * FeetPerGridSquare);
    }

    private static EncounterWeaponAttackPrerequisiteEvaluation
        CreateUnavailable(
            EncounterActionUnavailabilityReason reason,
            int? distanceFeet = null,
            EncounterLineOfSightResult? lineOfSight = null)
    {
        return new EncounterWeaponAttackPrerequisiteEvaluation
        {
            IsLegal = false,
            UnavailabilityReason = reason,
            AttackRollMode = null,
            DistanceFeet = distanceFeet,
            LineOfSight = lineOfSight
        };
    }
}

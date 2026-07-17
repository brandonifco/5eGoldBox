using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public static class EncounterWeaponAttackRules
{
    private const int FeetPerGridSquare = 5;
    private const int DefaultMeleeReachFeet = 5;

    public static EncounterWeaponAttackResult Resolve(
        EncounterState state,
        EncounterWeaponAttackCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        EncounterRules.ValidateState(state);
        ValidateCommand(command);

        if (state.LifecycleState
            != EncounterLifecycleState.Active)
        {
            throw new InvalidOperationException(
                "A completed encounter cannot resolve an attack.");
        }

        if (command.ExpectedRevision != state.Revision)
        {
            throw new InvalidOperationException(
                $"Expected encounter revision '{command.ExpectedRevision}', but the current revision is '{state.Revision}'.");
        }

        if (command.ActorCombatantId
            == command.TargetCombatantId)
        {
            throw new ArgumentException(
                "An attacker cannot target itself.",
                nameof(command));
        }

        int actorIndex = FindParticipantIndex(
            state,
            command.ActorCombatantId);

        if (actorIndex < 0)
        {
            throw new ArgumentException(
                $"Actor '{command.ActorCombatantId}' is not an encounter participant.",
                nameof(command));
        }

        int targetIndex = FindParticipantIndex(
            state,
            command.TargetCombatantId);

        if (targetIndex < 0)
        {
            throw new ArgumentException(
                $"Target '{command.TargetCombatantId}' is not an encounter participant.",
                nameof(command));
        }

        EncounterParticipantState actor =
            state.Participants[actorIndex];

        EncounterParticipantState target =
            state.Participants[targetIndex];

        ValidateActor(state, actor);

        if (actor.SideId == target.SideId)
        {
            throw new InvalidOperationException(
                "A basic weapon attack must target an opposing participant.");
        }

        if (target.Combatant.IsTerminal)
        {
            throw new InvalidOperationException(
                "A terminal combatant cannot be targeted by a basic weapon attack.");
        }

        WeaponAttack weapon = FindWeaponAttack(
            actor,
            command.WeaponId);

        ValidateWeaponAttack(weapon);

        if (weapon.AttackKind
            != WeaponAttackKind.Melee)
        {
            throw new InvalidOperationException(
                "This transition currently supports only melee weapon attacks.");
        }

        int distanceFeet = CalculateDistanceFeet(
            actor.Position,
            target.Position);

        int reachFeet =
            weapon.ReachFeet
            ?? DefaultMeleeReachFeet;

        if (distanceFeet > reachFeet)
        {
            throw new InvalidOperationException(
                $"Target is {distanceFeet} feet away, beyond the weapon's {reachFeet}-foot reach.");
        }

        long resolvedRevision =
            checked(state.Revision + 1);

        IReadOnlyList<int> protectedDamageRolls =
            Array.AsReadOnly(
                command.DamageRolls.ToArray());

        AttackRollResult attackRoll =
            AttackRollRules.ResolveResult(
                weapon.AttackRollMode,
                command.FirstAttackRoll,
                command.SecondAttackRoll,
                weapon.AttackBonus,
                target.CombatProfile.ArmorClass);

        IReadOnlyList<DamageResponseType>
            responseTypes =
                GetDamageResponseTypes(
                    target,
                    weapon.DamageType);

        AttackDamageResolutionResult attackDamage =
            DamageRules.ResolveAttackDamage(
                weapon.Damage,
                attackRoll.Outcome,
                protectedDamageRolls,
                weapon.DamageBonus,
                responseTypes);

        AttackResolutionResult attack = new()
        {
            AttackRoll = attackRoll,
            Damage = attackDamage
        };

        EncounterParticipantState[] participants =
            state.Participants.ToArray();

        participants[actorIndex] = actor with
        {
            TurnResources =
                CombatTurnResourceRules.SpendAction(
                    actor.TurnResources)
        };

        EncounterState actionSpentState = state with
        {
            Participants =
                Array.AsReadOnly(participants)
        };

        CombatantDamageResult? targetDamage = null;

        EncounterState resolvedState;

        if (attackRoll.Outcome
            == AttackRollOutcome.Miss)
        {
            resolvedState = actionSpentState with
            {
                Revision = resolvedRevision
            };
        }
        else
        {
            EncounterDamageResult damageResult =
                EncounterDamageRules.Resolve(
                    actionSpentState,
                    new EncounterDamageCommand
                    {
                        ExpectedRevision =
                            command.ExpectedRevision,
                        TargetCombatantId =
                            command.TargetCombatantId,
                        DamageAmount =
                            attackDamage.FinalDamage,
                        IsCriticalHit =
                            attackRoll.Outcome
                                == AttackRollOutcome.CriticalHit
                    });

            targetDamage =
                damageResult.CombatantDamage;

            resolvedState =
                damageResult.State;
        }

        EncounterRules.ValidateState(resolvedState);

        return new EncounterWeaponAttackResult
        {
            ActorCombatantId =
                command.ActorCombatantId,
            TargetCombatantId =
                command.TargetCombatantId,
            WeaponId = command.WeaponId,
            DistanceFeet = distanceFeet,
            Attack = attack,
            TargetDamage = targetDamage,
            State = resolvedState
        };
    }

    private static void ValidateCommand(
        EncounterWeaponAttackCommand command)
    {
        if (command.ExpectedRevision < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                command.ExpectedRevision,
                "Expected encounter revision must be at least 1.");
        }

        if (string.IsNullOrWhiteSpace(
            command.ActorCombatantId))
        {
            throw new ArgumentException(
                "Actor combatant ID is required.",
                nameof(command));
        }

        if (string.IsNullOrWhiteSpace(
            command.TargetCombatantId))
        {
            throw new ArgumentException(
                "Target combatant ID is required.",
                nameof(command));
        }

        if (string.IsNullOrWhiteSpace(
            command.WeaponId))
        {
            throw new ArgumentException(
                "Weapon ID is required.",
                nameof(command));
        }

        ArgumentNullException.ThrowIfNull(
            command.DamageRolls);
    }

    private static void ValidateActor(
        EncounterState state,
        EncounterParticipantState actor)
    {
        if (actor.Combatant.CombatantId
            != state.ActiveCombatantId)
        {
            throw new InvalidOperationException(
                "Only the active combatant can make a basic weapon attack.");
        }

        if (actor.Combatant.LifecycleState
            != CombatantLifecycleState.Conscious)
        {
            throw new InvalidOperationException(
                "The attacking combatant must be conscious.");
        }

        if (!actor.TurnResources.HasActionAvailable)
        {
            throw new InvalidOperationException(
                "The attacking combatant has already spent its action.");
        }
    }

    private static WeaponAttack FindWeaponAttack(
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

        if (matches.Count == 0)
        {
            throw new ArgumentException(
                $"Weapon '{weaponId}' is not available to actor '{actor.Combatant.CombatantId}'.",
                nameof(weaponId));
        }

        if (matches.Count > 1)
        {
            throw new ArgumentException(
                $"Actor '{actor.Combatant.CombatantId}' has duplicate weapon attack ID '{weaponId}'.",
                nameof(weaponId));
        }

        return matches[0];
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
    }

    private static IReadOnlyList<DamageResponseType>
        GetDamageResponseTypes(
            EncounterParticipantState target,
            string damageType)
    {
        List<DamageResponseType> responseTypes = [];

        foreach (CharacterDamageResponse response
            in target.CombatProfile.DamageResponses)
        {
            ArgumentNullException.ThrowIfNull(response);

            if (string.IsNullOrWhiteSpace(
                response.DamageType))
            {
                throw new ArgumentException(
                    "Damage-response damage type is required.",
                    nameof(target));
            }

            if (!Enum.IsDefined(response.ResponseType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(target),
                    response.ResponseType,
                    "Unsupported damage response type.");
            }

            if (string.Equals(
                response.DamageType,
                damageType,
                StringComparison.Ordinal))
            {
                responseTypes.Add(
                    response.ResponseType);
            }
        }

        return Array.AsReadOnly(
            responseTypes.ToArray());
    }

    private static int FindParticipantIndex(
        EncounterState state,
        string combatantId)
    {
        for (int index = 0;
            index < state.Participants.Count;
            index++)
        {
            if (string.Equals(
                state.Participants[index]
                    .Combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
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
}

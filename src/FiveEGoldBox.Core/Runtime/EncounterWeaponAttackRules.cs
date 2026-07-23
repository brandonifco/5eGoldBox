using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public static class EncounterWeaponAttackRules
{
    private const int DefaultMeleeReachFeet = 5;

    internal static EncounterWeaponAttackEvaluation Evaluate(
        EncounterState state,
        EncounterWeaponAttackEvaluationCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        EncounterRules.ValidateState(state);
        ValidateEvaluationCommand(command);

        return EvaluateAttack(
            state,
            command.ExpectedRevision,
            command.ActorCombatantId,
            command.TargetCombatantId,
            command.WeaponId,
            command.FirstAttackRoll,
            command.SecondAttackRoll);
    }

    public static EncounterWeaponAttackResult Resolve(
        EncounterState state,
        EncounterWeaponAttackCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        EncounterRules.ValidateState(state);
        ValidateCommand(command);

        EncounterWeaponAttackEvaluation evaluation =
            EvaluateAttack(
                state,
                command.ExpectedRevision,
                command.ActorCombatantId,
                command.TargetCombatantId,
                command.WeaponId,
                command.FirstAttackRoll,
                command.SecondAttackRoll);

        int actorIndex = FindParticipantIndex(
            state,
            command.ActorCombatantId);

        int targetIndex = FindParticipantIndex(
            state,
            command.TargetCombatantId);

        EncounterParticipantState actor =
            state.Participants[actorIndex];

        EncounterParticipantState target =
            state.Participants[targetIndex];

        WeaponAttack weapon = FindWeaponAttack(
            actor,
            command.WeaponId);

        long resolvedRevision =
            checked(state.Revision + 1);

        IReadOnlyList<int> protectedDamageRolls =
            Array.AsReadOnly(
                command.DamageRolls.ToArray());

        IReadOnlyList<DamageResponseType>
            responseTypes =
                GetDamageResponseTypes(
                    target,
                    weapon.DamageType);

        AttackDamageResolutionResult attackDamage =
            DamageRules.ResolveAttackDamage(
                weapon.Damage,
                evaluation.AttackRoll.Outcome,
                protectedDamageRolls,
                weapon.DamageBonus,
                responseTypes);

        AttackResolutionResult attack = new()
        {
            AttackRoll = evaluation.AttackRoll,
            Damage = attackDamage
        };

        EncounterParticipantState[] participants =
            state.Participants.ToArray();

        participants[actorIndex] =
            PrepareActorAfterAttack(
                actor,
                weapon);

        EncounterState actionSpentState = state with
        {
            Participants =
                Array.AsReadOnly(participants)
        };

        CombatantDamageResult? targetDamage = null;

        EncounterState resolvedState;

        if (evaluation.AttackRoll.Outcome
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
                            evaluation.AttackRoll.Outcome
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
            DistanceFeet =
                evaluation.Prerequisites
                    .DistanceFeet!.Value,
            LineOfSight =
                evaluation.Prerequisites.LineOfSight!,
            Cover = evaluation.Prerequisites.Cover,
            Attack = attack,
            TargetDamage = targetDamage,
            State = resolvedState
        };
    }

    private static EncounterWeaponAttackEvaluation
        EvaluateAttack(
            EncounterState state,
            long expectedRevision,
            string actorCombatantId,
            string targetCombatantId,
            string weaponId,
            int firstAttackRoll,
            int? secondAttackRoll)
    {
        if (state.LifecycleState
            != EncounterLifecycleState.Active)
        {
            throw new InvalidOperationException(
                "A completed encounter cannot evaluate an attack.");
        }

        if (expectedRevision != state.Revision)
        {
            throw new InvalidOperationException(
                $"Expected encounter revision '{expectedRevision}', but the current revision is '{state.Revision}'.");
        }

        EncounterWeaponAttackPrerequisiteEvaluation prerequisites =
            EncounterWeaponAttackPrerequisiteRules.Evaluate(
                state,
                actorCombatantId,
                targetCombatantId,
                weaponId);

        EnsurePrerequisitesAreLegal(
            state,
            actorCombatantId,
            targetCombatantId,
            weaponId,
            prerequisites);

        int actorIndex = FindParticipantIndex(
            state,
            actorCombatantId);

        int targetIndex = FindParticipantIndex(
            state,
            targetCombatantId);

        EncounterParticipantState actor =
            state.Participants[actorIndex];

        EncounterParticipantState target =
            state.Participants[targetIndex];

        WeaponAttack weapon = FindWeaponAttack(
            actor,
            weaponId);

        int targetArmorClass =
            checked(
                target.CombatProfile.ArmorClass
                + prerequisites.Cover.ArmorClassBonus);

        AttackRollResult attackRoll =
            AttackRollRules.ResolveResult(
                prerequisites.AttackRollMode!.Value,
                firstAttackRoll,
                secondAttackRoll,
                weapon.AttackBonus,
                targetArmorClass);

        DamageDice? requiredDamageDice =
            DamageRules.GetDamageDiceForAttackOutcome(
                weapon.Damage,
                attackRoll.Outcome);

        return new EncounterWeaponAttackEvaluation
        {
            EncounterRevision = state.Revision,
            ActorCombatantId = actorCombatantId,
            TargetCombatantId = targetCombatantId,
            WeaponId = weaponId,
            Prerequisites = prerequisites,
            AttackRoll = attackRoll,
            RequiredDamageDice = requiredDamageDice
        };
    }

    private static void EnsurePrerequisitesAreLegal(
        EncounterState state,
        string actorCombatantId,
        string targetCombatantId,
        string weaponId,
        EncounterWeaponAttackPrerequisiteEvaluation prerequisites)
    {
        if (prerequisites.IsLegal)
        {
            return;
        }

        switch (prerequisites.UnavailabilityReason)
        {
            case EncounterActionUnavailabilityReason
                .EncounterCompleted:
                throw new InvalidOperationException(
                    "A completed encounter cannot resolve an attack.");

            case EncounterActionUnavailabilityReason
                .SelfTargetNotAllowed:
                throw new ArgumentException(
                    "An attacker cannot target itself.",
                    nameof(targetCombatantId));

            case EncounterActionUnavailabilityReason
                .ActorNotParticipant:
                throw new ArgumentException(
                    $"Actor '{actorCombatantId}' is not an encounter participant.",
                    nameof(actorCombatantId));

            case EncounterActionUnavailabilityReason
                .TargetNotParticipant:
                throw new ArgumentException(
                    $"Target '{targetCombatantId}' is not an encounter participant.",
                    nameof(targetCombatantId));

            case EncounterActionUnavailabilityReason
                .ActorNotActive:
                throw new InvalidOperationException(
                    "Only the active combatant can make a basic weapon attack.");

            case EncounterActionUnavailabilityReason
                .ActorCannotAct:
                throw new InvalidOperationException(
                    "The attacking combatant must be conscious.");

            case EncounterActionUnavailabilityReason
                .ActionUnavailable:
                throw new InvalidOperationException(
                    "The attacking combatant has already spent its action.");

            case EncounterActionUnavailabilityReason
                .TargetNotHostile:
                throw new InvalidOperationException(
                    "A basic weapon attack must target an opposing participant.");

            case EncounterActionUnavailabilityReason
                .TargetCannotBeAttacked:
                throw new InvalidOperationException(
                    "A terminal combatant cannot be targeted by a basic weapon attack.");

            case EncounterActionUnavailabilityReason
                .WeaponUnavailable:
                throw new ArgumentException(
                    $"Weapon '{weaponId}' is not available to actor '{actorCombatantId}'.",
                    nameof(weaponId));

            case EncounterActionUnavailabilityReason
                .TargetOutOfRange:
                ThrowTargetOutOfRange(
                    state,
                    actorCombatantId,
                    weaponId,
                    prerequisites);
                break;

            case EncounterActionUnavailabilityReason
                .LineOfSightBlocked:
                throw new InvalidOperationException(
                    $"Target does not have line of sight from the attacker because position '{prerequisites.LineOfSight!.BlockingPosition}' blocks the path.");

            case EncounterActionUnavailabilityReason
                .AmmunitionUnavailable:
                throw new InvalidOperationException(
                    $"Weapon '{weaponId}' has no available ammunition.");

            default:
                throw new InvalidOperationException(
                    $"Weapon attack prerequisites failed for unsupported reason '{prerequisites.UnavailabilityReason}'.");
        }
    }

    private static void ThrowTargetOutOfRange(
        EncounterState state,
        string actorCombatantId,
        string weaponId,
        EncounterWeaponAttackPrerequisiteEvaluation prerequisites)
    {
        int actorIndex = FindParticipantIndex(
            state,
            actorCombatantId);

        EncounterParticipantState actor =
            state.Participants[actorIndex];

        WeaponAttack weapon = FindWeaponAttack(
            actor,
            weaponId);

        int distanceFeet =
            prerequisites.DistanceFeet!.Value;

        if (weapon.AttackKind
            == WeaponAttackKind.Melee)
        {
            int reachFeet =
                weapon.ReachFeet
                ?? DefaultMeleeReachFeet;

            throw new InvalidOperationException(
                $"Target is {distanceFeet} feet away, beyond the weapon's {reachFeet}-foot reach.");
        }

        throw new InvalidOperationException(
            $"Target is {distanceFeet} feet away, beyond the weapon's {weapon.LongRangeFeet!.Value}-foot long range.");
    }

    private static void ValidateEvaluationCommand(
        EncounterWeaponAttackEvaluationCommand command)
    {
        ValidateCommonCommand(
            command.ExpectedRevision,
            command.ActorCombatantId,
            command.TargetCombatantId,
            command.WeaponId);
    }

    private static void ValidateCommand(
        EncounterWeaponAttackCommand command)
    {
        ValidateCommonCommand(
            command.ExpectedRevision,
            command.ActorCombatantId,
            command.TargetCombatantId,
            command.WeaponId);

        ArgumentNullException.ThrowIfNull(
            command.DamageRolls);
    }

    private static void ValidateCommonCommand(
        long expectedRevision,
        string actorCombatantId,
        string targetCombatantId,
        string weaponId)
    {
        if (expectedRevision < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expectedRevision),
                expectedRevision,
                "Expected encounter revision must be at least 1.");
        }

        if (string.IsNullOrWhiteSpace(
            actorCombatantId))
        {
            throw new ArgumentException(
                "Actor combatant ID is required.",
                nameof(actorCombatantId));
        }

        if (string.IsNullOrWhiteSpace(
            targetCombatantId))
        {
            throw new ArgumentException(
                "Target combatant ID is required.",
                nameof(targetCombatantId));
        }

        if (string.IsNullOrWhiteSpace(
            weaponId))
        {
            throw new ArgumentException(
                "Weapon ID is required.",
                nameof(weaponId));
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

    private static EncounterParticipantState
        PrepareActorAfterAttack(
            EncounterParticipantState actor,
            WeaponAttack weapon)
    {
        EncounterCombatProfile resolvedCombatProfile =
            actor.CombatProfile;

        if (weapon.AttackKind
                == WeaponAttackKind.Ranged
            && weapon.AmmunitionItemId is not null)
        {
            WeaponAttack[] weaponAttacks =
                actor.CombatProfile.WeaponAttacks
                    .ToArray();

            int weaponIndex = Array.FindIndex(
                weaponAttacks,
                candidate => string.Equals(
                    candidate.WeaponId,
                    weapon.WeaponId,
                    StringComparison.Ordinal));

            if (weaponIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Weapon '{weapon.WeaponId}' could not be updated after the attack.");
            }

            weaponAttacks[weaponIndex] =
                weapon with
                {
                    AmmunitionQuantityAvailable =
                        weapon
                            .AmmunitionQuantityAvailable!
                            .Value - 1
                };

            resolvedCombatProfile =
                actor.CombatProfile with
                {
                    WeaponAttacks =
                        Array.AsReadOnly(
                            weaponAttacks)
                };
        }

        return actor with
        {
            CombatProfile =
                resolvedCombatProfile,
            TurnResources =
                CombatTurnResourceRules.SpendAction(
                    actor.TurnResources)
        };
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
}

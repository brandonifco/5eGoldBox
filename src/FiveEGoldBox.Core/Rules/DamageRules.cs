using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Internal;

namespace FiveEGoldBox.Core.Rules;

public static class DamageRules
{
    public static int ApplyDamageResponse(
        int damageAmount,
        DamageResponseType? responseType)
    {
        if (responseType is not null)
        {
            ValidateDamageResponseType(
                responseType.Value,
                nameof(responseType));
        }

        ValidateDamageAmount(damageAmount);

        return responseType switch
        {
            DamageResponseType.Immunity => 0,
            DamageResponseType.Resistance => damageAmount / 2,
            DamageResponseType.Vulnerability => checked(damageAmount * 2),
            null => damageAmount,
            _ => throw new InvalidOperationException(
                "Validated damage response was not handled.")
        };
    }

    public static int ApplyDamageResponses(
        int damageAmount,
        IReadOnlyList<DamageResponseType> responseTypes)
    {
        ArgumentNullException.ThrowIfNull(responseTypes);

        ValidateDamageResponseTypes(responseTypes);
        ValidateDamageAmount(damageAmount);

        return ApplyDamageResponsesCore(
            damageAmount,
            responseTypes);
    }

    public static DamageDice GetCriticalHitDamageDice(DamageDice damage)
    {
        ArgumentNullException.ThrowIfNull(damage);
        ValidateDamageDice(damage);

        return damage with
        {
            Count = checked(damage.Count * 2)
        };
    }

    public static DamageDice? GetDamageDiceForAttackOutcome(
        DamageDice damage,
        AttackRollOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(damage);
        ValidateAttackOutcome(
            outcome,
            nameof(outcome));
        ValidateDamageDice(damage);

        return outcome switch
        {
            AttackRollOutcome.Miss => null,
            AttackRollOutcome.Hit => damage,
            AttackRollOutcome.CriticalHit =>
                GetCriticalHitDamageDice(damage),
            _ => throw new InvalidOperationException(
                "Validated attack outcome was not handled.")
        };
    }

    public static int GetDamageDiceTotal(
        DamageDice damage,
        IReadOnlyList<int> rolls)
    {
        ArgumentNullException.ThrowIfNull(damage);
        ArgumentNullException.ThrowIfNull(rolls);

        ValidateDamageDice(damage);

        if (rolls.Count != damage.Count)
        {
            throw new ArgumentException(
                $"Expected {damage.Count} damage roll(s), but received {rolls.Count}.",
                nameof(rolls));
        }

        int maximumRoll = (int)damage.Die;

        foreach (int roll in rolls)
        {
            if (roll is < 1 || roll > maximumRoll)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rolls),
                    roll,
                    $"Damage roll must be between 1 and {maximumRoll}.");
            }
        }

        return rolls.Sum();
    }

    public static DamageRollResult ResolveDamageRoll(
        DamageDice damage,
        IReadOnlyList<int> rolls,
        int damageBonus)
    {
        ArgumentNullException.ThrowIfNull(damage);
        ArgumentNullException.ThrowIfNull(rolls);

        IReadOnlyList<int> protectedRolls =
            CoreCollectionProtection.ProtectList(rolls);

        int diceTotal = GetDamageDiceTotal(
            damage,
            protectedRolls);

        int total = checked(diceTotal + damageBonus);

        return new DamageRollResult
        {
            DamageDice = damage,
            Rolls = protectedRolls,
            DiceTotal = diceTotal,
            DamageBonus = damageBonus,
            Total = Math.Max(0, total),
        };
    }

    public static DamageResolutionResult ResolveDamage(
        DamageDice damage,
        IReadOnlyList<int> rolls,
        int damageBonus,
        IReadOnlyList<DamageResponseType> responseTypes)
    {
        ArgumentNullException.ThrowIfNull(damage);
        ArgumentNullException.ThrowIfNull(rolls);
        ArgumentNullException.ThrowIfNull(responseTypes);

        IReadOnlyList<DamageResponseType> protectedResponseTypes =
            CoreCollectionProtection.ProtectList(responseTypes);

        return ResolveDamageCore(
            damage,
            rolls,
            damageBonus,
            protectedResponseTypes);
    }

    public static AttackDamageResolutionResult ResolveAttackDamage(
        DamageDice damage,
        AttackRollOutcome attackOutcome,
        IReadOnlyList<int> rolls,
        int damageBonus,
        IReadOnlyList<DamageResponseType> responseTypes)
    {
        ArgumentNullException.ThrowIfNull(damage);
        ArgumentNullException.ThrowIfNull(rolls);
        ArgumentNullException.ThrowIfNull(responseTypes);

        ValidateAttackOutcome(
            attackOutcome,
            nameof(attackOutcome));

        IReadOnlyList<DamageResponseType> protectedResponseTypes =
            CoreCollectionProtection.ProtectList(responseTypes);

        DamageDice? damageDice = GetDamageDiceForAttackOutcome(
            damage,
            attackOutcome);

        if (damageDice is null)
        {
            ValidateDamageResponseTypes(protectedResponseTypes);

            if (rolls.Count > 0)
            {
                throw new ArgumentException(
                    "Missed attacks should not include damage rolls.",
                    nameof(rolls));
            }

            return new AttackDamageResolutionResult
            {
                AttackOutcome = attackOutcome,
                DamageDice = null,
                DamageRoll = null,
                ResponseTypes = protectedResponseTypes,
                FinalDamage = 0
            };
        }

        DamageResolutionResult damageResolution = ResolveDamageCore(
            damageDice,
            rolls,
            damageBonus,
            protectedResponseTypes);

        return new AttackDamageResolutionResult
        {
            AttackOutcome = attackOutcome,
            DamageDice = damageDice,
            DamageRoll = damageResolution.DamageRoll,
            ResponseTypes = damageResolution.ResponseTypes,
            FinalDamage = damageResolution.FinalDamage
        };
    }

    private static DamageResolutionResult ResolveDamageCore(
        DamageDice damage,
        IReadOnlyList<int> rolls,
        int damageBonus,
        IReadOnlyList<DamageResponseType> protectedResponseTypes)
    {
        ValidateDamageResponseTypes(protectedResponseTypes);

        DamageRollResult damageRoll = ResolveDamageRoll(
            damage,
            rolls,
            damageBonus);

        int finalDamage = ApplyDamageResponsesCore(
            damageRoll.Total,
            protectedResponseTypes);

        return new DamageResolutionResult
        {
            DamageRoll = damageRoll,
            ResponseTypes = protectedResponseTypes,
            FinalDamage = finalDamage
        };
    }

    private static int ApplyDamageResponsesCore(
        int damageAmount,
        IReadOnlyList<DamageResponseType> responseTypes)
    {
        if (responseTypes.Contains(DamageResponseType.Immunity))
        {
            return 0;
        }

        int result = damageAmount;

        if (responseTypes.Contains(DamageResponseType.Resistance))
        {
            result /= 2;
        }

        if (responseTypes.Contains(DamageResponseType.Vulnerability))
        {
            result = checked(result * 2);
        }

        return result;
    }

    private static void ValidateDamageAmount(int damageAmount)
    {
        if (damageAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(damageAmount),
                damageAmount,
                "Damage amount cannot be negative.");
        }
    }

    private static void ValidateDamageDice(DamageDice damage)
    {
        if (!Enum.IsDefined(damage.Die))
        {
            throw new ArgumentOutOfRangeException(
                nameof(damage),
                damage.Die,
                "Damage die is not supported.");
        }

        if (damage.Count < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(damage),
                damage.Count,
                "Damage dice count must be at least 1.");
        }
    }

    private static void ValidateAttackOutcome(
        AttackRollOutcome outcome,
        string parameterName)
    {
        if (!Enum.IsDefined(outcome))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                outcome,
                "Attack outcome is not supported.");
        }
    }

    private static void ValidateDamageResponseTypes(
        IReadOnlyList<DamageResponseType> responseTypes)
    {
        foreach (DamageResponseType responseType in responseTypes)
        {
            ValidateDamageResponseType(
                responseType,
                nameof(responseTypes));
        }
    }

    private static void ValidateDamageResponseType(
        DamageResponseType responseType,
        string parameterName)
    {
        if (!Enum.IsDefined(responseType))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                responseType,
                "Damage response type is not supported.");
        }
    }
}

using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Core.Rules;

public static class DamageRules
{
    public static int ApplyDamageResponse(
        int damageAmount,
        DamageResponseType? responseType)
    {
        if (damageAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(damageAmount),
                damageAmount,
                "Damage amount cannot be negative.");
        }

        return responseType switch
        {
            DamageResponseType.Immunity => 0,
            DamageResponseType.Resistance => damageAmount / 2,
            DamageResponseType.Vulnerability => damageAmount * 2,
            null => damageAmount,
            _ => damageAmount
        };
    }

    public static int ApplyDamageResponses(
        int damageAmount,
        IReadOnlyList<DamageResponseType> responseTypes)
    {
        if (damageAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(damageAmount),
                damageAmount,
                "Damage amount cannot be negative.");
        }

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
            result *= 2;
        }

        return result;
    }

    public static DamageDice GetCriticalHitDamageDice(DamageDice damage)
    {
        if (damage.Count < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(damage),
                damage.Count,
                "Damage dice count must be at least 1.");
        }

        return damage with
        {
            Count = damage.Count * 2
        };
    }
    public static DamageDice? GetDamageDiceForAttackOutcome(
        DamageDice damage,
        AttackRollOutcome outcome)
    {
        if (outcome == AttackRollOutcome.Miss)
        {
            return null;
        }

        if (damage.Count < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(damage),
                damage.Count,
                "Damage dice count must be at least 1.");
        }

        if (outcome == AttackRollOutcome.CriticalHit)
        {
            return GetCriticalHitDamageDice(damage);
        }

        return damage;
    }
    public static int GetDamageDiceTotal(
        DamageDice damage,
        IReadOnlyList<int> rolls)
    {
        if (damage.Count < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(damage),
                damage.Count,
                "Damage dice count must be at least 1.");
        }

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
        int diceTotal = GetDamageDiceTotal(
            damage,
            rolls);

        return new DamageRollResult
        {
            DamageDice = damage,
            Rolls = rolls,
            DiceTotal = diceTotal,
            DamageBonus = damageBonus,
            Total = diceTotal + damageBonus
        };
    }
    public static DamageResolutionResult ResolveDamage(
    DamageDice damage,
    IReadOnlyList<int> rolls,
    int damageBonus,
    IReadOnlyList<DamageResponseType> responseTypes)
    {
        DamageRollResult damageRoll = ResolveDamageRoll(
            damage,
            rolls,
            damageBonus);

        int finalDamage = ApplyDamageResponses(
            damageRoll.Total,
            responseTypes);

        return new DamageResolutionResult
        {
            DamageRoll = damageRoll,
            ResponseTypes = responseTypes,
            FinalDamage = finalDamage
        };
    }
}
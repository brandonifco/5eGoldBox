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
}
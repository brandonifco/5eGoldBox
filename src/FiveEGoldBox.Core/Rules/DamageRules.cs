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
}
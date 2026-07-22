namespace FiveEGoldBox.Core.Rules;

public static class D20Rules
{
    public static D20RollMode ResolveRollMode(
        bool hasAdvantage,
        bool hasDisadvantage)
    {
        if (hasAdvantage == hasDisadvantage)
        {
            return D20RollMode.Normal;
        }

        return hasAdvantage
            ? D20RollMode.Advantage
            : D20RollMode.Disadvantage;
    }

    public static int ResolveNaturalRoll(
        D20RollMode rollMode,
        int firstRoll,
        int? secondRoll = null)
    {
        ValidateRollMode(
            rollMode,
            nameof(rollMode));

        ValidateD20Roll(firstRoll);

        if (rollMode == D20RollMode.Normal)
        {
            return firstRoll;
        }

        if (secondRoll is null)
        {
            throw new ArgumentException(
                "A second d20 roll is required for advantage or disadvantage.",
                nameof(secondRoll));
        }

        ValidateD20Roll(secondRoll.Value);

        return rollMode == D20RollMode.Advantage
            ? Math.Max(firstRoll, secondRoll.Value)
            : Math.Min(firstRoll, secondRoll.Value);
    }

    internal static void ValidateRollMode(
        D20RollMode rollMode,
        string parameterName)
    {
        if (!Enum.IsDefined(rollMode))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                rollMode,
                "D20 roll mode is not supported.");
        }
    }

    internal static int ResolveTotal(
        int naturalRoll,
        int bonus)
    {
        return checked(naturalRoll + bonus);
    }

    private static void ValidateD20Roll(int roll)
    {
        if (roll is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(
                nameof(roll),
                roll,
                "D20 roll must be between 1 and 20.");
        }
    }
}

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
}
namespace FiveEGoldBox.Core.Rules;

public static class D20TestRules
{
    public static D20TestOutcome ResolveOutcome(
        int naturalRoll,
        int bonus,
        int difficultyClass)
    {
        if (naturalRoll is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(
                nameof(naturalRoll),
                naturalRoll,
                "Natural d20 roll must be between 1 and 20.");
        }

        int total = naturalRoll + bonus;

        return total >= difficultyClass
            ? D20TestOutcome.Success
            : D20TestOutcome.Failure;
    }
}
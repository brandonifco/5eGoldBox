namespace FiveEGoldBox.Core.Rules;

public static class D20TestRules
{
    public static D20TestOutcome ResolveOutcome(
        int naturalRoll,
        int bonus,
        int difficultyClass)
    {
        ValidateNaturalRoll(naturalRoll);

        int total = D20Rules.ResolveTotal(
            naturalRoll,
            bonus);

        return ResolveOutcomeFromTotal(
            total,
            difficultyClass);
    }

    public static D20TestOutcome ResolveOutcome(
        D20RollMode rollMode,
        int firstRoll,
        int? secondRoll,
        int bonus,
        int difficultyClass)
    {
        int naturalRoll = D20Rules.ResolveNaturalRoll(
            rollMode,
            firstRoll,
            secondRoll);

        return ResolveOutcome(
            naturalRoll,
            bonus,
            difficultyClass);
    }

    public static D20TestResult ResolveResult(
        D20RollMode rollMode,
        int firstRoll,
        int? secondRoll,
        int bonus,
        int difficultyClass)
    {
        int naturalRoll = D20Rules.ResolveNaturalRoll(
            rollMode,
            firstRoll,
            secondRoll);

        int total = D20Rules.ResolveTotal(
            naturalRoll,
            bonus);

        D20TestOutcome outcome = ResolveOutcomeFromTotal(
            total,
            difficultyClass);

        return new D20TestResult
        {
            RollMode = rollMode,
            FirstRoll = firstRoll,
            SecondRoll = secondRoll,
            NaturalRoll = naturalRoll,
            Bonus = bonus,
            Total = total,
            DifficultyClass = difficultyClass,
            Outcome = outcome
        };
    }

    private static D20TestOutcome ResolveOutcomeFromTotal(
        int total,
        int difficultyClass)
    {
        return total >= difficultyClass
            ? D20TestOutcome.Success
            : D20TestOutcome.Failure;
    }

    private static void ValidateNaturalRoll(int naturalRoll)
    {
        if (naturalRoll is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(
                nameof(naturalRoll),
                naturalRoll,
                "Natural d20 roll must be between 1 and 20.");
        }
    }
}

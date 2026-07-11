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

        D20TestOutcome outcome = ResolveOutcome(
            naturalRoll,
            bonus,
            difficultyClass);

        return new D20TestResult
        {
            RollMode = rollMode,
            FirstRoll = firstRoll,
            SecondRoll = secondRoll,
            NaturalRoll = naturalRoll,
            Bonus = bonus,
            Total = naturalRoll + bonus,
            DifficultyClass = difficultyClass,
            Outcome = outcome
        };
    }
}

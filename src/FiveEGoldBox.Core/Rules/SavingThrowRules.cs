namespace FiveEGoldBox.Core.Rules;

public static class SavingThrowRules
{
    public static SavingThrowResult ResolveSavingThrow(
        Ability ability,
        D20RollMode rollMode,
        int firstRoll,
        int? secondRoll,
        int savingThrowBonus,
        int difficultyClass)
    {
        D20TestResult test = D20TestRules.ResolveResult(
            rollMode,
            firstRoll,
            secondRoll,
            savingThrowBonus,
            difficultyClass);

        return new SavingThrowResult
        {
            Ability = ability,
            Test = test
        };
    }
}

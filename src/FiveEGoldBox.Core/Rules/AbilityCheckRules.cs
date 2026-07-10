namespace FiveEGoldBox.Core.Rules;

public static class AbilityCheckRules
{
    public static AbilityCheckResult ResolveAbilityCheck(
        Ability ability,
        D20RollMode rollMode,
        int firstRoll,
        int? secondRoll,
        int abilityCheckBonus,
        int difficultyClass)
    {
        D20TestResult test = D20TestRules.ResolveResult(
            rollMode,
            firstRoll,
            secondRoll,
            abilityCheckBonus,
            difficultyClass);

        return new AbilityCheckResult
        {
            Ability = ability,
            Test = test
        };
    }
}
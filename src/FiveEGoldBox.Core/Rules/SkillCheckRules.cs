namespace FiveEGoldBox.Core.Rules;

public static class SkillCheckRules
{
    public static SkillCheckResult ResolveSkillCheck(
        string skillId,
        Ability ability,
        D20RollMode rollMode,
        int firstRoll,
        int? secondRoll,
        int skillCheckBonus,
        int difficultyClass)
    {
        if (string.IsNullOrWhiteSpace(skillId))
        {
            throw new ArgumentException(
                "Skill ID is required.",
                nameof(skillId));
        }

        D20TestResult test = D20TestRules.ResolveResult(
            rollMode,
            firstRoll,
            secondRoll,
            skillCheckBonus,
            difficultyClass);

        return new SkillCheckResult
        {
            SkillId = skillId,
            Ability = ability,
            Test = test
        };
    }
}

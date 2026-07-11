namespace FiveEGoldBox.Core.Rules;

public static class ConditionRules
{
    public static bool CanApplyCondition(
        ConditionType condition,
        IReadOnlyList<ConditionType> conditionImmunities)
    {
        return !conditionImmunities.Contains(condition);
    }
}

namespace FiveEGoldBox.Core.Rules;

public static class ConditionRules
{
    public static bool CanApplyCondition(
        ConditionType condition,
        IReadOnlyList<ConditionType> conditionImmunities)
    {
        ArgumentNullException.ThrowIfNull(conditionImmunities);

        ValidateCondition(
            condition,
            nameof(condition));

        foreach (ConditionType immunity in conditionImmunities)
        {
            ValidateCondition(
                immunity,
                nameof(conditionImmunities));
        }

        return !conditionImmunities.Contains(condition);
    }

    private static void ValidateCondition(
        ConditionType condition,
        string parameterName)
    {
        if (!Enum.IsDefined(condition))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                condition,
                "Condition is not supported.");
        }
    }
}

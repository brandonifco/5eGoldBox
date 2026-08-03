using FiveEGoldBox.Core.Internal;

namespace FiveEGoldBox.Core.Rules;

internal static class ConditionRules
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
        CoreEnumValidation.RequireDefined(
            condition,
            parameterName,
            "Condition is not supported.");
    }
}

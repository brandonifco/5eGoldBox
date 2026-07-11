using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class ConditionRulesTests
{
    [Fact]
    public void CanApplyCondition_WhenConditionIsNotInImmunityList_ReturnsTrue()
    {
        bool result = ConditionRules.CanApplyCondition(
            ConditionType.Poisoned,
            []);

        Assert.True(result);
    }

    [Fact]
    public void CanApplyCondition_WhenConditionIsInImmunityList_ReturnsFalse()
    {
        bool result = ConditionRules.CanApplyCondition(
            ConditionType.Poisoned,
            [ConditionType.Poisoned]);

        Assert.False(result);
    }

    [Fact]
    public void CanApplyCondition_WhenDifferentConditionIsInImmunityList_ReturnsTrue()
    {
        bool result = ConditionRules.CanApplyCondition(
            ConditionType.Poisoned,
            [ConditionType.Charmed]);

        Assert.True(result);
    }
}

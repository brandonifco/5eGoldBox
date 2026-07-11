using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class AbilityCheckRulesTests
{
    [Fact]
    public void ResolveAbilityCheck_WhenTotalMeetsDifficultyClass_ReturnsSuccess()
    {
        AbilityCheckResult result = AbilityCheckRules.ResolveAbilityCheck(
            Ability.Strength,
            D20RollMode.Normal,
            firstRoll: 12,
            secondRoll: null,
            abilityCheckBonus: 5,
            difficultyClass: 17);

        Assert.Equal(Ability.Strength, result.Ability);
        Assert.Equal(D20RollMode.Normal, result.Test.RollMode);
        Assert.Equal(12, result.Test.NaturalRoll);
        Assert.Equal(5, result.Test.Bonus);
        Assert.Equal(17, result.Test.Total);
        Assert.Equal(17, result.Test.DifficultyClass);
        Assert.Equal(D20TestOutcome.Success, result.Test.Outcome);
    }

    [Fact]
    public void ResolveAbilityCheck_WhenTotalIsBelowDifficultyClass_ReturnsFailure()
    {
        AbilityCheckResult result = AbilityCheckRules.ResolveAbilityCheck(
            Ability.Intelligence,
            D20RollMode.Normal,
            firstRoll: 8,
            secondRoll: null,
            abilityCheckBonus: 2,
            difficultyClass: 15);

        Assert.Equal(Ability.Intelligence, result.Ability);
        Assert.Equal(10, result.Test.Total);
        Assert.Equal(D20TestOutcome.Failure, result.Test.Outcome);
    }

    [Fact]
    public void ResolveAbilityCheck_WithAdvantage_UsesHigherRoll()
    {
        AbilityCheckResult result = AbilityCheckRules.ResolveAbilityCheck(
            Ability.Dexterity,
            D20RollMode.Advantage,
            firstRoll: 5,
            secondRoll: 15,
            abilityCheckBonus: 2,
            difficultyClass: 17);

        Assert.Equal(Ability.Dexterity, result.Ability);
        Assert.Equal(D20RollMode.Advantage, result.Test.RollMode);
        Assert.Equal(15, result.Test.NaturalRoll);
        Assert.Equal(17, result.Test.Total);
        Assert.Equal(D20TestOutcome.Success, result.Test.Outcome);
    }

    [Fact]
    public void ResolveAbilityCheck_WithDisadvantage_UsesLowerRoll()
    {
        AbilityCheckResult result = AbilityCheckRules.ResolveAbilityCheck(
            Ability.Charisma,
            D20RollMode.Disadvantage,
            firstRoll: 20,
            secondRoll: 1,
            abilityCheckBonus: 99,
            difficultyClass: 20);

        Assert.Equal(Ability.Charisma, result.Ability);
        Assert.Equal(D20RollMode.Disadvantage, result.Test.RollMode);
        Assert.Equal(1, result.Test.NaturalRoll);
        Assert.Equal(100, result.Test.Total);
        Assert.Equal(D20TestOutcome.Success, result.Test.Outcome);
    }

    [Fact]
    public void ResolveAbilityCheck_WithAdvantageAndMissingSecondRoll_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            AbilityCheckRules.ResolveAbilityCheck(
                Ability.Strength,
                D20RollMode.Advantage,
                firstRoll: 7,
                secondRoll: null,
                abilityCheckBonus: 5,
                difficultyClass: 17));
    }
}

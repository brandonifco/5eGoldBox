using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class SavingThrowRulesTests
{
    [Fact]
    public void ResolveSavingThrow_WhenTotalMeetsDifficultyClass_ReturnsSuccess()
    {
        SavingThrowResult result = SavingThrowRules.ResolveSavingThrow(
            Ability.Dexterity,
            D20RollMode.Normal,
            firstRoll: 12,
            secondRoll: null,
            savingThrowBonus: 5,
            difficultyClass: 17);

        Assert.Equal(Ability.Dexterity, result.Ability);
        Assert.Equal(D20RollMode.Normal, result.Test.RollMode);
        Assert.Equal(12, result.Test.NaturalRoll);
        Assert.Equal(5, result.Test.Bonus);
        Assert.Equal(17, result.Test.Total);
        Assert.Equal(17, result.Test.DifficultyClass);
        Assert.Equal(D20TestOutcome.Success, result.Test.Outcome);
    }

    [Fact]
    public void ResolveSavingThrow_WhenTotalIsBelowDifficultyClass_ReturnsFailure()
    {
        SavingThrowResult result = SavingThrowRules.ResolveSavingThrow(
            Ability.Wisdom,
            D20RollMode.Normal,
            firstRoll: 8,
            secondRoll: null,
            savingThrowBonus: 2,
            difficultyClass: 15);

        Assert.Equal(Ability.Wisdom, result.Ability);
        Assert.Equal(10, result.Test.Total);
        Assert.Equal(D20TestOutcome.Failure, result.Test.Outcome);
    }

    [Fact]
    public void ResolveSavingThrow_WithAdvantage_UsesHigherRoll()
    {
        SavingThrowResult result = SavingThrowRules.ResolveSavingThrow(
            Ability.Constitution,
            D20RollMode.Advantage,
            firstRoll: 5,
            secondRoll: 15,
            savingThrowBonus: 2,
            difficultyClass: 17);

        Assert.Equal(Ability.Constitution, result.Ability);
        Assert.Equal(D20RollMode.Advantage, result.Test.RollMode);
        Assert.Equal(15, result.Test.NaturalRoll);
        Assert.Equal(17, result.Test.Total);
        Assert.Equal(D20TestOutcome.Success, result.Test.Outcome);
    }

    [Fact]
    public void ResolveSavingThrow_WithDisadvantage_UsesLowerRoll()
    {
        SavingThrowResult result = SavingThrowRules.ResolveSavingThrow(
            Ability.Strength,
            D20RollMode.Disadvantage,
            firstRoll: 20,
            secondRoll: 1,
            savingThrowBonus: 99,
            difficultyClass: 20);

        Assert.Equal(Ability.Strength, result.Ability);
        Assert.Equal(D20RollMode.Disadvantage, result.Test.RollMode);
        Assert.Equal(1, result.Test.NaturalRoll);
        Assert.Equal(100, result.Test.Total);
        Assert.Equal(D20TestOutcome.Success, result.Test.Outcome);
    }

    [Fact]
    public void ResolveSavingThrow_WithAdvantageAndMissingSecondRoll_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            SavingThrowRules.ResolveSavingThrow(
                Ability.Dexterity,
                D20RollMode.Advantage,
                firstRoll: 7,
                secondRoll: null,
                savingThrowBonus: 5,
                difficultyClass: 17));
    }
}

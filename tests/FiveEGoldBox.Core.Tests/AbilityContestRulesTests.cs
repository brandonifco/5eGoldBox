using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class AbilityContestRulesTests
{
    [Fact]
    public void ResolveAbilityContest_WhenFirstTotalIsHigher_ReturnsFirstWins()
    {
        AbilityContestResult result = AbilityContestRules.ResolveAbilityContest(
            firstAbility: Ability.Strength,
            firstRollMode: D20RollMode.Normal,
            firstRoll: 12,
            firstSecondRoll: null,
            firstBonus: 5,
            secondAbility: Ability.Dexterity,
            secondRollMode: D20RollMode.Normal,
            secondRoll: 10,
            secondSecondRoll: null,
            secondBonus: 2);

        Assert.Equal(D20ContestOutcome.FirstWins, result.Outcome);

        Assert.Equal(Ability.Strength, result.FirstContestant.Ability);
        Assert.Equal(12, result.FirstContestant.Contestant.NaturalRoll);
        Assert.Equal(5, result.FirstContestant.Contestant.Bonus);
        Assert.Equal(17, result.FirstContestant.Contestant.Total);

        Assert.Equal(Ability.Dexterity, result.SecondContestant.Ability);
        Assert.Equal(10, result.SecondContestant.Contestant.NaturalRoll);
        Assert.Equal(2, result.SecondContestant.Contestant.Bonus);
        Assert.Equal(12, result.SecondContestant.Contestant.Total);
    }

    [Fact]
    public void ResolveAbilityContest_WhenSecondTotalIsHigher_ReturnsSecondWins()
    {
        AbilityContestResult result = AbilityContestRules.ResolveAbilityContest(
            firstAbility: Ability.Strength,
            firstRollMode: D20RollMode.Normal,
            firstRoll: 8,
            firstSecondRoll: null,
            firstBonus: 3,
            secondAbility: Ability.Dexterity,
            secondRollMode: D20RollMode.Normal,
            secondRoll: 14,
            secondSecondRoll: null,
            secondBonus: 4);

        Assert.Equal(D20ContestOutcome.SecondWins, result.Outcome);

        Assert.Equal(Ability.Strength, result.FirstContestant.Ability);
        Assert.Equal(11, result.FirstContestant.Contestant.Total);

        Assert.Equal(Ability.Dexterity, result.SecondContestant.Ability);
        Assert.Equal(18, result.SecondContestant.Contestant.Total);
    }

    [Fact]
    public void ResolveAbilityContest_WhenTotalsAreEqual_ReturnsTie()
    {
        AbilityContestResult result = AbilityContestRules.ResolveAbilityContest(
            firstAbility: Ability.Strength,
            firstRollMode: D20RollMode.Normal,
            firstRoll: 12,
            firstSecondRoll: null,
            firstBonus: 3,
            secondAbility: Ability.Strength,
            secondRollMode: D20RollMode.Normal,
            secondRoll: 10,
            secondSecondRoll: null,
            secondBonus: 5);

        Assert.Equal(D20ContestOutcome.Tie, result.Outcome);

        Assert.Equal(Ability.Strength, result.FirstContestant.Ability);
        Assert.Equal(15, result.FirstContestant.Contestant.Total);

        Assert.Equal(Ability.Strength, result.SecondContestant.Ability);
        Assert.Equal(15, result.SecondContestant.Contestant.Total);
    }

    [Fact]
    public void ResolveAbilityContest_WithFirstContestantAdvantage_UsesHigherRoll()
    {
        AbilityContestResult result = AbilityContestRules.ResolveAbilityContest(
            firstAbility: Ability.Charisma,
            firstRollMode: D20RollMode.Advantage,
            firstRoll: 5,
            firstSecondRoll: 15,
            firstBonus: 2,
            secondAbility: Ability.Wisdom,
            secondRollMode: D20RollMode.Normal,
            secondRoll: 12,
            secondSecondRoll: null,
            secondBonus: 4);

        Assert.Equal(D20ContestOutcome.FirstWins, result.Outcome);

        Assert.Equal(Ability.Charisma, result.FirstContestant.Ability);
        Assert.Equal(D20RollMode.Advantage, result.FirstContestant.Contestant.RollMode);
        Assert.Equal(15, result.FirstContestant.Contestant.NaturalRoll);
        Assert.Equal(17, result.FirstContestant.Contestant.Total);

        Assert.Equal(Ability.Wisdom, result.SecondContestant.Ability);
        Assert.Equal(16, result.SecondContestant.Contestant.Total);
    }

    [Fact]
    public void ResolveAbilityContest_WithSecondContestantDisadvantage_UsesLowerRoll()
    {
        AbilityContestResult result = AbilityContestRules.ResolveAbilityContest(
            firstAbility: Ability.Strength,
            firstRollMode: D20RollMode.Normal,
            firstRoll: 10,
            firstSecondRoll: null,
            firstBonus: 3,
            secondAbility: Ability.Dexterity,
            secondRollMode: D20RollMode.Disadvantage,
            secondRoll: 20,
            secondSecondRoll: 1,
            secondBonus: 99);

        Assert.Equal(D20ContestOutcome.SecondWins, result.Outcome);

        Assert.Equal(Ability.Strength, result.FirstContestant.Ability);
        Assert.Equal(13, result.FirstContestant.Contestant.Total);

        Assert.Equal(Ability.Dexterity, result.SecondContestant.Ability);
        Assert.Equal(D20RollMode.Disadvantage, result.SecondContestant.Contestant.RollMode);
        Assert.Equal(1, result.SecondContestant.Contestant.NaturalRoll);
        Assert.Equal(100, result.SecondContestant.Contestant.Total);
    }

    [Fact]
    public void ResolveAbilityContest_WithAdvantageAndMissingSecondRoll_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            AbilityContestRules.ResolveAbilityContest(
                firstAbility: Ability.Strength,
                firstRollMode: D20RollMode.Advantage,
                firstRoll: 7,
                firstSecondRoll: null,
                firstBonus: 5,
                secondAbility: Ability.Dexterity,
                secondRollMode: D20RollMode.Normal,
                secondRoll: 12,
                secondSecondRoll: null,
                secondBonus: 3));
    }
}
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class SkillContestRulesTests
{
    [Fact]
    public void ResolveSkillContest_WhenFirstTotalIsHigher_ReturnsFirstWins()
    {
        SkillContestResult result = SkillContestRules.ResolveSkillContest(
            firstSkillId: "skill.stealth",
            firstAbility: Ability.Dexterity,
            firstRollMode: D20RollMode.Normal,
            firstRoll: 12,
            firstSecondRoll: null,
            firstBonus: 5,
            secondSkillId: "skill.perception",
            secondAbility: Ability.Wisdom,
            secondRollMode: D20RollMode.Normal,
            secondRoll: 10,
            secondSecondRoll: null,
            secondBonus: 2);

        Assert.Equal(D20ContestOutcome.FirstWins, result.Outcome);

        Assert.Equal("skill.stealth", result.FirstContestant.SkillId);
        Assert.Equal(Ability.Dexterity, result.FirstContestant.Ability);
        Assert.Equal(12, result.FirstContestant.Contestant.NaturalRoll);
        Assert.Equal(5, result.FirstContestant.Contestant.Bonus);
        Assert.Equal(17, result.FirstContestant.Contestant.Total);

        Assert.Equal("skill.perception", result.SecondContestant.SkillId);
        Assert.Equal(Ability.Wisdom, result.SecondContestant.Ability);
        Assert.Equal(10, result.SecondContestant.Contestant.NaturalRoll);
        Assert.Equal(2, result.SecondContestant.Contestant.Bonus);
        Assert.Equal(12, result.SecondContestant.Contestant.Total);
    }

    [Fact]
    public void ResolveSkillContest_WhenSecondTotalIsHigher_ReturnsSecondWins()
    {
        SkillContestResult result = SkillContestRules.ResolveSkillContest(
            firstSkillId: "skill.athletics",
            firstAbility: Ability.Strength,
            firstRollMode: D20RollMode.Normal,
            firstRoll: 8,
            firstSecondRoll: null,
            firstBonus: 3,
            secondSkillId: "skill.acrobatics",
            secondAbility: Ability.Dexterity,
            secondRollMode: D20RollMode.Normal,
            secondRoll: 14,
            secondSecondRoll: null,
            secondBonus: 4);

        Assert.Equal(D20ContestOutcome.SecondWins, result.Outcome);

        Assert.Equal("skill.athletics", result.FirstContestant.SkillId);
        Assert.Equal(Ability.Strength, result.FirstContestant.Ability);
        Assert.Equal(11, result.FirstContestant.Contestant.Total);

        Assert.Equal("skill.acrobatics", result.SecondContestant.SkillId);
        Assert.Equal(Ability.Dexterity, result.SecondContestant.Ability);
        Assert.Equal(18, result.SecondContestant.Contestant.Total);
    }

    [Fact]
    public void ResolveSkillContest_WhenTotalsAreEqual_ReturnsTie()
    {
        SkillContestResult result = SkillContestRules.ResolveSkillContest(
            firstSkillId: "skill.athletics",
            firstAbility: Ability.Strength,
            firstRollMode: D20RollMode.Normal,
            firstRoll: 12,
            firstSecondRoll: null,
            firstBonus: 3,
            secondSkillId: "skill.athletics",
            secondAbility: Ability.Strength,
            secondRollMode: D20RollMode.Normal,
            secondRoll: 10,
            secondSecondRoll: null,
            secondBonus: 5);

        Assert.Equal(D20ContestOutcome.Tie, result.Outcome);

        Assert.Equal("skill.athletics", result.FirstContestant.SkillId);
        Assert.Equal(Ability.Strength, result.FirstContestant.Ability);
        Assert.Equal(15, result.FirstContestant.Contestant.Total);

        Assert.Equal("skill.athletics", result.SecondContestant.SkillId);
        Assert.Equal(Ability.Strength, result.SecondContestant.Ability);
        Assert.Equal(15, result.SecondContestant.Contestant.Total);
    }

    [Fact]
    public void ResolveSkillContest_WithFirstContestantAdvantage_UsesHigherRoll()
    {
        SkillContestResult result = SkillContestRules.ResolveSkillContest(
            firstSkillId: "skill.deception",
            firstAbility: Ability.Charisma,
            firstRollMode: D20RollMode.Advantage,
            firstRoll: 5,
            firstSecondRoll: 15,
            firstBonus: 2,
            secondSkillId: "skill.insight",
            secondAbility: Ability.Wisdom,
            secondRollMode: D20RollMode.Normal,
            secondRoll: 12,
            secondSecondRoll: null,
            secondBonus: 4);

        Assert.Equal(D20ContestOutcome.FirstWins, result.Outcome);

        Assert.Equal("skill.deception", result.FirstContestant.SkillId);
        Assert.Equal(Ability.Charisma, result.FirstContestant.Ability);
        Assert.Equal(D20RollMode.Advantage, result.FirstContestant.Contestant.RollMode);
        Assert.Equal(15, result.FirstContestant.Contestant.NaturalRoll);
        Assert.Equal(17, result.FirstContestant.Contestant.Total);

        Assert.Equal("skill.insight", result.SecondContestant.SkillId);
        Assert.Equal(Ability.Wisdom, result.SecondContestant.Ability);
        Assert.Equal(16, result.SecondContestant.Contestant.Total);
    }

    [Fact]
    public void ResolveSkillContest_WithSecondContestantDisadvantage_UsesLowerRoll()
    {
        SkillContestResult result = SkillContestRules.ResolveSkillContest(
            firstSkillId: "skill.athletics",
            firstAbility: Ability.Strength,
            firstRollMode: D20RollMode.Normal,
            firstRoll: 10,
            firstSecondRoll: null,
            firstBonus: 3,
            secondSkillId: "skill.acrobatics",
            secondAbility: Ability.Dexterity,
            secondRollMode: D20RollMode.Disadvantage,
            secondRoll: 20,
            secondSecondRoll: 1,
            secondBonus: 99);

        Assert.Equal(D20ContestOutcome.SecondWins, result.Outcome);

        Assert.Equal("skill.athletics", result.FirstContestant.SkillId);
        Assert.Equal(Ability.Strength, result.FirstContestant.Ability);
        Assert.Equal(13, result.FirstContestant.Contestant.Total);

        Assert.Equal("skill.acrobatics", result.SecondContestant.SkillId);
        Assert.Equal(Ability.Dexterity, result.SecondContestant.Ability);
        Assert.Equal(D20RollMode.Disadvantage, result.SecondContestant.Contestant.RollMode);
        Assert.Equal(1, result.SecondContestant.Contestant.NaturalRoll);
        Assert.Equal(100, result.SecondContestant.Contestant.Total);
    }

    [Fact]
    public void ResolveSkillContest_WithFirstContestantAdvantageAndMissingSecondRoll_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            SkillContestRules.ResolveSkillContest(
                firstSkillId: "skill.stealth",
                firstAbility: Ability.Dexterity,
                firstRollMode: D20RollMode.Advantage,
                firstRoll: 7,
                firstSecondRoll: null,
                firstBonus: 5,
                secondSkillId: "skill.perception",
                secondAbility: Ability.Wisdom,
                secondRollMode: D20RollMode.Normal,
                secondRoll: 12,
                secondSecondRoll: null,
                secondBonus: 3));
    }

    [Fact]
    public void ResolveSkillContest_WithSecondContestantDisadvantageAndMissingSecondRoll_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            SkillContestRules.ResolveSkillContest(
                firstSkillId: "skill.stealth",
                firstAbility: Ability.Dexterity,
                firstRollMode: D20RollMode.Normal,
                firstRoll: 12,
                firstSecondRoll: null,
                firstBonus: 3,
                secondSkillId: "skill.perception",
                secondAbility: Ability.Wisdom,
                secondRollMode: D20RollMode.Disadvantage,
                secondRoll: 7,
                secondSecondRoll: null,
                secondBonus: 5));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ResolveSkillContest_WithMissingFirstSkillId_Throws(string firstSkillId)
    {
        Assert.Throws<ArgumentException>(() =>
            SkillContestRules.ResolveSkillContest(
                firstSkillId,
                firstAbility: Ability.Dexterity,
                firstRollMode: D20RollMode.Normal,
                firstRoll: 12,
                firstSecondRoll: null,
                firstBonus: 5,
                secondSkillId: "skill.perception",
                secondAbility: Ability.Wisdom,
                secondRollMode: D20RollMode.Normal,
                secondRoll: 10,
                secondSecondRoll: null,
                secondBonus: 2));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ResolveSkillContest_WithMissingSecondSkillId_Throws(string secondSkillId)
    {
        Assert.Throws<ArgumentException>(() =>
            SkillContestRules.ResolveSkillContest(
                firstSkillId: "skill.stealth",
                firstAbility: Ability.Dexterity,
                firstRollMode: D20RollMode.Normal,
                firstRoll: 12,
                firstSecondRoll: null,
                firstBonus: 5,
                secondSkillId,
                secondAbility: Ability.Wisdom,
                secondRollMode: D20RollMode.Normal,
                secondRoll: 10,
                secondSecondRoll: null,
                secondBonus: 2));
    }
}

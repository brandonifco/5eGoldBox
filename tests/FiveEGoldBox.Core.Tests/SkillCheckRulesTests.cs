using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class SkillCheckRulesTests
{
    [Fact]
    public void ResolveSkillCheck_WhenTotalMeetsDifficultyClass_ReturnsSuccess()
    {
        SkillCheckResult result = SkillCheckRules.ResolveSkillCheck(
            skillId: "skill.stealth",
            Ability.Dexterity,
            D20RollMode.Normal,
            firstRoll: 12,
            secondRoll: null,
            skillCheckBonus: 5,
            difficultyClass: 17);

        Assert.Equal("skill.stealth", result.SkillId);
        Assert.Equal(Ability.Dexterity, result.Ability);
        Assert.Equal(D20RollMode.Normal, result.Test.RollMode);
        Assert.Equal(12, result.Test.NaturalRoll);
        Assert.Equal(5, result.Test.Bonus);
        Assert.Equal(17, result.Test.Total);
        Assert.Equal(17, result.Test.DifficultyClass);
        Assert.Equal(D20TestOutcome.Success, result.Test.Outcome);
    }

    [Fact]
    public void ResolveSkillCheck_WhenTotalIsBelowDifficultyClass_ReturnsFailure()
    {
        SkillCheckResult result = SkillCheckRules.ResolveSkillCheck(
            skillId: "skill.arcana",
            Ability.Intelligence,
            D20RollMode.Normal,
            firstRoll: 8,
            secondRoll: null,
            skillCheckBonus: 2,
            difficultyClass: 15);

        Assert.Equal("skill.arcana", result.SkillId);
        Assert.Equal(Ability.Intelligence, result.Ability);
        Assert.Equal(10, result.Test.Total);
        Assert.Equal(D20TestOutcome.Failure, result.Test.Outcome);
    }

    [Fact]
    public void ResolveSkillCheck_WithAdvantage_UsesHigherRoll()
    {
        SkillCheckResult result = SkillCheckRules.ResolveSkillCheck(
            skillId: "skill.athletics",
            Ability.Strength,
            D20RollMode.Advantage,
            firstRoll: 5,
            secondRoll: 15,
            skillCheckBonus: 2,
            difficultyClass: 17);

        Assert.Equal("skill.athletics", result.SkillId);
        Assert.Equal(Ability.Strength, result.Ability);
        Assert.Equal(D20RollMode.Advantage, result.Test.RollMode);
        Assert.Equal(15, result.Test.NaturalRoll);
        Assert.Equal(17, result.Test.Total);
        Assert.Equal(D20TestOutcome.Success, result.Test.Outcome);
    }

    [Fact]
    public void ResolveSkillCheck_WithDisadvantage_UsesLowerRoll()
    {
        SkillCheckResult result = SkillCheckRules.ResolveSkillCheck(
            skillId: "skill.persuasion",
            Ability.Charisma,
            D20RollMode.Disadvantage,
            firstRoll: 20,
            secondRoll: 1,
            skillCheckBonus: 99,
            difficultyClass: 20);

        Assert.Equal("skill.persuasion", result.SkillId);
        Assert.Equal(Ability.Charisma, result.Ability);
        Assert.Equal(D20RollMode.Disadvantage, result.Test.RollMode);
        Assert.Equal(1, result.Test.NaturalRoll);
        Assert.Equal(100, result.Test.Total);
        Assert.Equal(D20TestOutcome.Success, result.Test.Outcome);
    }

    [Fact]
    public void ResolveSkillCheck_WithAdvantageAndMissingSecondRoll_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            SkillCheckRules.ResolveSkillCheck(
                skillId: "skill.stealth",
                Ability.Dexterity,
                D20RollMode.Advantage,
                firstRoll: 7,
                secondRoll: null,
                skillCheckBonus: 5,
                difficultyClass: 17));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ResolveSkillCheck_WithMissingSkillId_Throws(string skillId)
    {
        Assert.Throws<ArgumentException>(() =>
            SkillCheckRules.ResolveSkillCheck(
                skillId,
                Ability.Dexterity,
                D20RollMode.Normal,
                firstRoll: 12,
                secondRoll: null,
                skillCheckBonus: 5,
                difficultyClass: 17));
    }
}
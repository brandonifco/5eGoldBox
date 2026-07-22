using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class PublicRuleEnumBoundaryTests
{
    [Fact]
    public void ResolveNaturalRoll_WithUndefinedMode_RejectsModeBeforeSecondRollRequirement()
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                D20Rules.ResolveNaturalRoll(
                    (D20RollMode)999,
                    firstRoll: 10,
                    secondRoll: null));

        Assert.Equal("rollMode", exception.ParamName);
    }

    [Fact]
    public void ResolveContest_WithUndefinedFirstMode_IdentifiesFirstMode()
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                D20ContestRules.ResolveContest(
                    (D20RollMode)999,
                    firstRoll: 10,
                    firstSecondRoll: null,
                    firstBonus: 0,
                    secondRollMode: D20RollMode.Normal,
                    secondRoll: 10,
                    secondSecondRoll: null,
                    secondBonus: 0));

        Assert.Equal("firstRollMode", exception.ParamName);
    }

    [Fact]
    public void ResolveContest_WithUndefinedSecondMode_IdentifiesSecondMode()
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                D20ContestRules.ResolveContest(
                    D20RollMode.Normal,
                    firstRoll: 10,
                    firstSecondRoll: null,
                    firstBonus: 0,
                    secondRollMode: (D20RollMode)999,
                    secondRoll: 10,
                    secondSecondRoll: null,
                    secondBonus: 0));

        Assert.Equal("secondRollMode", exception.ParamName);
    }

    [Fact]
    public void ResolveSavingThrow_WithUndefinedAbility_Throws()
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SavingThrowRules.ResolveSavingThrow(
                    (Ability)999,
                    D20RollMode.Normal,
                    firstRoll: 10,
                    secondRoll: null,
                    savingThrowBonus: 0,
                    difficultyClass: 10));

        Assert.Equal("ability", exception.ParamName);
    }

    [Fact]
    public void ResolveAbilityCheck_WithUndefinedAbility_Throws()
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                AbilityCheckRules.ResolveAbilityCheck(
                    (Ability)999,
                    D20RollMode.Normal,
                    firstRoll: 10,
                    secondRoll: null,
                    abilityCheckBonus: 0,
                    difficultyClass: 10));

        Assert.Equal("ability", exception.ParamName);
    }

    [Fact]
    public void ResolveSkillCheck_WithUndefinedAbility_Throws()
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SkillCheckRules.ResolveSkillCheck(
                    "skill.test",
                    (Ability)999,
                    D20RollMode.Normal,
                    firstRoll: 10,
                    secondRoll: null,
                    skillCheckBonus: 0,
                    difficultyClass: 10));

        Assert.Equal("ability", exception.ParamName);
    }

    [Fact]
    public void ResolveAbilityContest_WithUndefinedFirstAbility_Throws()
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                AbilityContestRules.ResolveAbilityContest(
                    (Ability)999,
                    D20RollMode.Normal,
                    firstRoll: 10,
                    firstSecondRoll: null,
                    firstBonus: 0,
                    secondAbility: Ability.Dexterity,
                    secondRollMode: D20RollMode.Normal,
                    secondRoll: 10,
                    secondSecondRoll: null,
                    secondBonus: 0));

        Assert.Equal("firstAbility", exception.ParamName);
    }

    [Fact]
    public void ResolveAbilityContest_WithUndefinedSecondAbility_Throws()
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                AbilityContestRules.ResolveAbilityContest(
                    Ability.Strength,
                    D20RollMode.Normal,
                    firstRoll: 10,
                    firstSecondRoll: null,
                    firstBonus: 0,
                    secondAbility: (Ability)999,
                    secondRollMode: D20RollMode.Normal,
                    secondRoll: 10,
                    secondSecondRoll: null,
                    secondBonus: 0));

        Assert.Equal("secondAbility", exception.ParamName);
    }

    [Fact]
    public void ResolveSkillContest_WithUndefinedFirstAbility_Throws()
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SkillContestRules.ResolveSkillContest(
                    "skill.first",
                    (Ability)999,
                    D20RollMode.Normal,
                    firstRoll: 10,
                    firstSecondRoll: null,
                    firstBonus: 0,
                    secondSkillId: "skill.second",
                    secondAbility: Ability.Dexterity,
                    secondRollMode: D20RollMode.Normal,
                    secondRoll: 10,
                    secondSecondRoll: null,
                    secondBonus: 0));

        Assert.Equal("firstAbility", exception.ParamName);
    }

    [Fact]
    public void ResolveSkillContest_WithUndefinedSecondAbility_Throws()
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SkillContestRules.ResolveSkillContest(
                    "skill.first",
                    Ability.Strength,
                    D20RollMode.Normal,
                    firstRoll: 10,
                    firstSecondRoll: null,
                    firstBonus: 0,
                    secondSkillId: "skill.second",
                    secondAbility: (Ability)999,
                    secondRollMode: D20RollMode.Normal,
                    secondRoll: 10,
                    secondSecondRoll: null,
                    secondBonus: 0));

        Assert.Equal("secondAbility", exception.ParamName);
    }

    [Fact]
    public void ApplyDamageResponse_WithNullResponse_RemainsValid()
    {
        int result = DamageRules.ApplyDamageResponse(
            damageAmount: 9,
            responseType: null);

        Assert.Equal(9, result);
    }

    [Fact]
    public void ApplyDamageResponse_WithUndefinedResponse_Throws()
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                DamageRules.ApplyDamageResponse(
                    damageAmount: 9,
                    responseType: (DamageResponseType)999));

        Assert.Equal("responseType", exception.ParamName);
    }

    [Fact]
    public void ApplyDamageResponses_WithImmunityBeforeUndefinedElement_StillRejectsCollection()
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                DamageRules.ApplyDamageResponses(
                    damageAmount: 9,
                    responseTypes:
                    [
                        DamageResponseType.Immunity,
                        (DamageResponseType)999
                    ]));

        Assert.Equal("responseTypes", exception.ParamName);
    }

    [Fact]
    public void ApplyDamageResponses_WithUndefinedElement_RejectsEnumBeforeNegativeDamage()
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                DamageRules.ApplyDamageResponses(
                    damageAmount: -1,
                    responseTypes: [(DamageResponseType)999]));

        Assert.Equal("responseTypes", exception.ParamName);
    }

    [Fact]
    public void GetDamageDiceForAttackOutcome_WithUndefinedOutcome_ThrowsBeforeDamageValidation()
    {
        DamageDice invalidDamage = new()
        {
            Count = 0,
            Die = DieType.D6
        };

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                DamageRules.GetDamageDiceForAttackOutcome(
                    invalidDamage,
                    (AttackRollOutcome)999));

        Assert.Equal("outcome", exception.ParamName);
    }

    [Fact]
    public void ResolveAttackDamage_WithUndefinedOutcome_IdentifiesAttackOutcome()
    {
        DamageDice damage = CreateDamage();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                DamageRules.ResolveAttackDamage(
                    damage,
                    (AttackRollOutcome)999,
                    rolls: [],
                    damageBonus: 0,
                    responseTypes: []));

        Assert.Equal("attackOutcome", exception.ParamName);
    }

    [Fact]
    public void GetDamageDiceTotal_WithUndefinedDie_Throws()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = (DieType)999
        };

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                DamageRules.GetDamageDiceTotal(
                    damage,
                    rolls: [1]));

        Assert.Equal("damage", exception.ParamName);
    }

    [Fact]
    public void CanApplyCondition_WithUndefinedCondition_Throws()
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ConditionRules.CanApplyCondition(
                    (ConditionType)999,
                    []));

        Assert.Equal("condition", exception.ParamName);
    }

    [Fact]
    public void CanApplyCondition_WithMatchingImmunityBeforeUndefinedElement_StillRejectsCollection()
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ConditionRules.CanApplyCondition(
                    ConditionType.Poisoned,
                    [
                        ConditionType.Poisoned,
                        (ConditionType)999
                    ]));

        Assert.Equal("conditionImmunities", exception.ParamName);
    }

    [Fact]
    public void PointBuy_WithUndefinedAbilityKey_Throws()
    {
        Dictionary<Ability, int> scores = CreateScores();
        scores[(Ability)999] = 8;

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PointBuyRules.GetTotalCost(scores));

        Assert.Equal("scores", exception.ParamName);
    }

    [Fact]
    public void StandardArray_WithUndefinedAbilityKey_Throws()
    {
        Dictionary<Ability, int> scores = CreateScores();
        scores.Remove(Ability.Charisma);
        scores[(Ability)999] = 8;

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                StandardArrayRules.IsValid(scores));

        Assert.Equal("scores", exception.ParamName);
    }

    private static DamageDice CreateDamage()
    {
        return new DamageDice
        {
            Count = 1,
            Die = DieType.D6
        };
    }

    private static Dictionary<Ability, int> CreateScores()
    {
        return new Dictionary<Ability, int>
        {
            [Ability.Strength] = 15,
            [Ability.Dexterity] = 14,
            [Ability.Constitution] = 13,
            [Ability.Intelligence] = 12,
            [Ability.Wisdom] = 10,
            [Ability.Charisma] = 8
        };
    }
}

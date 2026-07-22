using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class PublicRuleNullBoundaryTests
{
    [Fact]
    public void CanApplyCondition_WithNullImmunities_IdentifiesPublicParameter()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                ConditionRules.CanApplyCondition(
                    ConditionType.Poisoned,
                    null!));

        Assert.Equal("conditionImmunities", exception.ParamName);
    }

    [Fact]
    public void ApplyDamageResponses_WithNullResponses_IdentifiesPublicParameter()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                DamageRules.ApplyDamageResponses(
                    damageAmount: -1,
                    responseTypes: null!));

        Assert.Equal("responseTypes", exception.ParamName);
    }

    [Fact]
    public void GetCriticalHitDamageDice_WithNullDamage_Throws()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                DamageRules.GetCriticalHitDamageDice(null!));

        Assert.Equal("damage", exception.ParamName);
    }

    [Fact]
    public void GetDamageDiceForAttackOutcome_WithNullDamage_Throws()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                DamageRules.GetDamageDiceForAttackOutcome(
                    null!,
                    AttackRollOutcome.Miss));

        Assert.Equal("damage", exception.ParamName);
    }

    [Fact]
    public void GetDamageDiceTotal_WithNullRolls_Throws()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                DamageRules.GetDamageDiceTotal(
                    CreateDamage(),
                    null!));

        Assert.Equal("rolls", exception.ParamName);
    }

    [Fact]
    public void ResolveDamageRoll_WithNullRolls_Throws()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                DamageRules.ResolveDamageRoll(
                    CreateDamage(),
                    null!,
                    damageBonus: 0));

        Assert.Equal("rolls", exception.ParamName);
    }

    [Fact]
    public void ResolveDamage_WithNullResponses_Throws()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                DamageRules.ResolveDamage(
                    CreateDamage(),
                    rolls: [1],
                    damageBonus: 0,
                    responseTypes: null!));

        Assert.Equal("responseTypes", exception.ParamName);
    }

    [Fact]
    public void ResolveAttackDamage_WithNullRolls_Throws()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                DamageRules.ResolveAttackDamage(
                    CreateDamage(),
                    AttackRollOutcome.Hit,
                    null!,
                    damageBonus: 0,
                    responseTypes: []));

        Assert.Equal("rolls", exception.ParamName);
    }


    [Fact]
    public void GetDamageDiceTotal_WithNullDamage_Throws()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                DamageRules.GetDamageDiceTotal(
                    null!,
                    rolls: []));

        Assert.Equal("damage", exception.ParamName);
    }

    [Fact]
    public void ResolveDamageRoll_WithNullDamage_Throws()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                DamageRules.ResolveDamageRoll(
                    null!,
                    rolls: [],
                    damageBonus: 0));

        Assert.Equal("damage", exception.ParamName);
    }

    [Fact]
    public void ResolveDamage_WithNullDamage_Throws()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                DamageRules.ResolveDamage(
                    null!,
                    rolls: [],
                    damageBonus: 0,
                    responseTypes: []));

        Assert.Equal("damage", exception.ParamName);
    }

    [Fact]
    public void ResolveDamage_WithNullRolls_Throws()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                DamageRules.ResolveDamage(
                    CreateDamage(),
                    rolls: null!,
                    damageBonus: 0,
                    responseTypes: []));

        Assert.Equal("rolls", exception.ParamName);
    }

    [Fact]
    public void ResolveAttackDamage_WithNullDamage_Throws()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                DamageRules.ResolveAttackDamage(
                    null!,
                    AttackRollOutcome.Hit,
                    rolls: [],
                    damageBonus: 0,
                    responseTypes: []));

        Assert.Equal("damage", exception.ParamName);
    }

    [Fact]
    public void ResolveAttackDamage_WithNullResponses_Throws()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                DamageRules.ResolveAttackDamage(
                    CreateDamage(),
                    AttackRollOutcome.Hit,
                    rolls: [1],
                    damageBonus: 0,
                    responseTypes: null!));

        Assert.Equal("responseTypes", exception.ParamName);
    }

    [Fact]
    public void ResolveAttack_WithNullDamage_ThrowsBeforeRollResolution()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                CombatRules.ResolveAttack(
                    (D20RollMode)999,
                    firstRoll: 10,
                    secondRoll: null,
                    attackBonus: 0,
                    targetArmorClass: 10,
                    damage: null!,
                    damageRolls: [],
                    damageBonus: 0,
                    responseTypes: []));

        Assert.Equal("damage", exception.ParamName);
    }

    [Fact]
    public void ResolveAttack_WithNullDamageRolls_Throws()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                CombatRules.ResolveAttack(
                    D20RollMode.Normal,
                    firstRoll: 10,
                    secondRoll: null,
                    attackBonus: 0,
                    targetArmorClass: 10,
                    damage: CreateDamage(),
                    damageRolls: null!,
                    damageBonus: 0,
                    responseTypes: []));

        Assert.Equal("damageRolls", exception.ParamName);
    }

    [Fact]
    public void ResolveAttack_WithNullResponseTypes_Throws()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                CombatRules.ResolveAttack(
                    D20RollMode.Normal,
                    firstRoll: 10,
                    secondRoll: null,
                    attackBonus: 0,
                    targetArmorClass: 10,
                    damage: CreateDamage(),
                    damageRolls: [1],
                    damageBonus: 0,
                    responseTypes: null!));

        Assert.Equal("responseTypes", exception.ParamName);
    }

    private static DamageDice CreateDamage()
    {
        return new DamageDice
        {
            Count = 1,
            Die = DieType.D6
        };
    }
}

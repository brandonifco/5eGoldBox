using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CombatRulesTests
{
    [Fact]
    public void ResolveAttack_WithMiss_ReturnsMissAndZeroDamage()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D8
        };

        AttackResolutionResult result = CombatRules.ResolveAttack(
            D20RollMode.Normal,
            firstRoll: 5,
            secondRoll: null,
            attackBonus: 2,
            targetArmorClass: 18,
            damage,
            damageRolls: [],
            damageBonus: 3,
            responseTypes: []);

        Assert.Equal(AttackRollOutcome.Miss, result.AttackRoll.Outcome);
        Assert.Equal(7, result.AttackRoll.Total);
        Assert.Null(result.Damage.DamageDice);
        Assert.Null(result.Damage.DamageRoll);
        Assert.Equal(0, result.Damage.FinalDamage);
    }

    [Fact]
    public void ResolveAttack_WithHit_ReturnsHitAndResolvedDamage()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D8
        };

        AttackResolutionResult result = CombatRules.ResolveAttack(
            D20RollMode.Normal,
            firstRoll: 12,
            secondRoll: null,
            attackBonus: 5,
            targetArmorClass: 17,
            damage,
            damageRolls: [6],
            damageBonus: 3,
            responseTypes: []);

        Assert.Equal(AttackRollOutcome.Hit, result.AttackRoll.Outcome);
        Assert.Equal(17, result.AttackRoll.Total);
        Assert.NotNull(result.Damage.DamageRoll);
        Assert.Equal(9, result.Damage.DamageRoll.Total);
        Assert.Equal(9, result.Damage.FinalDamage);
    }

    [Fact]
    public void ResolveAttack_WithCriticalHit_ReturnsCriticalHitAndDoubledDamageDice()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D8
        };

        AttackResolutionResult result = CombatRules.ResolveAttack(
            D20RollMode.Normal,
            firstRoll: 20,
            secondRoll: null,
            attackBonus: 0,
            targetArmorClass: 99,
            damage,
            damageRolls: [6, 4],
            damageBonus: 3,
            responseTypes: []);

        Assert.Equal(AttackRollOutcome.CriticalHit, result.AttackRoll.Outcome);
        Assert.NotNull(result.Damage.DamageDice);
        Assert.Equal(2, result.Damage.DamageDice.Count);
        Assert.Equal(DieType.D8, result.Damage.DamageDice.Die);
        Assert.NotNull(result.Damage.DamageRoll);
        Assert.Equal(13, result.Damage.DamageRoll.Total);
        Assert.Equal(13, result.Damage.FinalDamage);
    }

    [Fact]
    public void ResolveAttack_WithResistance_AppliesDamageResponse()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D8
        };

        AttackResolutionResult result = CombatRules.ResolveAttack(
            D20RollMode.Normal,
            firstRoll: 12,
            secondRoll: null,
            attackBonus: 5,
            targetArmorClass: 17,
            damage,
            damageRolls: [6],
            damageBonus: 3,
            responseTypes: [DamageResponseType.Resistance]);

        Assert.Equal(AttackRollOutcome.Hit, result.AttackRoll.Outcome);
        Assert.NotNull(result.Damage.DamageRoll);
        Assert.Equal(9, result.Damage.DamageRoll.Total);
        Assert.Equal([DamageResponseType.Resistance], result.Damage.ResponseTypes);
        Assert.Equal(4, result.Damage.FinalDamage);
    }

    [Fact]
    public void ResolveAttack_WithAdvantage_UsesHigherAttackRoll()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D8
        };

        AttackResolutionResult result = CombatRules.ResolveAttack(
            D20RollMode.Advantage,
            firstRoll: 5,
            secondRoll: 15,
            attackBonus: 2,
            targetArmorClass: 17,
            damage,
            damageRolls: [6],
            damageBonus: 3,
            responseTypes: []);

        Assert.Equal(D20RollMode.Advantage, result.AttackRoll.RollMode);
        Assert.Equal(15, result.AttackRoll.NaturalRoll);
        Assert.Equal(17, result.AttackRoll.Total);
        Assert.Equal(AttackRollOutcome.Hit, result.AttackRoll.Outcome);
        Assert.Equal(9, result.Damage.FinalDamage);
    }
}
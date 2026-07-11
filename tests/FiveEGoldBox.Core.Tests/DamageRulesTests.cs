using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class DamageRulesTests
{
    [Fact]
    public void ApplyDamageResponse_WithNoResponse_ReturnsOriginalDamage()
    {
        int result = DamageRules.ApplyDamageResponse(
            17,
            responseType: null);

        Assert.Equal(17, result);
    }

    [Fact]
    public void ApplyDamageResponse_WithImmunity_ReturnsZero()
    {
        int result = DamageRules.ApplyDamageResponse(
            17,
            DamageResponseType.Immunity);

        Assert.Equal(0, result);
    }

    [Fact]
    public void ApplyDamageResponse_WithResistance_HalvesDamageRoundedDown()
    {
        Assert.Equal(
            5,
            DamageRules.ApplyDamageResponse(
                10,
                DamageResponseType.Resistance));

        Assert.Equal(
            5,
            DamageRules.ApplyDamageResponse(
                11,
                DamageResponseType.Resistance));
    }

    [Fact]
    public void ApplyDamageResponse_WithVulnerability_DoublesDamage()
    {
        int result = DamageRules.ApplyDamageResponse(
            17,
            DamageResponseType.Vulnerability);

        Assert.Equal(34, result);
    }

    [Fact]
    public void ApplyDamageResponse_WithNegativeDamage_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DamageRules.ApplyDamageResponse(
                -1,
                DamageResponseType.Resistance));
    }

    [Fact]
    public void ApplyDamageResponses_WithNoResponses_ReturnsOriginalDamage()
    {
        int result = DamageRules.ApplyDamageResponses(
            17,
            []);

        Assert.Equal(17, result);
    }

    [Fact]
    public void ApplyDamageResponses_WithImmunity_ReturnsZero()
    {
        int result = DamageRules.ApplyDamageResponses(
            17,
            [DamageResponseType.Immunity]);

        Assert.Equal(0, result);
    }

    [Fact]
    public void ApplyDamageResponses_WithResistance_HalvesDamageRoundedDown()
    {
        Assert.Equal(
            5,
            DamageRules.ApplyDamageResponses(
                10,
                [DamageResponseType.Resistance]));

        Assert.Equal(
            5,
            DamageRules.ApplyDamageResponses(
                11,
                [DamageResponseType.Resistance]));
    }

    [Fact]
    public void ApplyDamageResponses_WithVulnerability_DoublesDamage()
    {
        int result = DamageRules.ApplyDamageResponses(
            17,
            [DamageResponseType.Vulnerability]);

        Assert.Equal(34, result);
    }

    [Fact]
    public void ApplyDamageResponses_WithResistanceAndVulnerability_AppliesResistanceThenVulnerability()
    {
        int result = DamageRules.ApplyDamageResponses(
            11,
            [
                DamageResponseType.Resistance,
                DamageResponseType.Vulnerability
            ]);

        Assert.Equal(10, result);
    }

    [Fact]
    public void ApplyDamageResponses_WithImmunityResistanceAndVulnerability_ReturnsZero()
    {
        int result = DamageRules.ApplyDamageResponses(
            17,
            [
                DamageResponseType.Resistance,
                DamageResponseType.Vulnerability,
                DamageResponseType.Immunity
            ]);

        Assert.Equal(0, result);
    }

    [Fact]
    public void ApplyDamageResponses_WithNegativeDamage_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DamageRules.ApplyDamageResponses(
                -1,
                [DamageResponseType.Resistance]));
    }

    [Fact]
    public void GetCriticalHitDamageDice_DoublesDamageDiceCount()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D8
        };

        DamageDice result = DamageRules.GetCriticalHitDamageDice(damage);

        Assert.Equal(2, result.Count);
        Assert.Equal(DieType.D8, result.Die);
    }

    [Fact]
    public void GetCriticalHitDamageDice_WithMultipleDice_DoublesDamageDiceCount()
    {
        DamageDice damage = new()
        {
            Count = 2,
            Die = DieType.D6
        };

        DamageDice result = DamageRules.GetCriticalHitDamageDice(damage);

        Assert.Equal(4, result.Count);
        Assert.Equal(DieType.D6, result.Die);
    }

    [Fact]
    public void GetCriticalHitDamageDice_WithInvalidDamageDiceCount_Throws()
    {
        DamageDice damage = new()
        {
            Count = 0,
            Die = DieType.D6
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DamageRules.GetCriticalHitDamageDice(damage));
    }

    [Fact]
    public void GetDamageDiceForAttackOutcome_WithMiss_ReturnsNull()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D8
        };

        DamageDice? result = DamageRules.GetDamageDiceForAttackOutcome(
            damage,
            AttackRollOutcome.Miss);

        Assert.Null(result);
    }

    [Fact]
    public void GetDamageDiceForAttackOutcome_WithHit_ReturnsNormalDamageDice()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D8
        };

        DamageDice? result = DamageRules.GetDamageDiceForAttackOutcome(
            damage,
            AttackRollOutcome.Hit);

        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.Equal(DieType.D8, result.Die);
    }

    [Fact]
    public void GetDamageDiceForAttackOutcome_WithCriticalHit_ReturnsDoubledDamageDice()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D8
        };

        DamageDice? result = DamageRules.GetDamageDiceForAttackOutcome(
            damage,
            AttackRollOutcome.CriticalHit);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(DieType.D8, result.Die);
    }

    [Fact]
    public void GetDamageDiceForAttackOutcome_WithInvalidDamageDiceCountAndHit_Throws()
    {
        DamageDice damage = new()
        {
            Count = 0,
            Die = DieType.D6
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DamageRules.GetDamageDiceForAttackOutcome(
                damage,
                AttackRollOutcome.Hit));
    }

    [Fact]
    public void GetDamageDiceTotal_WithSingleDamageDie_ReturnsRollTotal()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D8
        };

        int result = DamageRules.GetDamageDiceTotal(
            damage,
            [6]);

        Assert.Equal(6, result);
    }

    [Fact]
    public void GetDamageDiceTotal_WithMultipleDamageDice_ReturnsRollTotal()
    {
        DamageDice damage = new()
        {
            Count = 2,
            Die = DieType.D6
        };

        int result = DamageRules.GetDamageDiceTotal(
            damage,
            [4, 5]);

        Assert.Equal(9, result);
    }

    [Fact]
    public void GetDamageDiceTotal_WithInvalidDamageDiceCount_Throws()
    {
        DamageDice damage = new()
        {
            Count = 0,
            Die = DieType.D6
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DamageRules.GetDamageDiceTotal(
                damage,
                []));
    }

    [Fact]
    public void GetDamageDiceTotal_WithWrongNumberOfRolls_Throws()
    {
        DamageDice damage = new()
        {
            Count = 2,
            Die = DieType.D6
        };

        Assert.Throws<ArgumentException>(() =>
            DamageRules.GetDamageDiceTotal(
                damage,
                [4]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void GetDamageDiceTotal_WithOutOfRangeRoll_Throws(int roll)
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D6
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DamageRules.GetDamageDiceTotal(
                damage,
                [roll]));
    }

    [Fact]
    public void ResolveDamageRoll_ReturnsDamageRollDetails()
    {
        DamageDice damage = new()
        {
            Count = 2,
            Die = DieType.D6
        };

        DamageRollResult result = DamageRules.ResolveDamageRoll(
            damage,
            [4, 5],
            damageBonus: 3);

        Assert.Equal(damage, result.DamageDice);
        Assert.Equal([4, 5], result.Rolls);
        Assert.Equal(9, result.DiceTotal);
        Assert.Equal(3, result.DamageBonus);
        Assert.Equal(12, result.Total);
    }

    [Fact]
    public void ResolveDamageRoll_WithNegativeDamageBonus_AllowsNegativeBonus()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D8
        };

        DamageRollResult result = DamageRules.ResolveDamageRoll(
            damage,
            [6],
            damageBonus: -2);

        Assert.Equal(6, result.DiceTotal);
        Assert.Equal(-2, result.DamageBonus);
        Assert.Equal(4, result.Total);
    }
    [Fact]
    public void ResolveDamageRoll_WhenDamageBonusWouldReduceTotalBelowOne_FloorsTotalAtZero()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D4
        };

        DamageRollResult result = DamageRules.ResolveDamageRoll(
            damage,
            [1],
            damageBonus: -5);

        Assert.Equal(1, result.DiceTotal);
        Assert.Equal(-5, result.DamageBonus);
        Assert.Equal(0, result.Total);
    }
    [Fact]
    public void ResolveDamageRoll_WithInvalidRolls_Throws()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D6
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DamageRules.ResolveDamageRoll(
                damage,
                [7],
                damageBonus: 3));
    }

    [Fact]
    public void ResolveDamage_WithNoResponses_ReturnsDamageRollTotalAsFinalDamage()
    {
        DamageDice damage = new()
        {
            Count = 2,
            Die = DieType.D6
        };

        DamageResolutionResult result = DamageRules.ResolveDamage(
            damage,
            [4, 5],
            damageBonus: 3,
            responseTypes: []);

        Assert.Equal(12, result.DamageRoll.Total);
        Assert.Empty(result.ResponseTypes);
        Assert.Equal(12, result.FinalDamage);
    }
    [Fact]
    public void ResolveDamage_WhenDamageBonusWouldReduceTotalBelowOne_ReturnsZeroFinalDamage()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D4
        };

        DamageResolutionResult result = DamageRules.ResolveDamage(
            damage,
            [1],
            damageBonus: -5,
            responseTypes: []);

        Assert.Equal(0, result.DamageRoll.Total);
        Assert.Equal(0, result.FinalDamage);
    }
    [Fact]
    public void ResolveDamage_WithResistance_AppliesResistanceToDamageRollTotal()
    {
        DamageDice damage = new()
        {
            Count = 2,
            Die = DieType.D6
        };

        DamageResolutionResult result = DamageRules.ResolveDamage(
            damage,
            [4, 5],
            damageBonus: 3,
            responseTypes: [DamageResponseType.Resistance]);

        Assert.Equal(12, result.DamageRoll.Total);
        Assert.Equal([DamageResponseType.Resistance], result.ResponseTypes);
        Assert.Equal(6, result.FinalDamage);
    }

    [Fact]
    public void ResolveDamage_WithImmunity_ReturnsZeroFinalDamage()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D8
        };

        DamageResolutionResult result = DamageRules.ResolveDamage(
            damage,
            [6],
            damageBonus: 2,
            responseTypes: [DamageResponseType.Immunity]);

        Assert.Equal(8, result.DamageRoll.Total);
        Assert.Equal(0, result.FinalDamage);
    }

    [Fact]
    public void ResolveDamage_WithInvalidRolls_Throws()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D6
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DamageRules.ResolveDamage(
                damage,
                [7],
                damageBonus: 3,
                responseTypes: []));
    }

    [Fact]
    public void ResolveAttackDamage_WithMiss_ReturnsZeroDamageWithoutDamageRoll()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D8
        };

        AttackDamageResolutionResult result = DamageRules.ResolveAttackDamage(
            damage,
            AttackRollOutcome.Miss,
            rolls: [],
            damageBonus: 3,
            responseTypes: []);

        Assert.Equal(AttackRollOutcome.Miss, result.AttackOutcome);
        Assert.Null(result.DamageDice);
        Assert.Null(result.DamageRoll);
        Assert.Empty(result.ResponseTypes);
        Assert.Equal(0, result.FinalDamage);
    }

    [Fact]
    public void ResolveAttackDamage_WithMissAndDamageRolls_Throws()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D8
        };

        Assert.Throws<ArgumentException>(() =>
            DamageRules.ResolveAttackDamage(
                damage,
                AttackRollOutcome.Miss,
                rolls: [6],
                damageBonus: 3,
                responseTypes: []));
    }

    [Fact]
    public void ResolveAttackDamage_WithHit_UsesNormalDamageDice()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D8
        };

        AttackDamageResolutionResult result = DamageRules.ResolveAttackDamage(
            damage,
            AttackRollOutcome.Hit,
            rolls: [6],
            damageBonus: 3,
            responseTypes: []);

        Assert.Equal(AttackRollOutcome.Hit, result.AttackOutcome);
        Assert.NotNull(result.DamageDice);
        Assert.Equal(1, result.DamageDice.Count);
        Assert.Equal(DieType.D8, result.DamageDice.Die);
        Assert.NotNull(result.DamageRoll);
        Assert.Equal(9, result.DamageRoll.Total);
        Assert.Equal(9, result.FinalDamage);
    }

    [Fact]
    public void ResolveAttackDamage_WithCriticalHit_UsesDoubledDamageDice()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D8
        };

        AttackDamageResolutionResult result = DamageRules.ResolveAttackDamage(
            damage,
            AttackRollOutcome.CriticalHit,
            rolls: [6, 4],
            damageBonus: 3,
            responseTypes: []);

        Assert.Equal(AttackRollOutcome.CriticalHit, result.AttackOutcome);
        Assert.NotNull(result.DamageDice);
        Assert.Equal(2, result.DamageDice.Count);
        Assert.Equal(DieType.D8, result.DamageDice.Die);
        Assert.NotNull(result.DamageRoll);
        Assert.Equal(10, result.DamageRoll.DiceTotal);
        Assert.Equal(13, result.DamageRoll.Total);
        Assert.Equal(13, result.FinalDamage);
    }

    [Fact]
    public void ResolveAttackDamage_WithResistance_AppliesResponseAfterDamageRoll()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D8
        };

        AttackDamageResolutionResult result = DamageRules.ResolveAttackDamage(
            damage,
            AttackRollOutcome.Hit,
            rolls: [6],
            damageBonus: 3,
            responseTypes: [DamageResponseType.Resistance]);

        Assert.NotNull(result.DamageRoll);
        Assert.Equal(9, result.DamageRoll.Total);
        Assert.Equal([DamageResponseType.Resistance], result.ResponseTypes);
        Assert.Equal(4, result.FinalDamage);
    }
}
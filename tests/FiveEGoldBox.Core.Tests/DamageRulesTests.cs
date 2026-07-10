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
}
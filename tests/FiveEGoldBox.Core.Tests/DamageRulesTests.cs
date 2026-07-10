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
}
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class ArmorDefinitionTests
{
    [Fact]
    public void ArmorDefinition_CanRepresentLeatherArmor()
    {
        ArmorDefinition leather = new()
        {
            Id = "armor.leather",
            Name = "Leather",
            Category = ArmorCategory.Light,
            BaseArmorClass = 11,
            AddsDexterityModifier = true,
            MaximumDexterityModifier = null,
            ArmorClassBonus = 0,
            StrengthRequirement = null,
            HasStealthDisadvantage = false,
            WeightPounds = 10m
        };

        Assert.Equal("armor.leather", leather.Id);
        Assert.Equal("Leather", leather.Name);
        Assert.Equal(ArmorCategory.Light, leather.Category);
        Assert.Equal(11, leather.BaseArmorClass);
        Assert.True(leather.AddsDexterityModifier);
        Assert.Null(leather.MaximumDexterityModifier);
        Assert.Equal(0, leather.ArmorClassBonus);
        Assert.Null(leather.StrengthRequirement);
        Assert.False(leather.HasStealthDisadvantage);
        Assert.Equal(10m, leather.WeightPounds);
    }

    [Fact]
    public void ArmorDefinition_CanRepresentScaleMail()
    {
        ArmorDefinition scaleMail = new()
        {
            Id = "armor.scale_mail",
            Name = "Scale Mail",
            Category = ArmorCategory.Medium,
            BaseArmorClass = 14,
            AddsDexterityModifier = true,
            MaximumDexterityModifier = 2,
            ArmorClassBonus = 0,
            StrengthRequirement = null,
            HasStealthDisadvantage = true,
            WeightPounds = 45m
        };

        Assert.Equal("armor.scale_mail", scaleMail.Id);
        Assert.Equal(ArmorCategory.Medium, scaleMail.Category);
        Assert.Equal(14, scaleMail.BaseArmorClass);
        Assert.True(scaleMail.AddsDexterityModifier);
        Assert.Equal(2, scaleMail.MaximumDexterityModifier);
        Assert.True(scaleMail.HasStealthDisadvantage);
    }

    [Fact]
    public void ArmorDefinition_CanRepresentChainMail()
    {
        ArmorDefinition chainMail = new()
        {
            Id = "armor.chain_mail",
            Name = "Chain Mail",
            Category = ArmorCategory.Heavy,
            BaseArmorClass = 16,
            AddsDexterityModifier = false,
            MaximumDexterityModifier = null,
            ArmorClassBonus = 0,
            StrengthRequirement = 13,
            HasStealthDisadvantage = true,
            WeightPounds = 55m
        };

        Assert.Equal("armor.chain_mail", chainMail.Id);
        Assert.Equal(ArmorCategory.Heavy, chainMail.Category);
        Assert.Equal(16, chainMail.BaseArmorClass);
        Assert.False(chainMail.AddsDexterityModifier);
        Assert.Equal(13, chainMail.StrengthRequirement);
        Assert.True(chainMail.HasStealthDisadvantage);
    }

    [Fact]
    public void ArmorDefinition_CanRepresentShield()
    {
        ArmorDefinition shield = new()
        {
            Id = "armor.shield",
            Name = "Shield",
            Category = ArmorCategory.Shield,
            BaseArmorClass = 0,
            AddsDexterityModifier = false,
            MaximumDexterityModifier = null,
            ArmorClassBonus = 2,
            StrengthRequirement = null,
            HasStealthDisadvantage = false,
            WeightPounds = 6m
        };

        Assert.Equal("armor.shield", shield.Id);
        Assert.Equal(ArmorCategory.Shield, shield.Category);
        Assert.Equal(0, shield.BaseArmorClass);
        Assert.Equal(2, shield.ArmorClassBonus);
    }

    [Fact]
    public void RulesetDefinition_CanContainArmorDefinitions()
    {
        RulesetDefinition ruleset = new()
        {
            Id = "ruleset.test",
            Name = "Test Ruleset",
            Armors =
            [
                new ArmorDefinition
                {
                    Id = "armor.leather",
                    Name = "Leather",
                    Category = ArmorCategory.Light,
                    BaseArmorClass = 11,
                    AddsDexterityModifier = true,
                    WeightPounds = 10m
                },
                new ArmorDefinition
                {
                    Id = "armor.shield",
                    Name = "Shield",
                    Category = ArmorCategory.Shield,
                    BaseArmorClass = 0,
                    ArmorClassBonus = 2,
                    WeightPounds = 6m
                }
            ]
        };

        Assert.Equal(2, ruleset.Armors.Count);

        Assert.Contains(
            ruleset.Armors,
            armor => armor.Id == "armor.leather"
                && armor.Category == ArmorCategory.Light);

        Assert.Contains(
            ruleset.Armors,
            armor => armor.Id == "armor.shield"
                && armor.Category == ArmorCategory.Shield);
    }
}

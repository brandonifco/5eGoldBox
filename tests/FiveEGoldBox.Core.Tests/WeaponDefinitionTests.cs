using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class WeaponDefinitionTests
{
    [Fact]
    public void WeaponDefinition_CanRepresentLongsword()
    {
        WeaponDefinition longsword = new()
        {
            Id = "weapon.longsword",
            Name = "Longsword",
            Category = WeaponCategory.Martial,
            AttackKind = WeaponAttackKind.Melee,
            Damage = new DamageDice
            {
                Count = 1,
                Die = DieType.D8
            },
            DamageType = "damage.slashing",
            Properties =
            [
                "weapon_property.versatile"
            ],
            WeightPounds = 3m
        };

        Assert.Equal("weapon.longsword", longsword.Id);
        Assert.Equal("Longsword", longsword.Name);
        Assert.Equal(WeaponCategory.Martial, longsword.Category);
        Assert.Equal(WeaponAttackKind.Melee, longsword.AttackKind);
        Assert.Equal(1, longsword.Damage.Count);
        Assert.Equal(DieType.D8, longsword.Damage.Die);
        Assert.Equal("damage.slashing", longsword.DamageType);
        Assert.Contains("weapon_property.versatile", longsword.Properties);
        Assert.Equal(3m, longsword.WeightPounds);
    }

    [Fact]
    public void WeaponDefinition_CanRepresentShortbow()
    {
        WeaponDefinition shortbow = new()
        {
            Id = "weapon.shortbow",
            Name = "Shortbow",
            Category = WeaponCategory.Simple,
            AttackKind = WeaponAttackKind.Ranged,
            Damage = new DamageDice
            {
                Count = 1,
                Die = DieType.D6
            },
            DamageType = "damage.piercing",
            Properties =
            [
                "weapon_property.ammunition",
                "weapon_property.two_handed"
            ],
            NormalRangeFeet = 80,
            LongRangeFeet = 320,
            WeightPounds = 2m
        };

        Assert.Equal("weapon.shortbow", shortbow.Id);
        Assert.Equal("Shortbow", shortbow.Name);
        Assert.Equal(WeaponCategory.Simple, shortbow.Category);
        Assert.Equal(WeaponAttackKind.Ranged, shortbow.AttackKind);
        Assert.Equal(1, shortbow.Damage.Count);
        Assert.Equal(DieType.D6, shortbow.Damage.Die);
        Assert.Equal("damage.piercing", shortbow.DamageType);
        Assert.Equal(80, shortbow.NormalRangeFeet);
        Assert.Equal(320, shortbow.LongRangeFeet);
        Assert.Contains("weapon_property.ammunition", shortbow.Properties);
        Assert.Contains("weapon_property.two_handed", shortbow.Properties);
    }

    [Fact]
    public void RulesetDefinition_CanContainWeaponDefinitions()
    {
        RulesetDefinition ruleset = new()
        {
            Id = "ruleset.test",
            Name = "Test Ruleset",
            Weapons =
            [
                new WeaponDefinition
                {
                    Id = "weapon.longsword",
                    Name = "Longsword",
                    Category = WeaponCategory.Martial,
                    AttackKind = WeaponAttackKind.Melee,
                    Damage = new DamageDice
                    {
                        Count = 1,
                        Die = DieType.D8
                    },
                    DamageType = "damage.slashing",
                    WeightPounds = 3m
                },
                new WeaponDefinition
                {
                    Id = "weapon.shortbow",
                    Name = "Shortbow",
                    Category = WeaponCategory.Simple,
                    AttackKind = WeaponAttackKind.Ranged,
                    Damage = new DamageDice
                    {
                        Count = 1,
                        Die = DieType.D6
                    },
                    DamageType = "damage.piercing",
                    NormalRangeFeet = 80,
                    LongRangeFeet = 320,
                    WeightPounds = 2m
                }
            ]
        };

        Assert.Equal(2, ruleset.Weapons.Count);

        Assert.Contains(
            ruleset.Weapons,
            weapon => weapon.Id == "weapon.longsword"
                && weapon.Category == WeaponCategory.Martial);

        Assert.Contains(
            ruleset.Weapons,
            weapon => weapon.Id == "weapon.shortbow"
                && weapon.AttackKind == WeaponAttackKind.Ranged);
    }
}
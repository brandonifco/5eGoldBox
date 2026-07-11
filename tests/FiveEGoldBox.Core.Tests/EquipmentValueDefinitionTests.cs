using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class EquipmentValueDefinitionTests
{
    [Fact]
    public void EquipmentItemDefinition_CanRepresentCostInCopperPieces()
    {
        EquipmentItemDefinition backpack = new()
        {
            Id = "equipment.backpack",
            Name = "Backpack",
            WeightPounds = 5m,
            CostInCopperPieces = 200
        };

        Assert.Equal("equipment.backpack", backpack.Id);
        Assert.Equal("Backpack", backpack.Name);
        Assert.Equal(5m, backpack.WeightPounds);
        Assert.Equal(200, backpack.CostInCopperPieces);
    }

    [Fact]
    public void WeaponDefinition_CanRepresentCostInCopperPieces()
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
            WeightPounds = 3m,
            CostInCopperPieces = 1500
        };

        Assert.Equal("weapon.longsword", longsword.Id);
        Assert.Equal("Longsword", longsword.Name);
        Assert.Equal(3m, longsword.WeightPounds);
        Assert.Equal(1500, longsword.CostInCopperPieces);
    }

    [Fact]
    public void ArmorDefinition_CanRepresentCostInCopperPieces()
    {
        ArmorDefinition leather = new()
        {
            Id = "armor.leather",
            Name = "Leather",
            Category = ArmorCategory.Light,
            BaseArmorClass = 11,
            AddsDexterityModifier = true,
            WeightPounds = 10m,
            CostInCopperPieces = 1000
        };

        Assert.Equal("armor.leather", leather.Id);
        Assert.Equal("Leather", leather.Name);
        Assert.Equal(10m, leather.WeightPounds);
        Assert.Equal(1000, leather.CostInCopperPieces);
    }
}

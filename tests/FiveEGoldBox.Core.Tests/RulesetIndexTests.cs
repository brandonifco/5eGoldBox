using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

public sealed class RulesetIndexTests
{
    [Fact]
    public void Constructor_WithRuleset_IndexesTopLevelDefinitionsById()
    {
        RaceDefinition race = new()
        {
            Id = "race.test",
            Name = "Test Race",
            BaseSpeedFeet = 30
        };

        ClassDefinition characterClass = new()
        {
            Id = "class.test",
            Name = "Test Class",
            HitDie = DieType.D10
        };

        BackgroundDefinition background = new()
        {
            Id = "background.test",
            Name = "Test Background"
        };

        SkillDefinition skill = new()
        {
            Id = "skill.test",
            Name = "Test Skill",
            Ability = Ability.Wisdom
        };

        ArmorDefinition armor = new()
        {
            Id = "armor.test",
            Name = "Test Armor",
            Category = ArmorCategory.Light,
            BaseArmorClass = 11
        };

        WeaponDefinition weapon = new()
        {
            Id = "weapon.test",
            Name = "Test Weapon",
            Category = WeaponCategory.Simple,
            AttackKind = WeaponAttackKind.Melee,
            Damage = new DamageDice
            {
                Count = 1,
                Die = DieType.D6
            },
            DamageType = "damage.test"
        };

        EquipmentItemDefinition equipmentItem = new()
        {
            Id = "item.test",
            Name = "Test Item"
        };

        MonsterDefinition monster = new()
        {
            Id = "monster.test",
            Name = "Test Monster",
            MaximumHitPoints = 10,
            ArmorClass = 12,
            MovementSpeedFeet = 30,
            ZeroHitPointPolicy = CombatantZeroHitPointPolicy.Defeated,
            AbilityModifiers = Enum.GetValues<Ability>()
                .Select(ability => new MonsterAbilityModifier
                {
                    Ability = ability,
                    Modifier = 0
                })
                .ToArray(),
            ProficiencyBonus = 2,
            Weapons = [new MonsterWeaponDefinition { WeaponId = "weapon.test" }]
        };

        RulesetDefinition ruleset = new()
        {
            Id = "ruleset.test",
            Name = "Test Ruleset",
            Races = [race],
            Classes = [characterClass],
            Backgrounds = [background],
            Skills = [skill],
            Armors = [armor],
            Weapons = [weapon],
            Monsters = [monster],
            EquipmentItems = [equipmentItem]
        };

        RulesetIndex index = new(ruleset);

        Assert.NotSame(ruleset, index.Ruleset);
        Assert.Same(index.Ruleset.Races.Single(), index.RacesById["race.test"]);
        Assert.Same(index.Ruleset.Classes.Single(), index.ClassesById["class.test"]);
        Assert.Same(
            index.Ruleset.Backgrounds.Single(),
            index.BackgroundsById["background.test"]);
        Assert.Same(index.Ruleset.Skills.Single(), index.SkillsById["skill.test"]);
        Assert.Same(index.Ruleset.Armors.Single(), index.ArmorsById["armor.test"]);
        Assert.Same(index.Ruleset.Weapons.Single(), index.WeaponsById["weapon.test"]);
        Assert.Same(
            index.Ruleset.Monsters.Single(),
            index.MonstersById["monster.test"]);
        Assert.Same(
            index.Ruleset.EquipmentItems.Single(),
            index.EquipmentItemsById["item.test"]);
    }

    [Fact]
    public void Constructor_WithNullRuleset_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new RulesetIndex(null!));
    }
}

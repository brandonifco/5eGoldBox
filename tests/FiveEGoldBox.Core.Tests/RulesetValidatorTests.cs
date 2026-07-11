using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Core.Tests;

public sealed class RulesetValidatorTests
{
    [Fact]
    public void Validate_WithValidRuleset_ReturnsSuccess()
    {
        RulesetDefinition ruleset = new()
        {
            Id = "ruleset.test",
            Name = "Test Ruleset"
        };

        ValidationResult result = RulesetValidator.Validate(ruleset);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }
        [Fact]
    public void Validate_WithMissingTopLevelIdsOrNames_ReturnsErrors()
    {
        RulesetDefinition ruleset = new()
        {
            Id = " ",
            Name = "",
            Races = [CreateRace(" ", "")],
            Classes = [CreateClass(" ", "")],
            Backgrounds = [CreateBackground(" ", "")],
            Skills = [CreateSkill(" ", "")],
            Armors = [CreateArmor(" ", "")],
            Weapons = [CreateWeapon(" ", "")],
            EquipmentItems = [CreateEquipmentItem(" ", "")]
        };

        ValidationResult result = RulesetValidator.Validate(ruleset);

        Assert.False(result.IsValid);

        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.id.required");
        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.name.required");
        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.races.id.required");
        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.races.name.required");
        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.classes.id.required");
        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.classes.name.required");
        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.backgrounds.id.required");
        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.backgrounds.name.required");
        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.skills.id.required");
        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.skills.name.required");
        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.armors.id.required");
        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.armors.name.required");
        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.weapons.id.required");
        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.weapons.name.required");
        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.equipment_items.id.required");
        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.equipment_items.name.required");
        Assert.All(result.Issues, issue => Assert.Equal(ValidationSeverity.Error, issue.Severity));
    }
    [Fact]
    public void Validate_WithDuplicateTopLevelIds_ReturnsErrors()
    {
        RulesetDefinition ruleset = new()
        {
            Id = "ruleset.test",
            Name = "Test Ruleset",
            Races =
            [
                CreateRace("race.duplicate", "Race One"),
                CreateRace("race.duplicate", "Race Two")
            ],
            Classes =
            [
                CreateClass("class.duplicate", "Class One"),
                CreateClass("class.duplicate", "Class Two")
            ],
            Backgrounds =
            [
                CreateBackground("background.duplicate", "Background One"),
                CreateBackground("background.duplicate", "Background Two")
            ],
            Skills =
            [
                CreateSkill("skill.duplicate", "Skill One"),
                CreateSkill("skill.duplicate", "Skill Two")
            ],
            Armors =
            [
                CreateArmor("armor.duplicate", "Armor One"),
                CreateArmor("armor.duplicate", "Armor Two")
            ],
            Weapons =
            [
                CreateWeapon("weapon.duplicate", "Weapon One"),
                CreateWeapon("weapon.duplicate", "Weapon Two")
            ],
            EquipmentItems =
            [
                CreateEquipmentItem("item.duplicate", "Item One"),
                CreateEquipmentItem("item.duplicate", "Item Two")
            ]
        };

        ValidationResult result = RulesetValidator.Validate(ruleset);

        Assert.False(result.IsValid);

        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.races.duplicate_id");
        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.classes.duplicate_id");
        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.backgrounds.duplicate_id");
        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.skills.duplicate_id");
        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.armors.duplicate_id");
        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.weapons.duplicate_id");
        Assert.Contains(result.Issues, issue => issue.Code == "ruleset.equipment_items.duplicate_id");
        Assert.All(result.Issues, issue => Assert.Equal(ValidationSeverity.Error, issue.Severity));
    }

    [Fact]
    public void Validate_WithNullRuleset_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => RulesetValidator.Validate(null!));
    }
    [Fact]
    public void Validate_WithUnknownClassOrBackgroundSkillReferences_ReturnsErrors()
    {
        RulesetDefinition ruleset = new()
        {
            Id = "ruleset.test",
            Name = "Test Ruleset",
            Skills =
            [
                CreateSkill("skill.known", "Known Skill")
            ],
            Classes =
            [
                CreateClass("class.test", "Test Class") with
                {
                    SkillChoices = ["skill.known", "skill.unknown.class"]
                }
            ],
            Backgrounds =
            [
                CreateBackground("background.test", "Test Background") with
                {
                    SkillProficiencies = ["skill.known", "skill.unknown.background"]
                }
            ]
        };

        ValidationResult result = RulesetValidator.Validate(ruleset);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Issues,
            issue => issue.Code == "ruleset.classes.skill_choices.unknown_skill"
                && issue.Message.Contains("skill.unknown.class"));

        Assert.Contains(
            result.Issues,
            issue => issue.Code == "ruleset.backgrounds.skill_proficiencies.unknown_skill"
                && issue.Message.Contains("skill.unknown.background"));
    }
    private static RaceDefinition CreateRace(string id, string name)
    {
        return new RaceDefinition
        {
            Id = id,
            Name = name,
            BaseSpeedFeet = 30
        };
    }

    private static ClassDefinition CreateClass(string id, string name)
    {
        return new ClassDefinition
        {
            Id = id,
            Name = name,
            HitDie = DieType.D10
        };
    }

    private static BackgroundDefinition CreateBackground(string id, string name)
    {
        return new BackgroundDefinition
        {
            Id = id,
            Name = name
        };
    }

    private static SkillDefinition CreateSkill(string id, string name)
    {
        return new SkillDefinition
        {
            Id = id,
            Name = name,
            Ability = Ability.Wisdom
        };
    }

    private static ArmorDefinition CreateArmor(string id, string name)
    {
        return new ArmorDefinition
        {
            Id = id,
            Name = name,
            Category = ArmorCategory.Light,
            BaseArmorClass = 11
        };
    }

    private static WeaponDefinition CreateWeapon(string id, string name)
    {
        return new WeaponDefinition
        {
            Id = id,
            Name = name,
            Category = WeaponCategory.Simple,
            AttackKind = WeaponAttackKind.Melee,
            Damage = new DamageDice
            {
                Count = 1,
                Die = DieType.D6
            },
            DamageType = "damage.test"
        };
    }

    private static EquipmentItemDefinition CreateEquipmentItem(string id, string name)
    {
        return new EquipmentItemDefinition
        {
            Id = id,
            Name = name
        };
    }
}
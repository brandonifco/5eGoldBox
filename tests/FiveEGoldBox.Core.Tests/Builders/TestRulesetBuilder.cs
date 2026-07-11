using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests.Builders;

internal sealed class TestRulesetBuilder
{
    private string _id = "ruleset.test";
    private string _name = "Test Ruleset";
    private IReadOnlyList<RaceDefinition> _races = [HumanRace()];
    private IReadOnlyList<ClassDefinition> _classes = [FighterClass()];
    private IReadOnlyList<BackgroundDefinition> _backgrounds = Array.Empty<BackgroundDefinition>();
    private IReadOnlyList<SkillDefinition> _skills = FighterSkillDefinitions();
    private IReadOnlyList<ArmorDefinition> _armors = Array.Empty<ArmorDefinition>();
    private IReadOnlyList<WeaponDefinition> _weapons = Array.Empty<WeaponDefinition>();
    private IReadOnlyList<EquipmentItemDefinition> _equipmentItems = Array.Empty<EquipmentItemDefinition>();

    public static RulesetDefinition Default()
    {
        return new TestRulesetBuilder().Build();
    }

    public TestRulesetBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public TestRulesetBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public TestRulesetBuilder WithRaces(IReadOnlyList<RaceDefinition> races)
    {
        _races = races;
        return this;
    }

    public TestRulesetBuilder WithClasses(IReadOnlyList<ClassDefinition> classes)
    {
        _classes = classes;
        return this;
    }

    public TestRulesetBuilder WithBackgrounds(IReadOnlyList<BackgroundDefinition> backgrounds)
    {
        _backgrounds = backgrounds;
        return this;
    }

    public TestRulesetBuilder WithSkills(IReadOnlyList<SkillDefinition> skills)
    {
        _skills = skills;
        return this;
    }

    public TestRulesetBuilder WithArmors(IReadOnlyList<ArmorDefinition> armors)
    {
        _armors = armors;
        return this;
    }

    public TestRulesetBuilder WithWeapons(IReadOnlyList<WeaponDefinition> weapons)
    {
        _weapons = weapons;
        return this;
    }

    public TestRulesetBuilder WithEquipmentItems(IReadOnlyList<EquipmentItemDefinition> equipmentItems)
    {
        _equipmentItems = equipmentItems;
        return this;
    }

    public RulesetDefinition Build()
    {
        return new RulesetDefinition
        {
            Id = _id,
            Name = _name,
            Races = _races,
            Classes = _classes,
            Backgrounds = _backgrounds,
            Skills = _skills,
            Armors = _armors,
            Weapons = _weapons,
            EquipmentItems = _equipmentItems
        };
    }

    public static RaceDefinition HumanRace()
    {
        return new RaceDefinition
        {
            Id = "race.human",
            Name = "Human",
            BaseSpeedFeet = 30,
            AbilityScoreIncreases =
            [
                new AbilityScoreIncrease(Ability.Strength, 1),
                new AbilityScoreIncrease(Ability.Dexterity, 1),
                new AbilityScoreIncrease(Ability.Constitution, 1),
                new AbilityScoreIncrease(Ability.Intelligence, 1),
                new AbilityScoreIncrease(Ability.Wisdom, 1),
                new AbilityScoreIncrease(Ability.Charisma, 1)
            ],
            Languages =
            [
                "language.common"
            ]
        };
    }

    public static ClassDefinition FighterClass()
    {
        return new ClassDefinition
        {
            Id = "class.fighter",
            Name = "Fighter",
            HitDie = DieType.D10,
            SavingThrowProficiencies =
            [
                Ability.Strength,
                Ability.Constitution
            ],
            SkillChoices =
            [
                "skill.acrobatics",
                "skill.animal_handling",
                "skill.athletics",
                "skill.history",
                "skill.insight",
                "skill.intimidation",
                "skill.perception",
                "skill.survival"
            ],
            NumberOfSkillChoices = 2
        };
    }

    public static IReadOnlyList<SkillDefinition> FighterSkillDefinitions()
    {
        return
        [
            new SkillDefinition
            {
                Id = "skill.acrobatics",
                Name = "Acrobatics",
                Ability = Ability.Dexterity
            },
            new SkillDefinition
            {
                Id = "skill.animal_handling",
                Name = "Animal Handling",
                Ability = Ability.Wisdom
            },
            new SkillDefinition
            {
                Id = "skill.athletics",
                Name = "Athletics",
                Ability = Ability.Strength
            },
            new SkillDefinition
            {
                Id = "skill.history",
                Name = "History",
                Ability = Ability.Intelligence
            },
            new SkillDefinition
            {
                Id = "skill.insight",
                Name = "Insight",
                Ability = Ability.Wisdom
            },
            new SkillDefinition
            {
                Id = "skill.intimidation",
                Name = "Intimidation",
                Ability = Ability.Charisma
            },
            new SkillDefinition
            {
                Id = "skill.perception",
                Name = "Perception",
                Ability = Ability.Wisdom
            },
            new SkillDefinition
            {
                Id = "skill.survival",
                Name = "Survival",
                Ability = Ability.Wisdom
            }
        ];
    }
}

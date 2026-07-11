using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class ClassDefinitionTests
{
    [Fact]
    public void ClassDefinition_CanRepresentFighter()
    {
        ClassDefinition fighter = new()
        {
            Id = "class.fighter",
            Name = "Fighter",
            HitDie = DieType.D10,
            SavingThrowProficiencies =
            [
                Ability.Strength,
                Ability.Constitution
            ],
            ArmorProficiencies =
            [
                "armor.light",
                "armor.medium",
                "armor.heavy",
                "armor.shields"
            ],
            WeaponProficiencies =
            [
                "weapon.simple",
                "weapon.martial"
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
            NumberOfSkillChoices = 2,
            FeaturesByLevel = new Dictionary<int, IReadOnlyList<string>>
            {
                [1] =
                [
                    "feature.fighting_style",
                    "feature.second_wind"
                ]
            }
        };

        Assert.Equal("class.fighter", fighter.Id);
        Assert.Equal("Fighter", fighter.Name);
        Assert.Equal(DieType.D10, fighter.HitDie);

        Assert.Contains(Ability.Strength, fighter.SavingThrowProficiencies);
        Assert.Contains(Ability.Constitution, fighter.SavingThrowProficiencies);

        Assert.Contains("armor.heavy", fighter.ArmorProficiencies);
        Assert.Contains("armor.shields", fighter.ArmorProficiencies);

        Assert.Contains("weapon.simple", fighter.WeaponProficiencies);
        Assert.Contains("weapon.martial", fighter.WeaponProficiencies);

        Assert.Equal(2, fighter.NumberOfSkillChoices);
        Assert.Contains("skill.athletics", fighter.SkillChoices);
        Assert.Contains("skill.perception", fighter.SkillChoices);

        Assert.True(fighter.FeaturesByLevel.ContainsKey(1));
        Assert.Contains("feature.fighting_style", fighter.FeaturesByLevel[1]);
        Assert.Contains("feature.second_wind", fighter.FeaturesByLevel[1]);
    }

    [Fact]
    public void RulesetDefinition_CanContainClassDefinitions()
    {
        ClassDefinition fighter = new()
        {
            Id = "class.fighter",
            Name = "Fighter",
            HitDie = DieType.D10,
            SavingThrowProficiencies =
            [
                Ability.Strength,
                Ability.Constitution
            ]
        };

        RulesetDefinition ruleset = new()
        {
            Id = "ruleset.test",
            Name = "Test Ruleset",
            Classes =
            [
                fighter
            ]
        };

        ClassDefinition characterClass = Assert.Single(ruleset.Classes);

        Assert.Equal("class.fighter", characterClass.Id);
        Assert.Equal("Fighter", characterClass.Name);
        Assert.Equal(DieType.D10, characterClass.HitDie);
    }
}

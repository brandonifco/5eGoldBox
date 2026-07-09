using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class SkillDefinitionTests
{
    [Fact]
    public void SkillDefinition_CanRepresentAthletics()
    {
        SkillDefinition athletics = new()
        {
            Id = "skill.athletics",
            Name = "Athletics",
            Ability = Ability.Strength
        };

        Assert.Equal("skill.athletics", athletics.Id);
        Assert.Equal("Athletics", athletics.Name);
        Assert.Equal(Ability.Strength, athletics.Ability);
    }

    [Fact]
    public void SkillDefinition_CanRepresentPerception()
    {
        SkillDefinition perception = new()
        {
            Id = "skill.perception",
            Name = "Perception",
            Ability = Ability.Wisdom
        };

        Assert.Equal("skill.perception", perception.Id);
        Assert.Equal("Perception", perception.Name);
        Assert.Equal(Ability.Wisdom, perception.Ability);
    }

    [Fact]
    public void RulesetDefinition_CanContainSkillDefinitions()
    {
        RulesetDefinition ruleset = new()
        {
            Id = "ruleset.test",
            Name = "Test Ruleset",
            Skills =
            [
                new SkillDefinition
                {
                    Id = "skill.athletics",
                    Name = "Athletics",
                    Ability = Ability.Strength
                },
                new SkillDefinition
                {
                    Id = "skill.perception",
                    Name = "Perception",
                    Ability = Ability.Wisdom
                }
            ]
        };

        Assert.Equal(2, ruleset.Skills.Count);

        Assert.Contains(
            ruleset.Skills,
            skill => skill.Id == "skill.athletics"
                && skill.Ability == Ability.Strength);

        Assert.Contains(
            ruleset.Skills,
            skill => skill.Id == "skill.perception"
                && skill.Ability == Ability.Wisdom);
    }
}
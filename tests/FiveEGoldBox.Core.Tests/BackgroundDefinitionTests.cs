using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Core.Tests;

public sealed class BackgroundDefinitionTests
{
    [Fact]
    public void BackgroundDefinition_CanRepresentFolkHero()
    {
        BackgroundDefinition folkHero = new()
        {
            Id = "background.folk_hero",
            Name = "Folk Hero",
            SkillProficiencies =
            [
                "skill.animal_handling",
                "skill.survival"
            ],
            ToolProficiencies =
            [
                "tool.artisans_tools",
                "tool.vehicles_land"
            ],
            FeatureId = "feature.rustic_hospitality"
        };

        Assert.Equal("background.folk_hero", folkHero.Id);
        Assert.Equal("Folk Hero", folkHero.Name);

        Assert.Contains("skill.animal_handling", folkHero.SkillProficiencies);
        Assert.Contains("skill.survival", folkHero.SkillProficiencies);

        Assert.Contains("tool.artisans_tools", folkHero.ToolProficiencies);
        Assert.Contains("tool.vehicles_land", folkHero.ToolProficiencies);

        Assert.Equal("feature.rustic_hospitality", folkHero.FeatureId);
    }

    [Fact]
    public void RulesetDefinition_CanContainBackgroundDefinitions()
    {
        BackgroundDefinition folkHero = new()
        {
            Id = "background.folk_hero",
            Name = "Folk Hero",
            SkillProficiencies =
            [
                "skill.animal_handling",
                "skill.survival"
            ],
            FeatureId = "feature.rustic_hospitality"
        };

        RulesetDefinition ruleset = new()
        {
            Id = "ruleset.test",
            Name = "Test Ruleset",
            Backgrounds =
            [
                folkHero
            ]
        };

        BackgroundDefinition background = Assert.Single(ruleset.Backgrounds);

        Assert.Equal("background.folk_hero", background.Id);
        Assert.Equal("Folk Hero", background.Name);
        Assert.Equal("feature.rustic_hospitality", background.FeatureId);
    }
}

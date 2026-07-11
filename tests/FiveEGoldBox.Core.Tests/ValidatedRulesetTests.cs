using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Tests.Builders;

namespace FiveEGoldBox.Core.Tests;

public sealed class ValidatedRulesetTests
{
    [Fact]
    public void Load_WithValidRuleset_ReturnsValidatedRulesetAndIndex()
    {
        RulesetDefinition definition = TestRulesetBuilder.Default();

        RulesetLoadResult result = ValidatedRuleset.Load(definition);

        Assert.True(result.IsValid);
        Assert.True(result.Validation.IsValid);
        Assert.NotNull(result.Ruleset);
        Assert.Same(definition, result.Ruleset.Definition);
        Assert.Same(definition, result.Ruleset.Index.Ruleset);
        Assert.True(result.Ruleset.Index.RacesById.ContainsKey("race.human"));
        Assert.True(result.Ruleset.Index.ClassesById.ContainsKey("class.fighter"));
    }

    [Fact]
    public void Load_WithInvalidRuleset_ReturnsValidationIssuesWithoutRuleset()
    {
        RulesetDefinition definition = new()
        {
            Id = "ruleset.test",
            Name = "Test Ruleset",
            Races =
            [
                TestRulesetBuilder.HumanRace(),
                TestRulesetBuilder.HumanRace()
            ]
        };

        RulesetLoadResult result = ValidatedRuleset.Load(definition);

        Assert.False(result.IsValid);
        Assert.False(result.Validation.IsValid);
        Assert.Null(result.Ruleset);
        Assert.Contains(
            result.Validation.Issues,
            issue => issue.Code == "ruleset.races.duplicate_id");
    }

    [Fact]
    public void Load_WithNullRuleset_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ValidatedRuleset.Load(null!));
    }
}

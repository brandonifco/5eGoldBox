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
        Assert.NotSame(definition, result.Ruleset.Definition);
        Assert.Same(result.Ruleset.Definition, result.Ruleset.Index.Ruleset);
        Assert.True(result.Ruleset.Index.RacesById.ContainsKey("race.human"));
        Assert.True(result.Ruleset.Index.ClassesById.ContainsKey("class.fighter"));
    }

    [Fact]
    public void TryGetBackground_WithExactId_ReturnsCanonicalBackgroundAfterSourceMutation()
    {
        BackgroundDefinition sourceBackground = new()
        {
            Id = "background.test",
            Name = "Test Background"
        };
        List<BackgroundDefinition> sourceBackgrounds =
        [
            sourceBackground
        ];
        RulesetDefinition definition = new TestRulesetBuilder()
            .WithBackgrounds(sourceBackgrounds)
            .Build();
        RulesetLoadResult result = ValidatedRuleset.Load(definition);
        ValidatedRuleset ruleset = Assert.IsType<ValidatedRuleset>(
            result.Ruleset);

        sourceBackgrounds.Clear();

        Assert.Empty(sourceBackgrounds);
        Assert.True(ruleset.TryGetBackground(
            sourceBackground.Id,
            out BackgroundDefinition? background));
        Assert.NotNull(background);
        Assert.NotSame(sourceBackground, background);
        Assert.Same(ruleset.Definition.Backgrounds.Single(), background);
    }

    [Fact]
    public void TryGetBackground_WithMissingId_ReturnsFalseAndNull()
    {
        RulesetDefinition definition = new TestRulesetBuilder()
            .WithBackgrounds(
            [
                new BackgroundDefinition
                {
                    Id = "background.test",
                    Name = "Test Background"
                }
            ])
            .Build();
        ValidatedRuleset ruleset = Assert.IsType<ValidatedRuleset>(
            ValidatedRuleset.Load(definition).Ruleset);

        bool found = ruleset.TryGetBackground(
            "background.missing",
            out BackgroundDefinition? background);

        Assert.False(found);
        Assert.Null(background);
    }

    [Fact]
    public void TryGetBackground_UsesCaseSensitiveIdentifierComparison()
    {
        RulesetDefinition definition = new TestRulesetBuilder()
            .WithBackgrounds(
            [
                new BackgroundDefinition
                {
                    Id = "background.test",
                    Name = "Test Background"
                }
            ])
            .Build();
        ValidatedRuleset ruleset = Assert.IsType<ValidatedRuleset>(
            ValidatedRuleset.Load(definition).Ruleset);

        bool found = ruleset.TryGetBackground(
            "BACKGROUND.TEST",
            out BackgroundDefinition? background);

        Assert.False(found);
        Assert.Null(background);
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

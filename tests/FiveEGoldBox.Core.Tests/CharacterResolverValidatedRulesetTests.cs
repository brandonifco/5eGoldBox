using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Tests.Builders;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverValidatedRulesetTests
{
    [Fact]
    public void Constructor_WithValidatedRuleset_UsesPrebuiltRulesetIndex()
    {
        RulesetLoadResult loadResult = ValidatedRuleset.Load(TestRulesetBuilder.Default());
        ValidatedRuleset ruleset = Assert.IsType<ValidatedRuleset>(loadResult.Ruleset);

        CharacterDraft draft = new TestCharacterDraftBuilder()
            .WithBackgroundId(null)
            .WithSelectedSkillIds(
            [
                "skill.athletics",
                "skill.perception"
            ])
            .Build();

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Contains("skill.athletics", snapshot.SkillProficiencies);
        Assert.Contains("skill.perception", snapshot.SkillProficiencies);
    }

    [Fact]
    public void Constructor_WithNullValidatedRuleset_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CharacterResolver((ValidatedRuleset)null!));
    }
}

using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Tests.Builders;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverSkillResolutionTests
{
    [Fact]
    public void Resolve_WithSelectedClassSkills_AddsSkillProficienciesToSnapshot()
    {
        RulesetDefinition ruleset = TestRulesetBuilder.Default();

        CharacterDraft draft = new TestCharacterDraftBuilder()
            .WithRaceId("race.human")
            .WithClassId("class.fighter")
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
        Assert.Equal(2, snapshot.SkillProficiencies.Count);
    }

    [Fact]
    public void Resolve_WithoutSelectedClass_LeavesSkillProficienciesEmpty()
    {
        CharacterDraft draft = new TestCharacterDraftBuilder()
            .WithRaceId(null)
            .WithClassId(null)
            .WithBackgroundId(null)
            .WithSelectedSkillIds(
            [
                "skill.athletics",
                "skill.perception"
            ])
            .Build();

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Empty(snapshot.SkillProficiencies);
    }
}

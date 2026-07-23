using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class WatchtowerPartyDefinitionsTests
{
    [Fact]
    public void CreateDraft_WithSoldierBackground_PreservesAuthoredBackground()
    {
        PartyMemberState member = CreateFighter();
        ValidatedRuleset ruleset =
            WatchtowerSignalTestData.CreateRuleset();

        CharacterDraft draft = WatchtowerPartyDefinitions.CreateDraft(
            member,
            ruleset);

        Assert.Equal("background.soldier", draft.BackgroundId);
    }

    [Fact]
    public void CreateDraft_WithNoBackgroundDefinitions_PreservesOptionalBackgroundBehavior()
    {
        PartyMemberState member = CreateFighter();
        ValidatedRuleset ruleset = CreateRulesetWithBackgrounds(
            Array.Empty<BackgroundDefinition>());

        CharacterDraft draft = WatchtowerPartyDefinitions.CreateDraft(
            member,
            ruleset);

        Assert.Null(draft.BackgroundId);
    }

    [Fact]
    public void CreateDraft_WithBackgroundsButNoSoldier_PreservesFailureBehavior()
    {
        PartyMemberState member = CreateFighter();
        ValidatedRuleset ruleset = CreateRulesetWithBackgrounds(
        [
            new BackgroundDefinition
            {
                Id = "background.other",
                Name = "Other Background"
            }
        ]);

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => WatchtowerPartyDefinitions.CreateDraft(
                    member,
                    ruleset));

        Assert.Equal(
            "The bounded party requires background 'background.soldier' when the supplied ruleset defines backgrounds.",
            exception.Message);
    }

    private static PartyMemberState CreateFighter()
    {
        return new PartyMemberState
        {
            PartyMemberId = "party-member.fighter",
            CharacterDefinitionId =
                WatchtowerPartyDefinitions.FighterDefinitionId,
            DisplayName = "Fighter",
            ClassId = WatchtowerPartyDefinitions.FighterClassId,
            ZeroHitPointPolicy =
                CombatantZeroHitPointPolicy.DeathSavingThrows,
            Health = CombatantHealthRules.Create(12),
            Ammunition = null
        };
    }

    private static ValidatedRuleset CreateRulesetWithBackgrounds(
        IReadOnlyList<BackgroundDefinition> backgrounds)
    {
        ValidatedRuleset source =
            WatchtowerSignalTestData.CreateRuleset();
        RulesetDefinition definition = source.Definition with
        {
            Backgrounds = backgrounds
        };
        RulesetLoadResult result = ValidatedRuleset.Load(definition);

        Assert.True(result.IsValid);
        return Assert.IsType<ValidatedRuleset>(result.Ruleset);
    }
}

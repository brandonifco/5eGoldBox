using FiveEGoldBox.Application.Campaigns;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Application.Tests;

/// Turning an authored character into a draft the character pipeline resolves.
/// These carry over from when each build was its own hand-written method; the
/// background behaviour they pin is the part that had real conditional logic.
public sealed class CampaignCharacterDraftFactoryTests
{
    [Fact]
    public void CreateDraft_UsesTheBuildsAuthoredBackground()
    {
        CharacterDraft draft = CampaignCharacterDraftFactory.CreateDraft(
            Fighter(),
            Campaign(),
            WatchtowerSignalTestData.CreateRuleset(),
            1);

        Assert.Equal("background.soldier", draft.BackgroundId);
    }

    /// A ruleset that declares no backgrounds at all leaves the draft without
    /// one, rather than failing.
    [Fact]
    public void CreateDraft_WithNoBackgroundsDeclared_LeavesItUnset()
    {
        CharacterDraft draft = CampaignCharacterDraftFactory.CreateDraft(
            Fighter(),
            Campaign(),
            CreateRulesetWithBackgrounds(
                Array.Empty<BackgroundDefinition>()),
            1);

        Assert.Null(draft.BackgroundId);
    }

    [Fact]
    public void CreateDraft_WithBackgroundsThatOmitTheBuilds_Throws()
    {
        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                CampaignCharacterDraftFactory.CreateDraft(
                    Fighter(),
                    Campaign(),
                    CreateRulesetWithBackgrounds(
                    [
                        new BackgroundDefinition
                        {
                            Id = "background.other",
                            Name = "Other Background"
                        }
                    ]),
                    1));

        Assert.Contains("background.soldier", exception.Message);
    }

    [Fact]
    public void CreateDraft_ForSomeoneNotOnTheRoster_Throws()
    {
        PartyMemberState stranger = Fighter() with
        {
            CharacterDefinitionId = "character.stranger"
        };

        Assert.Throws<InvalidOperationException>(() =>
            CampaignCharacterDraftFactory.CreateDraft(
                stranger,
                Campaign(),
                WatchtowerSignalTestData.CreateRuleset(),
                1));
    }

    /// The draft carries what the character has now, not what it started with:
    /// a quiver spent down to nothing leaves no arrows in inventory.
    [Fact]
    public void CreateDraft_CarriesRemainingAmmunitionRatherThanTheAuthoredAmount()
    {
        PartyMemberState archer = Assert.Single(
            CampaignPartyFactory.CreateStartingParty(Campaign()).Members,
            member => member.Ammunition is not null);

        CharacterDraft full = CampaignCharacterDraftFactory.CreateDraft(
            archer,
            Campaign(),
            WatchtowerSignalTestData.CreateRuleset(),
            1);
        CharacterDraft spent = CampaignCharacterDraftFactory.CreateDraft(
            archer with
            {
                Ammunition = archer.Ammunition! with { RemainingQuantity = 0 }
            },
            Campaign(),
            WatchtowerSignalTestData.CreateRuleset(),
            1);

        Assert.Equal(
            archer.Ammunition!.RemainingQuantity,
            Assert.Single(full.InventoryItems).Quantity);
        Assert.Empty(spent.InventoryItems);
    }

    /// The party's own current level always wins, both for a roster
    /// character (which has no level of its own to conflict with) and a
    /// custom build (whose own CustomBuild.Level -- always 1, since that is
    /// all player creation permits today -- must not silently override it).
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void CreateDraft_UsesThePartysCurrentLevel_ForARosterCharacter(
        int level)
    {
        CharacterDraft draft = CampaignCharacterDraftFactory.CreateDraft(
            Fighter(),
            Campaign(),
            WatchtowerSignalTestData.CreateRuleset(),
            level);

        Assert.Equal(level, draft.Level);
    }

    [Fact]
    public void CreateDraft_UsesThePartysCurrentLevel_ForACustomBuild()
    {
        PartyMemberState fighter = Fighter();
        PartyMemberState customBuilt = fighter with
        {
            CustomBuild = new CharacterDraft
            {
                Name = fighter.DisplayName,
                Level = 1,
                ClassId = fighter.ClassId,
                AbilityScoreGenerationMethod =
                    AbilityScoreGenerationMethod.StandardArray
            }
        };

        CharacterDraft draft = CampaignCharacterDraftFactory.CreateDraft(
            customBuilt,
            Campaign(),
            WatchtowerSignalTestData.CreateRuleset(),
            2);

        Assert.Equal(2, draft.Level);
    }

    private static CampaignDefinition Campaign()
    {
        return CampaignRegistry.Resolve(
            FrontierCampaignIds.CampaignId);
    }

    private static PartyMemberState Fighter()
    {
        return CampaignPartyFactory.CreateStartingParty(Campaign())
            .Members[0];
    }

    private static ValidatedRuleset CreateRulesetWithBackgrounds(
        IReadOnlyList<BackgroundDefinition> backgrounds)
    {
        RulesetDefinition definition =
            WatchtowerSignalTestData.CreateRuleset().Definition with
            {
                Backgrounds = backgrounds
            };
        RulesetLoadResult result = ValidatedRuleset.Load(definition);

        Assert.True(result.IsValid);
        return Assert.IsType<ValidatedRuleset>(result.Ruleset);
    }
}

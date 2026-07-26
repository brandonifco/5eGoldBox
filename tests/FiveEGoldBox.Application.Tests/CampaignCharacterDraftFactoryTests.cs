using FiveEGoldBox.Application.Campaigns;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;

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
            WatchtowerSignalTestData.CreateRuleset());

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
                Array.Empty<BackgroundDefinition>()));

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
                    ])));

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
                WatchtowerSignalTestData.CreateRuleset()));
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
            WatchtowerSignalTestData.CreateRuleset());
        CharacterDraft spent = CampaignCharacterDraftFactory.CreateDraft(
            archer with
            {
                Ammunition = archer.Ammunition! with { RemainingQuantity = 0 }
            },
            Campaign(),
            WatchtowerSignalTestData.CreateRuleset());

        Assert.Equal(
            archer.Ammunition!.RemainingQuantity,
            Assert.Single(full.InventoryItems).Quantity);
        Assert.Empty(spent.InventoryItems);
    }

    private static CampaignDefinition Campaign()
    {
        return CampaignRegistry.Resolve(
            FrontierCampaignContent.CampaignId);
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

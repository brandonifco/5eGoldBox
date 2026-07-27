using System.Text.RegularExpressions;
using FiveEGoldBox.Application.Campaigns;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Persistence;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Application.Tests;

/// Spell slots as something a character carries between encounters — the same
/// shape as the Ranger's arrows, and deliberately not spell-shaped, so Second
/// Wind and Rage uses need no second mechanism.
public sealed class CharacterResourceTests
{
    [Fact]
    public void SlotResourceIds_AreBuiltInOnePlace()
    {
        Assert.Equal("resource.spell-slot.1", SpellSlotResources.ForLevel(1));
        Assert.True(SpellSlotResources.IsSpellSlot(
            SpellSlotResources.ForLevel(2)));
        Assert.False(SpellSlotResources.IsSpellSlot("resource.second-wind"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SpellSlotResources.ForLevel(0));
    }

    /// Two of the four cast and two do not, which is the whole point of
    /// resources being granted by class rather than handed to everybody.
    [Fact]
    public void OnlyTheCastersInThePartyCarrySlots()
    {
        IReadOnlyList<PartyMemberState> members =
            CampaignPartyFactory.CreateStartingParty(Campaign())
                .Members;

        Assert.Equal(
            2,
            members.Count(member => member.Resources.Count > 0));
        Assert.All(
            members.Where(member => member.Resources.Count > 0),
            member => Assert.Equal(
                SpellSlotResources.ForLevel(1),
                Assert.Single(member.Resources).ResourceId));
        Assert.All(
            members.Where(member =>
                member.ClassId == "class.fighter"
                || member.ClassId == "class.rogue"),
            member => Assert.Empty(member.Resources));
    }

    /// Slots come from the class, the way hit points come from the hit die.
    [Fact]
    public void ACasterStartsWithTheSlotsItsClassGrants()
    {
        PartyMemberState caster = Assert.Single(
            CampaignPartyFactory.CreateStartingParty(
                CampaignWithACleric()).Members);

        CharacterResourceState slot = Assert.Single(caster.Resources);
        Assert.Equal(SpellSlotResources.ForLevel(1), slot.ResourceId);
        Assert.Equal(2, slot.Maximum);
        Assert.Equal(2, slot.Remaining);
    }

    [Fact]
    public void TheClericAndWizardBothCastButWithDifferentAbilities()
    {
        IReadOnlyList<ClassDefinition> classes = CampaignRulesetContent
            .CreateRulesetDefinition()
            .Classes;

        ClassDefinition cleric = Assert.Single(
            classes,
            candidate => candidate.Id == CampaignRulesetContent.ClericClassId);
        ClassDefinition wizard = Assert.Single(
            classes,
            candidate => candidate.Id == CampaignRulesetContent.WizardClassId);

        Assert.Equal(Ability.Wisdom, cleric.SpellcastingAbility);
        Assert.Equal(Ability.Intelligence, wizard.SpellcastingAbility);
        Assert.Equal(2, cleric.SpellSlotsByLevel[1]);
        Assert.Equal(2, wizard.SpellSlotsByLevel[1]);

        // Every other class casts nothing.
        Assert.All(
            classes.Where(candidate =>
                candidate.Id != CampaignRulesetContent.ClericClassId
                && candidate.Id != CampaignRulesetContent.WizardClassId),
            candidate =>
            {
                Assert.Null(candidate.SpellcastingAbility);
                Assert.Empty(candidate.SpellSlotsByLevel);
            });
    }

    [Theory]
    [InlineData(-1, 2)]
    [InlineData(3, 2)]
    [InlineData(0, 0)]
    public void Validate_RejectsAResourceOutsideItsOwnRange(
        int remaining,
        int maximum)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            ApplicationSessionRules.Validate(
                WithResources(
                [
                    new CharacterResourceState
                    {
                        ResourceId = SpellSlotResources.ForLevel(1),
                        Remaining = remaining,
                        Maximum = maximum
                    }
                ])));
    }

    [Fact]
    public void Validate_RejectsTheSameResourceTwice()
    {
        CharacterResourceState slot = new()
        {
            ResourceId = SpellSlotResources.ForLevel(1),
            Remaining = 1,
            Maximum = 2
        };

        Assert.ThrowsAny<ArgumentException>(() =>
            ApplicationSessionRules.Validate(WithResources([slot, slot])));
    }

    /// The engine checks a resource is coherent; the campaign checks it is one
    /// the build actually grants.
    [Fact]
    public void Validate_RejectsAResourceTheBuildDoesNotGrant()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            ApplicationSessionRules.Validate(
                WithResources(
                [
                    new CharacterResourceState
                    {
                        ResourceId = SpellSlotResources.ForLevel(1),
                        Remaining = 2,
                        Maximum = 2
                    }
                ])));
    }

    [Fact]
    public void Validate_RejectsACasterMissingItsSlots()
    {
        PartyState party = CampaignPartyFactory.CreateStartingParty(
            CampaignWithACleric());
        PartyMemberState[] members = party.Members.ToArray();
        members[0] = members[0] with
        {
            Resources = Array.Empty<CharacterResourceState>()
        };

        Assert.ThrowsAny<ArgumentException>(() =>
            CampaignPartyCompositionValidator.Validate(
                CampaignWithACleric(),
                party with { Members = Array.AsReadOnly(members) }));
    }

    /// How many slots are left is play, not composition.
    [Fact]
    public void Validate_AcceptsSlotsSpentDown()
    {
        PartyState party = CampaignPartyFactory.CreateStartingParty(
            CampaignWithACleric());
        PartyMemberState[] members = party.Members.ToArray();
        members[0] = members[0] with
        {
            Resources =
            [
                members[0].Resources[0] with { Remaining = 0 }
            ]
        };

        CampaignPartyCompositionValidator.Validate(
            CampaignWithACleric(),
            party with { Members = Array.AsReadOnly(members) });
    }

    /// The save format has always treated `Resources` as optional, and it
    /// still parses a document without it. What has changed is that such a
    /// document no longer describes a *valid* session: two of the four
    /// characters are casters, and a caster without the slots its class grants
    /// is not a party this campaign could have raised.
    ///
    /// Well-formed data describing an invalid session, which is the same call
    /// PR #107 made for an unrecognised progress marker.
    [Fact]
    public void Deserialize_ASaveWrittenBeforeResourcesExisted_IsNowInvalid()
    {
        // Written by stripping the field out of a real save, because the
        // serializer will no longer produce a caster without slots.
        string saved = ManualSaveSerializer.Serialize(
            ScenarioSessionFactory.CreateNew(
                WatchtowerScenarioContent.ScenarioId,
                randomSeed: 9));
        string withoutResources = Regex.Replace(
            saved,
            "\"Resources\"\\s*:\\s*\\[[^\\]]*\\]",
            "\"Resources\":[]");

        Assert.DoesNotContain("\"ResourceId\"", withoutResources);

        ManualSaveLoadResult result =
            ManualSaveSerializer.Deserialize(withoutResources);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ManualSaveLoadFailureReason.InvalidSessionState,
            result.FailureReason);
    }

    private static ApplicationSessionState WithResources(
        IReadOnlyList<CharacterResourceState> resources)
    {
        ApplicationSessionState session = ScenarioSessionFactory.CreateNew(
            WatchtowerScenarioContent.ScenarioId,
            randomSeed: 9);
        PartyMemberState[] members = session.Party.Members.ToArray();
        members[0] = members[0] with { Resources = resources };

        return session with
        {
            Party = session.Party with
            {
                Members = Array.AsReadOnly(members)
            }
        };
    }

    private static CampaignDefinition Campaign()
    {
        return CampaignRegistry.Resolve(FrontierCampaignContent.CampaignId);
    }

    /// A campaign of one Cleric, so the mechanism is exercised against real
    /// ruleset content without changing the roster the game actually plays.
    private static CampaignDefinition CampaignWithACleric()
    {
        CampaignDefinition campaign = Campaign();

        return campaign with
        {
            ActivePartySize = 1,
            Roster =
            [
                campaign.Roster[0] with
                {
                    PartyMemberId = "party-member.cleric",
                    CharacterDefinitionId = "character.cleric",
                    DisplayName = "Cleric",
                    ClassId = CampaignRulesetContent.ClericClassId,
                    PreparedSpellIds =
                    [
                        CampaignRulesetContent.CureWoundsId,
                        CampaignRulesetContent.HealingWordId
                    ]
                }
            ]
        };
    }
}

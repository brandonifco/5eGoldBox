using FiveEGoldBox.Application.Campaigns;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;

namespace FiveEGoldBox.Application.Tests;

/// The campaign is the thing three placeholders were standing in for: a
/// roster, a party size, a ruleset, and the adventures it contains.
public sealed class CampaignDefinitionTests
{
    [Fact]
    public void Resolve_ValidatesAndCachesTheCampaign()
    {
        CampaignDefinition campaign = CampaignRegistry.Resolve(
            FrontierCampaignContent.CampaignId);

        Assert.Equal(FrontierCampaignContent.CampaignId, campaign.CampaignId);
        Assert.Same(
            campaign,
            CampaignRegistry.Resolve(FrontierCampaignContent.CampaignId));
    }

    /// A campaign names its scenarios; the reverse lookup is derived from that
    /// rather than declared twice and left to drift.
    [Theory]
    [InlineData(WatchtowerScenarioContent.ScenarioId)]
    [InlineData(SunkenChapelScenarioDefinitionProvider.ScenarioId)]
    public void ResolveForScenario_FindsTheCampaignThatContainsIt(
        string scenarioId)
    {
        Assert.Equal(
            FrontierCampaignContent.CampaignId,
            CampaignRegistry.ResolveForScenario(scenarioId).CampaignId);
    }

    [Fact]
    public void ResolveForScenario_WithAnUnownedScenario_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CampaignRegistry.ResolveForScenario("scenario.not-in-a-campaign"));
    }

    /// Both scenarios draw the same party, because they belong to the same
    /// campaign. That is the point: a roster is not scenario content.
    [Fact]
    public void BothScenariosStartWithTheSameParty()
    {
        PartyState watchtower = ScenarioSessionFactory.CreateNew(
            WatchtowerScenarioContent.ScenarioId,
            randomSeed: 3).Party;
        PartyState chapel = ScenarioSessionFactory.CreateNew(
            SunkenChapelScenarioDefinitionProvider.ScenarioId,
            randomSeed: 3).Party;

        Assert.Equal(
            watchtower.Members.Select(member => member.PartyMemberId),
            chapel.Members.Select(member => member.PartyMemberId));
    }

    [Fact]
    public void StartingParty_FieldsExactlyTheActivePartySize()
    {
        CampaignDefinition campaign = CampaignRegistry.Resolve(
            FrontierCampaignContent.CampaignId);

        PartyState party =
            CampaignPartyFactory.CreateStartingParty(campaign);

        Assert.Equal(campaign.ActivePartySize, party.Members.Count);
        Assert.All(party.Members, member =>
            Assert.Contains(
                campaign.Roster,
                character => character.CharacterDefinitionId
                    == member.CharacterDefinitionId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_RejectsACampaignThatFieldsNobody(
        int activePartySize)
    {
        Assert.Throws<ArgumentException>(() =>
            CampaignDefinitionValidator.Validate(
                Base() with { ActivePartySize = activePartySize }));
    }

    [Fact]
    public void Validate_RejectsFieldingMoreThanTheRosterHolds()
    {
        Assert.Throws<ArgumentException>(() =>
            CampaignDefinitionValidator.Validate(
                Base() with { ActivePartySize = 99 }));
    }

    [Fact]
    public void Validate_RejectsACampaignWithNoScenarios()
    {
        Assert.Throws<ArgumentException>(() =>
            CampaignDefinitionValidator.Validate(
                Base() with { ScenarioIds = Array.Empty<string>() }));
    }

    [Fact]
    public void Validate_RejectsADuplicatedRosterIdentity()
    {
        CampaignDefinition campaign = Base();

        Assert.Throws<ArgumentException>(() =>
            CampaignDefinitionValidator.Validate(
                campaign with
                {
                    Roster =
                    [
                        campaign.Roster[0],
                        campaign.Roster[0] with
                        {
                            PartyMemberId = "party-member.other"
                        }
                    ]
                }));
    }

    /// Ammunition belongs to a weapon the character carries, or nothing spends
    /// it.
    [Fact]
    public void Validate_RejectsAmmunitionForAWeaponNotWielded()
    {
        CampaignDefinition campaign = Base();
        // More than one character on the roster carries a bow now, and any of
        // them proves the rule.
        CampaignCharacterDefinition archer = campaign.Roster
            .First(character => character.Ammunition is not null);

        Assert.Throws<ArgumentException>(() =>
            CampaignDefinitionValidator.Validate(
                campaign with
                {
                    Roster =
                    [
                        archer with
                        {
                            EquippedWeaponIds = ["weapon.greataxe"]
                        }
                    ],
                    ActivePartySize = 1
                }));
    }

    [Fact]
    public void Validate_RejectsStartingHealthOutsideItsOwnRange()
    {
        CampaignDefinition campaign = Base();

        Assert.Throws<ArgumentException>(() =>
            CampaignDefinitionValidator.Validate(
                campaign with
                {
                    Roster =
                    [
                        campaign.Roster[0] with
                        {
                            CurrentHitPoints =
                                campaign.Roster[0].MaximumHitPoints + 1
                        }
                    ],
                    ActivePartySize = 1
                }));
    }

    private static CampaignDefinition Base()
    {
        return FrontierCampaignContent.CreateDefinition();
    }
}

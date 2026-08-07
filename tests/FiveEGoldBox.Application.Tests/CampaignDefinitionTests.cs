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
            FrontierCampaignIds.CampaignId);

        Assert.Equal(FrontierCampaignIds.CampaignId, campaign.CampaignId);
        Assert.Same(
            campaign,
            CampaignRegistry.Resolve(FrontierCampaignIds.CampaignId));
    }

    /// A campaign names its scenarios; the reverse lookup is derived from that
    /// rather than declared twice and left to drift.
    [Theory]
    [InlineData(WatchtowerScenarioContent.ScenarioId)]
    [InlineData(SunkenChapelScenarioIds.ScenarioId)]
    public void ResolveForScenario_FindsTheCampaignThatContainsIt(
        string scenarioId)
    {
        Assert.Equal(
            FrontierCampaignIds.CampaignId,
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
            SunkenChapelScenarioIds.ScenarioId,
            randomSeed: 3).Party;

        Assert.Equal(
            watchtower.Members.Select(member => member.PartyMemberId),
            chapel.Members.Select(member => member.PartyMemberId));
    }

    [Fact]
    public void StartingParty_FieldsExactlyTheActivePartySize()
    {
        CampaignDefinition campaign = CampaignRegistry.Resolve(
            FrontierCampaignIds.CampaignId);

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

    // ----- Cross-pack checks against the campaign's own ruleset -----

    /// The ruleset is optional, and a null one skips these checks entirely
    /// rather than failing every id -- an unresolvable ruleset means there is
    /// nothing to check against. Every other test in this file relies on
    /// that, since none of them pass one.
    [Fact]
    public void Validate_WithoutARulesetSkipsCrossPackChecksEntirely()
    {
        CampaignDefinition campaign = Base();

        CampaignDefinitionValidator.Validate(
            campaign with
            {
                Roster = ReplaceFirst(
                    campaign,
                    campaign.Roster[0] with { ClassId = "class.does-not-exist" })
            });
    }

    [Theory]
    [InlineData("RaceId", "race.does-not-exist")]
    [InlineData("ClassId", "class.does-not-exist")]
    [InlineData("BackgroundId", "background.does-not-exist")]
    public void Validate_RejectsARosterEntryNamingAnIdTheRulesetDoesNotDefine(
        string field,
        string unknownId)
    {
        CampaignDefinition campaign = Base();
        CampaignCharacterDefinition original = campaign.Roster[0];

        CampaignCharacterDefinition broken = field switch
        {
            "RaceId" => original with { RaceId = unknownId },
            "ClassId" => original with { ClassId = unknownId },
            _ => original with { BackgroundId = unknownId }
        };

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            CampaignDefinitionValidator.Validate(
                campaign with { Roster = ReplaceFirst(campaign, broken) },
                Ruleset(campaign)));

        Assert.Contains(unknownId, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_RejectsAPreparedSpellTheRulesetDoesNotDefine()
    {
        CampaignDefinition campaign = Base();

        // The Cleric is the roster's spellcaster, so this exercises a real
        // non-empty PreparedSpellIds rather than a synthetic one.
        CampaignCharacterDefinition caster = campaign.Roster
            .First(character => character.PreparedSpellIds.Count > 0);

        CampaignCharacterDefinition broken = caster with
        {
            PreparedSpellIds = [.. caster.PreparedSpellIds, "spell.does-not-exist"]
        };

        Assert.Throws<ArgumentException>(() =>
            CampaignDefinitionValidator.Validate(
                campaign with
                {
                    Roster = campaign.Roster
                        .Select(character => character == caster ? broken : character)
                        .ToArray()
                },
                Ruleset(campaign)));
    }

    [Fact]
    public void Validate_RejectsAmmunitionMadeOfAnItemTheRulesetDoesNotDefine()
    {
        CampaignDefinition campaign = Base();

        CampaignCharacterDefinition archer = campaign.Roster
            .First(character => character.Ammunition is not null);

        CampaignCharacterDefinition broken = archer with
        {
            Ammunition = archer.Ammunition! with { AmmunitionItemId = "item.does-not-exist" }
        };

        Assert.Throws<ArgumentException>(() =>
            CampaignDefinitionValidator.Validate(
                campaign with
                {
                    Roster = campaign.Roster
                        .Select(character => character == archer ? broken : character)
                        .ToArray()
                },
                Ruleset(campaign)));
    }

    /// The real committed campaign has to survive its own new checks --
    /// otherwise this change would have broken the shipped content rather
    /// than protected it.
    [Fact]
    public void Validate_AcceptsTheRealCampaignAgainstItsOwnRuleset()
    {
        CampaignDefinition campaign = Base();

        CampaignDefinitionValidator.Validate(campaign, Ruleset(campaign));
    }

    private static CampaignCharacterDefinition[] ReplaceFirst(
        CampaignDefinition campaign,
        CampaignCharacterDefinition replacement)
    {
        return campaign.Roster
            .Select((character, index) => index == 0 ? replacement : character)
            .ToArray();
    }

    private static Core.Definitions.ValidatedRuleset Ruleset(
        CampaignDefinition campaign)
    {
        return RulesetRegistry.Resolve(campaign.RulesetId);
    }

    private static CampaignDefinition Base()
    {
        return CampaignRegistry.Resolve(FrontierCampaignIds.CampaignId);
    }
}

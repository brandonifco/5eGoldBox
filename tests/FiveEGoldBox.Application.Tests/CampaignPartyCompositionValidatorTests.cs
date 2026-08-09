using FiveEGoldBox.Application.Campaigns;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Application.Tests;

/// A party is checked against the campaign that raised it. These cover what a
/// campaign demands of its own party; whether a party is internally coherent -
/// unique identities, sane health, well-formed ammunition - is the engine's,
/// and ApplicationSessionRules checks it for every party alike.
public sealed class CampaignPartyCompositionValidatorTests
{
    [Fact]
    public void Validate_AcceptsTheCampaignsOwnStartingParty()
    {
        CampaignPartyCompositionValidator.Validate(
            Campaign(),
            CreateParty());
    }

    [Fact]
    public void Validate_RejectsAPartyOfTheWrongSize()
    {
        PartyState party = CreateParty();

        Assert.Throws<ArgumentException>(() =>
            CampaignPartyCompositionValidator.Validate(
                Campaign(),
                party with
                {
                    Members = Array.AsReadOnly(
                        party.Members.Take(2).ToArray())
                }));
        Assert.Throws<ArgumentException>(() =>
            CampaignPartyCompositionValidator.Validate(
                Campaign(),
                party with
                {
                    Members = Array.AsReadOnly(
                        party.Members.Append(party.Members[0]).ToArray())
                }));
    }

    [Fact]
    public void Validate_RejectsSomeoneNotOnTheRoster()
    {
        PartyState party = ReplaceFirstMember(
            CreateParty(),
            member => member with
            {
                CharacterDefinitionId = "character.stranger"
            });

        Assert.Throws<ArgumentException>(() =>
            CampaignPartyCompositionValidator.Validate(Campaign(), party));
    }

    /// The roster says what each character is. A member claiming a different
    /// class than its own build is not a party this campaign could produce.
    [Fact]
    public void Validate_RejectsAMemberWhoseClassContradictsItsBuild()
    {
        PartyState party = ReplaceFirstMember(
            CreateParty(),
            member => member with { ClassId = "class.wizard" });

        Assert.Throws<ArgumentException>(() =>
            CampaignPartyCompositionValidator.Validate(Campaign(), party));
    }

    [Fact]
    public void Validate_RejectsMaximumHitPointsTheBuildDoesNotGive()
    {
        PartyState party = ReplaceFirstMember(
            CreateParty(),
            member => member with
            {
                Health = member.Health with
                {
                    HitPoints = member.Health.HitPoints with
                    {
                        MaximumHitPoints =
                            member.Health.HitPoints.MaximumHitPoints + 1
                    }
                }
            });

        Assert.Throws<ArgumentException>(() =>
            CampaignPartyCompositionValidator.Validate(Campaign(), party));
    }

    /// A level-2 party's members are expected to carry level-2 hit points,
    /// not the level-1 baseline the roster authors -- staying at the level-1
    /// number once the party has advanced is exactly as wrong as claiming
    /// more than the build gives at level 1 (the test just above this one).
    [Fact]
    public void Validate_RejectsLevel1HitPointsOnALevel2Party()
    {
        PartyState party = CreateParty() with { Level = 2 };

        Assert.Throws<ArgumentException>(() =>
            CampaignPartyCompositionValidator.Validate(Campaign(), party));
    }

    /// The positive case: every member's hit points bumped by exactly the
    /// delta CharacterResolver says levels 1-to-2 are worth for their own
    /// class and Constitution is a party this campaign could have produced.
    [Fact]
    public void Validate_AcceptsCorrectLevel2HitPoints()
    {
        CampaignDefinition campaign = Campaign();
        ValidatedRuleset ruleset = RulesetRegistry.Resolve(campaign.RulesetId);
        PartyState party = CreateParty();

        PartyMemberState[] leveled = party.Members
            .Select(member =>
            {
                int delta =
                    CampaignCharacterDraftFactory.GetHitPointDeltaSinceLevelOne(
                        member,
                        campaign,
                        ruleset,
                        2);

                return member with
                {
                    Health = member.Health with
                    {
                        HitPoints = member.Health.HitPoints with
                        {
                            MaximumHitPoints =
                                member.Health.HitPoints.MaximumHitPoints
                                    + delta,
                            CurrentHitPoints =
                                member.Health.HitPoints.CurrentHitPoints
                                    + delta
                        }
                    }
                };
            })
            .ToArray();

        CampaignPartyCompositionValidator.Validate(
            campaign,
            party with { Members = Array.AsReadOnly(leveled), Level = 2 });
    }

    [Fact]
    public void Validate_RejectsAmmunitionTheBuildDoesNotGrant()
    {
        PartyState party = ReplaceFirstMember(
            CreateParty(),
            member => member with
            {
                Ammunition = new AmmunitionState
                {
                    WeaponId = "weapon.longbow",
                    AmmunitionItemId = "item.arrow",
                    RemainingQuantity = 3
                }
            });

        Assert.Throws<ArgumentException>(() =>
            CampaignPartyCompositionValidator.Validate(Campaign(), party));
    }

    [Fact]
    public void Validate_RejectsAMissingQuiver()
    {
        PartyState party = CreateParty();
        PartyMemberState[] members = party.Members.ToArray();
        int archer = Array.FindIndex(
            members,
            member => member.Ammunition is not null);

        members[archer] = members[archer] with { Ammunition = null };

        Assert.Throws<ArgumentException>(() =>
            CampaignPartyCompositionValidator.Validate(
                Campaign(),
                party with { Members = Array.AsReadOnly(members) }));
    }

    /// How much is left is play, not composition.
    [Fact]
    public void Validate_AcceptsAQuiverSpentDown()
    {
        PartyState party = CreateParty();
        PartyMemberState[] members = party.Members.ToArray();
        int archer = Array.FindIndex(
            members,
            member => member.Ammunition is not null);

        members[archer] = members[archer] with
        {
            Ammunition = members[archer].Ammunition! with
            {
                RemainingQuantity = 0
            }
        };

        CampaignPartyCompositionValidator.Validate(
            Campaign(),
            party with { Members = Array.AsReadOnly(members) });
    }

    private static CampaignDefinition Campaign()
    {
        return CampaignRegistry.Resolve(
            FrontierCampaignIds.CampaignId);
    }

    private static PartyState CreateParty()
    {
        return CampaignPartyFactory.CreateStartingParty(Campaign());
    }

    private static PartyState ReplaceFirstMember(
        PartyState party,
        Func<PartyMemberState, PartyMemberState> replace)
    {
        PartyMemberState[] members = party.Members.ToArray();
        members[0] = replace(members[0]);

        return party with { Members = Array.AsReadOnly(members) };
    }
}

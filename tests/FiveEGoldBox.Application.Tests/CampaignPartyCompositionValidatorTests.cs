using FiveEGoldBox.Application.Campaigns;
using FiveEGoldBox.Application.Parties;

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

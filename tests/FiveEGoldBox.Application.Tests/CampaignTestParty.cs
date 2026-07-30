using FiveEGoldBox.Application.Campaigns;
using FiveEGoldBox.Application.Parties;

namespace FiveEGoldBox.Application.Tests;

/// The party the campaign actually fields.
///
/// Every test that needed a valid party used to restate the roster in
/// literals, so each of them broke when the roster changed — which is
/// precisely what a campaign-declared roster was supposed to stop. Tests that
/// build a party by hand still do, because they are testing what happens to a
/// *malformed* one; this is for the ones that only need a party that passes.
internal static class CampaignTestParty
{
    internal static PartyState Create()
    {
        return CampaignPartyFactory.CreateStartingParty(
            CampaignRegistry.Resolve(
                FrontierCampaignIds.CampaignId));
    }

    internal static PartyMemberState[] CreateMembers()
    {
        return Create().Members.ToArray();
    }

    /// Where the member carrying ammunition sits. Found rather than hardcoded,
    /// because which character holds the bow is a roster decision — it was the
    /// third member and a ranger, and is now the second and a rogue.
    internal static int ArcherIndex()
    {
        PartyMemberState[] members = CreateMembers();

        return Array.FindIndex(
            members,
            member => member.Ammunition is not null);
    }
}

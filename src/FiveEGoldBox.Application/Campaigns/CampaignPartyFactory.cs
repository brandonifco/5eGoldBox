using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Campaigns;

/// Builds the party a campaign starts with.
internal static class CampaignPartyFactory
{
    internal static PartyState CreateStartingParty(
        CampaignDefinition campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);

        return new PartyState
        {
            PartyId = FrontierCampaignIds.PartyId,
            Members = campaign.Roster
                .Take(campaign.ActivePartySize)
                .Select(character => ToMember(character, campaign.RulesetId))
                .ToArray()
        };
    }

    private static PartyMemberState ToMember(
        CampaignCharacterDefinition character,
        string rulesetId)
    {
        return new PartyMemberState
        {
            PartyMemberId = character.PartyMemberId,
            CharacterDefinitionId = character.CharacterDefinitionId,
            DisplayName = character.DisplayName,
            ClassId = character.ClassId,
            ZeroHitPointPolicy = character.ZeroHitPointPolicy,
            Health = new CombatantHealthState
            {
                HitPoints = new HitPointState
                {
                    MaximumHitPoints = character.MaximumHitPoints,
                    CurrentHitPoints = character.CurrentHitPoints,
                    TemporaryHitPoints = character.TemporaryHitPoints
                },
                DeathSavingThrows = new DeathSavingThrowState
                {
                    SuccessCount = 0,
                    FailureCount = 0,
                    IsStable = false
                },
                IsInstantlyDead = false
            },
            Ammunition = character.Ammunition is null
                ? null
                : new AmmunitionState
                {
                    WeaponId = character.Ammunition.WeaponId,
                    AmmunitionItemId = character.Ammunition.AmmunitionItemId,
                    RemainingQuantity = character.Ammunition.Quantity
                },
            Resources = CreateStartingResources(character, rulesetId)
        };
    }

    /// Slots come from the class, the way hit points come from the hit die. A
    /// character starts rested, so everything is full.
    ///
    /// A campaign's starting party is a level-one party -- PartyState.Level
    /// defaults there and nothing has fought yet -- so this asks for the
    /// grants at that level rather than reading the class's slot table
    /// directly. Going through CampaignResourceGrants is what keeps this from
    /// being a second implementation of "which table applies," which the
    /// validator would then have to agree with by coincidence.
    private static IReadOnlyList<CharacterResourceState> CreateStartingResources(
        CampaignCharacterDefinition character,
        string rulesetId)
    {
        return CampaignResourceGrants
            .ForClass(rulesetId, character.ClassId, AdvancementRules.MinimumLevel)
            .OrderBy(grant => grant.Key, StringComparer.Ordinal)
            .Select(grant => new CharacterResourceState
            {
                ResourceId = grant.Key,
                Remaining = grant.Value,
                Maximum = grant.Value
            })
            .ToArray();
    }
}

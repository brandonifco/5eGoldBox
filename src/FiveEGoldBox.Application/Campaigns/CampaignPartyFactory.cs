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
            PartyId = FrontierCampaignContent.StartingPartyId,
            Members = campaign.Roster
                .Take(campaign.ActivePartySize)
                .Select(ToMember)
                .ToArray()
        };
    }

    private static PartyMemberState ToMember(
        CampaignCharacterDefinition character)
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
                }
        };
    }
}

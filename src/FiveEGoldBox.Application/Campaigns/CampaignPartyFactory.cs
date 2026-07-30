using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Core.Definitions;
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

        ValidatedRuleset ruleset =
            RulesetRegistry.Resolve(campaign.RulesetId);

        return new PartyState
        {
            PartyId = FrontierCampaignIds.PartyId,
            Members = campaign.Roster
                .Take(campaign.ActivePartySize)
                .Select(character => ToMember(character, ruleset))
                .ToArray()
        };
    }

    private static PartyMemberState ToMember(
        CampaignCharacterDefinition character,
        ValidatedRuleset ruleset)
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
            Resources = CreateStartingResources(character, ruleset)
        };
    }

    /// Slots come from the class, the way hit points come from the hit die. A
    /// character starts rested, so everything is full.
    private static IReadOnlyList<CharacterResourceState> CreateStartingResources(
        CampaignCharacterDefinition character,
        ValidatedRuleset ruleset)
    {
        ClassDefinition? characterClass = ruleset.Definition.Classes
            .FirstOrDefault(candidate => string.Equals(
                candidate.Id,
                character.ClassId,
                StringComparison.Ordinal));

        if (characterClass is null
            || characterClass.SpellSlotsByLevel.Count == 0)
        {
            return Array.Empty<CharacterResourceState>();
        }

        return characterClass.SpellSlotsByLevel
            .OrderBy(slots => slots.Key)
            .Select(slots => new CharacterResourceState
            {
                ResourceId = SpellSlotResources.ForLevel(slots.Key),
                Remaining = slots.Value,
                Maximum = slots.Value
            })
            .ToArray();
    }
}

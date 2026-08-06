using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Application.Campaigns;

/// Turns an authored character into the draft the character pipeline resolves.
///
/// There used to be one of these per class, each asserting that the member it
/// was handed really was the class it expected. That check is gone because the
/// thing it guarded against is: the build is looked up by the identifier the
/// member carries, so it cannot be resolved as a different character's.
internal static class CampaignCharacterDraftFactory
{
    internal static CharacterDraft CreateDraft(
        PartyMemberState member,
        CampaignDefinition campaign,
        ValidatedRuleset ruleset)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(ruleset);

        if (member.CustomBuild is not null)
        {
            return member.CustomBuild with
            {
                InventoryItems = MergeCustomBuildAmmunition(member)
            };
        }

        CampaignCharacterDefinition character = campaign.Roster
            .FirstOrDefault(candidate => string.Equals(
                candidate.CharacterDefinitionId,
                member.CharacterDefinitionId,
                StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Character definition '{member.CharacterDefinitionId}' is not part of campaign '{campaign.CampaignId}'.");

        return new CharacterDraft
        {
            Name = member.DisplayName,
            Level = 1,
            RaceId = character.RaceId,
            ClassId = character.ClassId,
            BackgroundId = ResolveBackgroundId(character, ruleset),
            AbilityScoreGenerationMethod =
                AbilityScoreGenerationMethod.Manual,
            BaseAbilityScores = character.AbilityScores,
            SelectedSkillIds = character.SelectedSkillIds,
            EquippedWeaponIds = character.EquippedWeaponIds,
            PreparedSpellIds = character.PreparedSpellIds,
            InventoryItems = CreateInventory(member)
        };
    }

    /// Same idea as CreateInventory below, but starting from the build's own
    /// declared inventory (which CreateInventory has nothing to start from,
    /// since a roster character's Ammunition is its only starting inventory)
    /// rather than replacing it outright: the live AmmunitionState still wins
    /// over whatever quantity the build was created with, since it reflects
    /// what has actually been spent.
    private static IReadOnlyList<InventoryItemDraft> MergeCustomBuildAmmunition(
        PartyMemberState member)
    {
        IReadOnlyList<InventoryItemDraft> baseItems =
            member.CustomBuild!.InventoryItems;

        if (member.Ammunition is null)
        {
            return baseItems;
        }

        List<InventoryItemDraft> merged = baseItems
            .Where(item => !string.Equals(
                item.ItemId,
                member.Ammunition.AmmunitionItemId,
                StringComparison.Ordinal))
            .ToList();

        if (member.Ammunition.RemainingQuantity > 0)
        {
            merged.Add(new InventoryItemDraft
            {
                ItemId = member.Ammunition.AmmunitionItemId,
                Quantity = member.Ammunition.RemainingQuantity
            });
        }

        return merged;
    }

    /// Ammunition is carried as inventory, and what the character has now is
    /// whatever it has spent down to - not what the campaign started it with.
    private static IReadOnlyList<InventoryItemDraft> CreateInventory(
        PartyMemberState member)
    {
        if (member.Ammunition is null
            || member.Ammunition.RemainingQuantity == 0)
        {
            return Array.Empty<InventoryItemDraft>();
        }

        return
        [
            new InventoryItemDraft
            {
                ItemId = member.Ammunition.AmmunitionItemId,
                Quantity = member.Ammunition.RemainingQuantity
            }
        ];
    }

    private static string? ResolveBackgroundId(
        CampaignCharacterDefinition character,
        ValidatedRuleset ruleset)
    {
        if (ruleset.Definition.Backgrounds.Count == 0)
        {
            return null;
        }

        if (!ruleset.TryGetBackground(character.BackgroundId, out _))
        {
            throw new InvalidOperationException(
                $"Character '{character.PartyMemberId}' requires background '{character.BackgroundId}', which the ruleset does not define.");
        }

        return character.BackgroundId;
    }
}

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
    /// level is the party's own current level (PartyState.Level), not
    /// anything baked into the member or its build -- the whole party
    /// advances together (see PartyState.Level's own doc comment), so a
    /// resolved draft always reflects where the party stands today, roster
    /// character or player-built alike, regardless of what level a custom
    /// build happened to be validated at when it was first created.
    internal static CharacterDraft CreateDraft(
        PartyMemberState member,
        CampaignDefinition campaign,
        ValidatedRuleset ruleset,
        int level)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(ruleset);

        if (member.CustomBuild is not null)
        {
            return member.CustomBuild with
            {
                Level = level,
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
            Level = level,
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

    /// How many more maximum hit points a member's build resolves to at
    /// `level` than it does at level 1.
    ///
    /// A delta, not an absolute value, deliberately: a roster character's
    /// authored CampaignCharacterDefinition.MaximumHitPoints is a hand-tuned
    /// number that is not required to reproduce CharacterResolver's own
    /// level-1 hit-die-plus-Constitution formula exactly (and, checked
    /// against real roster content, several do not) -- the resolver's
    /// internal hit-point math (HitDiceRules, CalculateMaxHitPoints) is Core-
    /// internal besides, unreachable from here directly. Resolving twice and
    /// taking the difference sidesteps both problems: whatever the level-1
    /// number actually is, this is exactly how much more the same build
    /// would have gained reaching `level`.
    ///
    /// Short-circuits at level 1 without resolving anything: every party is
    /// level 1 today, and a level-1 member's build is not required to be
    /// independently resolvable through CharacterResolver for composition
    /// validation to pass -- it never was before advancement existed, and a
    /// delta of zero needs no resolution to know.
    internal static int GetHitPointDeltaSinceLevelOne(
        PartyMemberState member,
        CampaignDefinition campaign,
        ValidatedRuleset ruleset,
        int level)
    {
        if (level <= 1)
        {
            return 0;
        }

        CharacterDraft levelOneDraft = CreateDraft(member, campaign, ruleset, 1);
        CharacterResolver resolver = new(ruleset);
        int levelOneHitPoints =
            resolver.Resolve(levelOneDraft).MaxHitPoints ?? 0;
        int currentHitPoints =
            resolver.Resolve(levelOneDraft with { Level = level }).MaxHitPoints
                ?? 0;

        return currentHitPoints - levelOneHitPoints;
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

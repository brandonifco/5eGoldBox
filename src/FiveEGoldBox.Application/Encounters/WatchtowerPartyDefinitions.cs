using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Application.Encounters;

internal static class WatchtowerPartyDefinitions
{
    internal const string FighterDefinitionId =
        "character.fighter";

    internal const string BarbarianDefinitionId =
        "character.barbarian";

    internal const string RangerDefinitionId =
        "character.ranger";

    internal const string FighterClassId =
        "class.fighter";

    internal const string BarbarianClassId =
        "class.barbarian";

    internal const string RangerClassId =
        "class.ranger";

    internal const string FighterWeaponId =
        "weapon.longsword";

    internal const string BarbarianWeaponId =
        "weapon.greataxe";

    internal const string RangerWeaponId =
        "weapon.longbow";

    internal const string RangerAmmunitionItemId =
        "item.arrow";

    private const string HumanRaceId =
        "race.human";

    private const string SoldierBackgroundId =
        "background.soldier";

    internal static CharacterDraft CreateDraft(
        PartyMemberState member,
        ValidatedRuleset ruleset)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(ruleset);

        return member.CharacterDefinitionId switch
        {
            FighterDefinitionId => CreateFighterDraft(
                member,
                ruleset),
            BarbarianDefinitionId => CreateBarbarianDraft(
                member,
                ruleset),
            RangerDefinitionId => CreateRangerDraft(
                member,
                ruleset),
            _ => throw new InvalidOperationException(
                $"Character definition '{member.CharacterDefinitionId}' is not supported by the watchtower party.")
        };
    }

    private static CharacterDraft CreateFighterDraft(
        PartyMemberState member,
        ValidatedRuleset ruleset)
    {
        RequireClass(member, FighterClassId);
        RequireNoAmmunition(member);

        return CreateDraft(
            member,
            ruleset,
            FighterClassId,
            FighterWeaponId,
            new Dictionary<Ability, int>
            {
                [Ability.Strength] = 15,
                [Ability.Dexterity] = 11,
                [Ability.Constitution] = 13,
                [Ability.Intelligence] = 7,
                [Ability.Wisdom] = 9,
                [Ability.Charisma] = 12
            },
            [
                "skill.athletics",
                "skill.perception"
            ],
            Array.Empty<InventoryItemDraft>());
    }

    private static CharacterDraft CreateBarbarianDraft(
        PartyMemberState member,
        ValidatedRuleset ruleset)
    {
        RequireClass(member, BarbarianClassId);
        RequireNoAmmunition(member);

        return CreateDraft(
            member,
            ruleset,
            BarbarianClassId,
            BarbarianWeaponId,
            new Dictionary<Ability, int>
            {
                [Ability.Strength] = 15,
                [Ability.Dexterity] = 13,
                [Ability.Constitution] = 13,
                [Ability.Intelligence] = 7,
                [Ability.Wisdom] = 11,
                [Ability.Charisma] = 9
            },
            [
                "skill.athletics",
                "skill.survival"
            ],
            Array.Empty<InventoryItemDraft>());
    }

    private static CharacterDraft CreateRangerDraft(
        PartyMemberState member,
        ValidatedRuleset ruleset)
    {
        RequireClass(member, RangerClassId);

        AmmunitionState ammunition =
            member.Ammunition
            ?? throw new InvalidOperationException(
                "The bounded Ranger requires persistent ammunition state.");

        if (!string.Equals(
            ammunition.WeaponId,
            RangerWeaponId,
            StringComparison.Ordinal)
            || !string.Equals(
                ammunition.AmmunitionItemId,
                RangerAmmunitionItemId,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The bounded Ranger ammunition state does not match the authored longbow profile.");
        }

        IReadOnlyList<InventoryItemDraft> inventoryItems =
            ammunition.RemainingQuantity == 0
                ? Array.Empty<InventoryItemDraft>()
                :
                [
                    new InventoryItemDraft
                    {
                        ItemId = RangerAmmunitionItemId,
                        Quantity = ammunition.RemainingQuantity
                    }
                ];

        return CreateDraft(
            member,
            ruleset,
            RangerClassId,
            RangerWeaponId,
            new Dictionary<Ability, int>
            {
                [Ability.Strength] = 11,
                [Ability.Dexterity] = 15,
                [Ability.Constitution] = 11,
                [Ability.Intelligence] = 9,
                [Ability.Wisdom] = 15,
                [Ability.Charisma] = 7
            },
            [
                "skill.perception",
                "skill.stealth",
                "skill.survival"
            ],
            inventoryItems);
    }

    private static CharacterDraft CreateDraft(
        PartyMemberState member,
        ValidatedRuleset ruleset,
        string classId,
        string weaponId,
        IReadOnlyDictionary<Ability, int> abilityScores,
        IReadOnlyList<string> selectedSkillIds,
        IReadOnlyList<InventoryItemDraft> inventoryItems)
    {
        return new CharacterDraft
        {
            Name = member.DisplayName,
            Level = 1,
            RaceId = HumanRaceId,
            ClassId = classId,
            BackgroundId = ResolveBackgroundId(ruleset),
            AbilityScoreGenerationMethod =
                AbilityScoreGenerationMethod.Manual,
            BaseAbilityScores = abilityScores,
            SelectedSkillIds = selectedSkillIds,
            EquippedWeaponIds =
            [
                weaponId
            ],
            InventoryItems = inventoryItems
        };
    }

    private static string? ResolveBackgroundId(
        ValidatedRuleset ruleset)
    {
        if (ruleset.Index.BackgroundsById.Count == 0)
        {
            return null;
        }

        if (!ruleset.Index.BackgroundsById.ContainsKey(
            SoldierBackgroundId))
        {
            throw new InvalidOperationException(
                $"The bounded party requires background '{SoldierBackgroundId}' when the supplied ruleset defines backgrounds.");
        }

        return SoldierBackgroundId;
    }

    private static void RequireClass(
        PartyMemberState member,
        string expectedClassId)
    {
        if (!string.Equals(
            member.ClassId,
            expectedClassId,
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Character definition '{member.CharacterDefinitionId}' requires class '{expectedClassId}'.");
        }
    }

    private static void RequireNoAmmunition(
        PartyMemberState member)
    {
        if (member.Ammunition is not null)
        {
            throw new InvalidOperationException(
                $"Character definition '{member.CharacterDefinitionId}' does not use persistent ammunition.");
        }
    }
}

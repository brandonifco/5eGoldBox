using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Internal;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Characters;

public sealed partial class CharacterResolver
{
    private static WeaponAttack CreateWeaponAttack(
        WeaponDefinition weapon,
        ClassDefinition? selectedClass,
        IReadOnlyDictionary<Ability, int> abilityModifiers,
        int proficiencyBonus,
        IReadOnlyList<InventoryItemDraft> inventoryItems,
        CharacterSize size)
    {
        Ability attackAbility = GetWeaponAttackAbility(weapon, abilityModifiers);
        int abilityModifier = abilityModifiers[attackAbility];

        bool isProficient = IsProficientWithWeapon(weapon, selectedClass);
        int appliedProficiencyBonus = isProficient ? proficiencyBonus : 0;

        int attackBonus = abilityModifier + appliedProficiencyBonus;
        int damageBonus = abilityModifier;

        IReadOnlyList<string> disadvantageReasons = GetWeaponAttackDisadvantageReasons(
            weapon,
            size);

        bool hasDisadvantage = disadvantageReasons.Count > 0;
        D20RollMode attackRollMode = D20Rules.ResolveRollMode(
            hasAdvantage: false,
            hasDisadvantage);

        int? ammunitionQuantityAvailable = weapon.AmmunitionItemId is null
            ? null
            : inventoryItems
                .Where(item => item.ItemId == weapon.AmmunitionItemId)
                .Where(item => item.Quantity > 0)
                .Sum(item => item.Quantity);

        return new WeaponAttack
        {
            WeaponId = weapon.Id,
            WeaponName = weapon.Name,
            Category = weapon.Category,
            AttackKind = weapon.AttackKind,
            AttackAbility = attackAbility,
            AbilityModifier = abilityModifier,
            IsProficient = isProficient,
            ProficiencyBonus = appliedProficiencyBonus,
            AttackBonus = attackBonus,
            HasDisadvantage = hasDisadvantage,
            DisadvantageReasons = CoreCollectionProtection.ProtectList(disadvantageReasons),
            AttackRollMode = attackRollMode,
            Damage = weapon.Damage,
            VersatileDamage = weapon.VersatileDamage,
            DamageType = weapon.DamageType,
            DamageBonus = damageBonus,
            Properties = CoreCollectionProtection.ProtectList(weapon.Properties),
            ReachFeet = weapon.ReachFeet,
            NormalRangeFeet = weapon.NormalRangeFeet,
            LongRangeFeet = weapon.LongRangeFeet,
            AmmunitionItemId = weapon.AmmunitionItemId,
            AmmunitionQuantityAvailable = ammunitionQuantityAvailable
        };
    }

    private static IReadOnlyList<string> GetWeaponAttackDisadvantageReasons(
        WeaponDefinition weapon,
        CharacterSize size)
    {
        List<string> reasons = [];

        if (size == CharacterSize.Small
            && weapon.Properties.Contains(RuleIds.WeaponProperties.Heavy))
        {
            reasons.Add(RuleIds.DisadvantageReasons.HeavyWeaponSmallSize);
        }

        return reasons;
    }

    private static Ability GetWeaponAttackAbility(
        WeaponDefinition weapon,
        IReadOnlyDictionary<Ability, int> abilityModifiers)
    {
        bool isFinesse = weapon.Properties.Contains(RuleIds.WeaponProperties.Finesse);

        if (isFinesse)
        {
            int strengthModifier = abilityModifiers[Ability.Strength];
            int dexterityModifier = abilityModifiers[Ability.Dexterity];

            return dexterityModifier > strengthModifier
                ? Ability.Dexterity
                : Ability.Strength;
        }

        return weapon.AttackKind == WeaponAttackKind.Ranged
            ? Ability.Dexterity
            : Ability.Strength;
    }

    private static bool IsProficientWithWeapon(
        WeaponDefinition weapon,
        ClassDefinition? selectedClass)
    {
        if (selectedClass is null)
        {
            return false;
        }

        string categoryProficiencyId = weapon.Category switch
        {
            WeaponCategory.Simple => RuleIds.WeaponProficiencies.Simple,
            WeaponCategory.Martial => RuleIds.WeaponProficiencies.Martial,
            _ => throw new InvalidOperationException($"Unsupported weapon category '{weapon.Category}'.")
        };

        return selectedClass.WeaponProficiencies.Contains(categoryProficiencyId)
            || selectedClass.WeaponProficiencies.Contains(weapon.Id);
    }
}

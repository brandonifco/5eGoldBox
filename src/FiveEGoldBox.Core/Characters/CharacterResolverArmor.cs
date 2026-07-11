using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Characters;

public sealed partial class CharacterResolver
{
    private static int CalculateArmorClass(
        int dexterityModifier,
        ArmorDefinition? equippedArmor,
        ArmorDefinition? equippedShield)
    {
        int armorClass;

        if (equippedArmor is null)
        {
            armorClass = 10 + dexterityModifier;
        }
        else
        {
            armorClass = equippedArmor.BaseArmorClass + equippedArmor.ArmorClassBonus;

            if (equippedArmor.AddsDexterityModifier)
            {
                int appliedDexterityModifier = equippedArmor.MaximumDexterityModifier.HasValue
                    ? Math.Min(dexterityModifier, equippedArmor.MaximumDexterityModifier.Value)
                    : dexterityModifier;

                armorClass += appliedDexterityModifier;
            }
        }

        if (equippedShield is not null)
        {
            armorClass += equippedShield.ArmorClassBonus;
        }

        return armorClass;
    }

    private static bool IsProficientWithArmor(
        ArmorDefinition armor,
        ClassDefinition? selectedClass)
    {
        if (selectedClass is null)
        {
            return false;
        }

        string categoryProficiencyId = armor.Category switch
        {
            ArmorCategory.Light => RuleIds.ArmorProficiencies.Light,
            ArmorCategory.Medium => RuleIds.ArmorProficiencies.Medium,
            ArmorCategory.Heavy => RuleIds.ArmorProficiencies.Heavy,
            ArmorCategory.Shield => RuleIds.ArmorProficiencies.Shields,
            _ => throw new InvalidOperationException($"Unsupported armor category '{armor.Category}'.")
        };

        return selectedClass.ArmorProficiencies.Contains(categoryProficiencyId)
            || selectedClass.ArmorProficiencies.Contains(armor.Id);
    }
}
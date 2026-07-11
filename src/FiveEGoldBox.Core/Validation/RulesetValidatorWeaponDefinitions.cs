using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Validation;

public static partial class RulesetValidator
{
    private static void AddWeaponDefinitionIssues(
        List<ValidationIssue> issues,
        IReadOnlyList<WeaponDefinition> weapons)
    {
        HashSet<string> knownWeaponPropertyIds =
        [
            RuleIds.WeaponProperties.Ammunition,
            RuleIds.WeaponProperties.Finesse,
            RuleIds.WeaponProperties.Heavy,
            RuleIds.WeaponProperties.TwoHanded,
            RuleIds.WeaponProperties.Versatile
        ];

        foreach (WeaponDefinition weapon in weapons)
        {
            AddRequiredStringIssue(
                issues,
                weapon.DamageType,
                "ruleset.weapons.damage_type.required",
                $"Ruleset weapon '{weapon.Id}' has missing damage type.");

            foreach (string propertyId in weapon.Properties)
            {
                if (knownWeaponPropertyIds.Contains(propertyId))
                {
                    continue;
                }

                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "ruleset.weapons.properties.unknown_property",
                    $"Ruleset weapon '{weapon.Id}' references unknown weapon property '{propertyId}'."));
            }
        }
    }
}

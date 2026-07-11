using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Core.Validation;

public static partial class RulesetValidator
{
    private static void AddNumericDefinitionIssues(
        List<ValidationIssue> issues,
        RulesetDefinition ruleset)
    {
        foreach (RaceDefinition race in ruleset.Races)
        {
            AddInvalidNumericIssue(
                issues,
                race.BaseSpeedFeet <= 0,
                "ruleset.races.base_speed.invalid",
                $"Ruleset race '{race.Id}' has invalid base speed '{race.BaseSpeedFeet}'.");

            AddMovementSpeedIssues(
                issues,
                race.MovementSpeeds,
                "ruleset.races.movement_speeds.speed.invalid",
                $"Ruleset race '{race.Id}'");

            AddSenseIssues(
                issues,
                race.Senses,
                "ruleset.races.senses.range.invalid",
                $"Ruleset race '{race.Id}'");

            foreach (SubraceDefinition subrace in race.Subraces)
            {
                AddMovementSpeedIssues(
                    issues,
                    subrace.MovementSpeeds,
                    "ruleset.races.subraces.movement_speeds.speed.invalid",
                    $"Ruleset race '{race.Id}' subrace '{subrace.Id}'");

                AddSenseIssues(
                    issues,
                    subrace.Senses,
                    "ruleset.races.subraces.senses.range.invalid",
                    $"Ruleset race '{race.Id}' subrace '{subrace.Id}'");
            }
        }

        foreach (ArmorDefinition armor in ruleset.Armors)
        {
            AddInvalidNumericIssue(
                issues,
                armor.BaseArmorClass <= 0,
                "ruleset.armors.base_armor_class.invalid",
                $"Ruleset armor '{armor.Id}' has invalid base armor class '{armor.BaseArmorClass}'.");

            AddInvalidNumericIssue(
                issues,
                armor.MaximumDexterityModifier < 0,
                "ruleset.armors.maximum_dexterity_modifier.invalid",
                $"Ruleset armor '{armor.Id}' has invalid maximum Dexterity modifier '{armor.MaximumDexterityModifier}'.");

            AddInvalidNumericIssue(
                issues,
                armor.StrengthRequirement < 0,
                "ruleset.armors.strength_requirement.invalid",
                $"Ruleset armor '{armor.Id}' has invalid Strength requirement '{armor.StrengthRequirement}'.");

            AddInvalidNumericIssue(
                issues,
                armor.WeightPounds < 0,
                "ruleset.armors.weight.invalid",
                $"Ruleset armor '{armor.Id}' has invalid weight '{armor.WeightPounds}'.");

            AddInvalidNumericIssue(
                issues,
                armor.CostInCopperPieces < 0,
                "ruleset.armors.cost.invalid",
                $"Ruleset armor '{armor.Id}' has invalid cost '{armor.CostInCopperPieces}'.");
        }

        foreach (WeaponDefinition weapon in ruleset.Weapons)
        {
            AddInvalidNumericIssue(
                issues,
                weapon.Damage.Count <= 0,
                "ruleset.weapons.damage.count.invalid",
                $"Ruleset weapon '{weapon.Id}' has invalid damage dice count '{weapon.Damage.Count}'.");

            AddInvalidNumericIssue(
                issues,
                weapon.VersatileDamage is not null && weapon.VersatileDamage.Count <= 0,
                "ruleset.weapons.versatile_damage.count.invalid",
                $"Ruleset weapon '{weapon.Id}' has invalid versatile damage dice count '{weapon.VersatileDamage?.Count}'.");

            AddInvalidNumericIssue(
                issues,
                weapon.ReachFeet <= 0,
                "ruleset.weapons.reach.invalid",
                $"Ruleset weapon '{weapon.Id}' has invalid reach '{weapon.ReachFeet}'.");

            AddInvalidNumericIssue(
                issues,
                weapon.NormalRangeFeet <= 0,
                "ruleset.weapons.normal_range.invalid",
                $"Ruleset weapon '{weapon.Id}' has invalid normal range '{weapon.NormalRangeFeet}'.");

            AddInvalidNumericIssue(
                issues,
                weapon.LongRangeFeet <= 0,
                "ruleset.weapons.long_range.invalid",
                $"Ruleset weapon '{weapon.Id}' has invalid long range '{weapon.LongRangeFeet}'.");

            AddInvalidNumericIssue(
                issues,
                weapon.WeightPounds < 0,
                "ruleset.weapons.weight.invalid",
                $"Ruleset weapon '{weapon.Id}' has invalid weight '{weapon.WeightPounds}'.");

            AddInvalidNumericIssue(
                issues,
                weapon.CostInCopperPieces < 0,
                "ruleset.weapons.cost.invalid",
                $"Ruleset weapon '{weapon.Id}' has invalid cost '{weapon.CostInCopperPieces}'.");
        }

        foreach (EquipmentItemDefinition equipmentItem in ruleset.EquipmentItems)
        {
            AddInvalidNumericIssue(
                issues,
                equipmentItem.WeightPounds < 0,
                "ruleset.equipment_items.weight.invalid",
                $"Ruleset equipment item '{equipmentItem.Id}' has invalid weight '{equipmentItem.WeightPounds}'.");

            AddInvalidNumericIssue(
                issues,
                equipmentItem.CostInCopperPieces < 0,
                "ruleset.equipment_items.cost.invalid",
                $"Ruleset equipment item '{equipmentItem.Id}' has invalid cost '{equipmentItem.CostInCopperPieces}'.");
        }
    }

    private static void AddMovementSpeedIssues(
        List<ValidationIssue> issues,
        IReadOnlyList<MovementSpeedDefinition> movementSpeeds,
        string issueCode,
        string ownerDescription)
    {
        foreach (MovementSpeedDefinition movementSpeed in movementSpeeds)
        {
            AddInvalidNumericIssue(
                issues,
                movementSpeed.SpeedFeet <= 0,
                issueCode,
                $"{ownerDescription} has invalid {movementSpeed.Mode} speed '{movementSpeed.SpeedFeet}'.");
        }
    }

    private static void AddSenseIssues(
        List<ValidationIssue> issues,
        IReadOnlyList<SenseDefinition> senses,
        string issueCode,
        string ownerDescription)
    {
        foreach (SenseDefinition sense in senses)
        {
            AddInvalidNumericIssue(
                issues,
                sense.RangeFeet <= 0,
                issueCode,
                $"{ownerDescription} has invalid {sense.Type} range '{sense.RangeFeet}'.");
        }
    }

    private static void AddInvalidNumericIssue(
        List<ValidationIssue> issues,
        bool isInvalid,
        string issueCode,
        string message)
    {
        if (!isInvalid)
        {
            return;
        }

        issues.Add(new ValidationIssue(
            ValidationSeverity.Error,
            issueCode,
            message));
    }
}

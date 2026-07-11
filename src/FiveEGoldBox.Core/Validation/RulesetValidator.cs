using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
namespace FiveEGoldBox.Core.Validation;

public static class RulesetValidator
{
    public static ValidationResult Validate(RulesetDefinition ruleset)
    {
        ArgumentNullException.ThrowIfNull(ruleset);

        List<ValidationIssue> issues = [];
        AddRequiredStringIssue(
    issues,
    ruleset.Id,
    "ruleset.id.required",
    "Ruleset ID is required.");

        AddRequiredStringIssue(
            issues,
            ruleset.Name,
            "ruleset.name.required",
            "Ruleset name is required.");

        AddRequiredDefinitionStringIssues(
            issues,
            ruleset.Races,
            "ruleset.races.id.required",
            "ruleset.races.name.required",
            "race",
            race => race.Id,
            race => race.Name);

        AddRequiredDefinitionStringIssues(
            issues,
            ruleset.Classes,
            "ruleset.classes.id.required",
            "ruleset.classes.name.required",
            "class",
            characterClass => characterClass.Id,
            characterClass => characterClass.Name);

        AddRequiredDefinitionStringIssues(
            issues,
            ruleset.Backgrounds,
            "ruleset.backgrounds.id.required",
            "ruleset.backgrounds.name.required",
            "background",
            background => background.Id,
            background => background.Name);

        AddRequiredDefinitionStringIssues(
            issues,
            ruleset.Skills,
            "ruleset.skills.id.required",
            "ruleset.skills.name.required",
            "skill",
            skill => skill.Id,
            skill => skill.Name);

        AddRequiredDefinitionStringIssues(
            issues,
            ruleset.Armors,
            "ruleset.armors.id.required",
            "ruleset.armors.name.required",
            "armor",
            armor => armor.Id,
            armor => armor.Name);

        AddRequiredDefinitionStringIssues(
            issues,
            ruleset.Weapons,
            "ruleset.weapons.id.required",
            "ruleset.weapons.name.required",
            "weapon",
            weapon => weapon.Id,
            weapon => weapon.Name);

        AddRequiredDefinitionStringIssues(
            issues,
            ruleset.EquipmentItems,
            "ruleset.equipment_items.id.required",
            "ruleset.equipment_items.name.required",
            "equipment item",
            equipmentItem => equipmentItem.Id,
            equipmentItem => equipmentItem.Name);

        AddDuplicateIdIssues(
            issues,
            ruleset.Races,
            "ruleset.races.duplicate_id",
            "race",
            race => race.Id);

        AddDuplicateIdIssues(
            issues,
            ruleset.Classes,
            "ruleset.classes.duplicate_id",
            "class",
            characterClass => characterClass.Id);

        AddDuplicateIdIssues(
            issues,
            ruleset.Backgrounds,
            "ruleset.backgrounds.duplicate_id",
            "background",
            background => background.Id);

        AddDuplicateIdIssues(
            issues,
            ruleset.Skills,
            "ruleset.skills.duplicate_id",
            "skill",
            skill => skill.Id);

        AddDuplicateIdIssues(
            issues,
            ruleset.Armors,
            "ruleset.armors.duplicate_id",
            "armor",
            armor => armor.Id);

        AddDuplicateIdIssues(
            issues,
            ruleset.Weapons,
            "ruleset.weapons.duplicate_id",
            "weapon",
            weapon => weapon.Id);

        AddDuplicateIdIssues(
            issues,
            ruleset.EquipmentItems,
            "ruleset.equipment_items.duplicate_id",
            "equipment item",
            equipmentItem => equipmentItem.Id);
        AddNumericDefinitionIssues(issues, ruleset);
        AddSubraceIssues(issues, ruleset.Races);

        HashSet<string> skillIds = ruleset.Skills
    .Select(skill => skill.Id)
    .ToHashSet();

        AddUnknownReferenceIssues(
            issues,
            ruleset.Classes,
            characterClass => characterClass.SkillChoices,
            skillIds,
            "ruleset.classes.skill_choices.unknown_skill",
            "class",
            characterClass => characterClass.Id,
            "skill");

        AddUnknownReferenceIssues(
            issues,
            ruleset.Backgrounds,
            background => background.SkillProficiencies,
            skillIds,
            "ruleset.backgrounds.skill_proficiencies.unknown_skill",
            "background",
            background => background.Id,
            "skill");

        HashSet<string> armorProficiencyIds = ruleset.Armors
    .Select(armor => armor.Id)
    .Concat(
    [
        RuleIds.ArmorProficiencies.Light,
                RuleIds.ArmorProficiencies.Medium,
                RuleIds.ArmorProficiencies.Heavy,
                RuleIds.ArmorProficiencies.Shields
    ])
    .ToHashSet();

        AddUnknownReferenceIssues(
            issues,
            ruleset.Classes,
            characterClass => characterClass.ArmorProficiencies,
            armorProficiencyIds,
            "ruleset.classes.armor_proficiencies.unknown_armor",
            "class",
            characterClass => characterClass.Id,
            "armor proficiency");

        HashSet<string> weaponProficiencyIds = ruleset.Weapons
            .Select(weapon => weapon.Id)
            .Concat(
            [
                RuleIds.WeaponProficiencies.Simple,
                RuleIds.WeaponProficiencies.Martial
            ])
            .ToHashSet();

        AddUnknownReferenceIssues(
            issues,
            ruleset.Classes,
            characterClass => characterClass.WeaponProficiencies,
            weaponProficiencyIds,
            "ruleset.classes.weapon_proficiencies.unknown_weapon",
            "class",
            characterClass => characterClass.Id,
            "weapon proficiency");

        HashSet<string> equipmentItemIds = ruleset.EquipmentItems
    .Select(item => item.Id)
    .ToHashSet();

        AddUnknownOptionalReferenceIssues(
            issues,
            ruleset.Weapons,
            weapon => weapon.AmmunitionItemId,
            equipmentItemIds,
            "ruleset.weapons.ammunition_item.unknown_item",
            "weapon",
            weapon => weapon.Id,
            "equipment item");

        return issues.Count == 0
            ? ValidationResult.Success
            : new ValidationResult(issues);
    }
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

    private static void AddSubraceIssues(
        List<ValidationIssue> issues,
        IReadOnlyList<RaceDefinition> races)
    {
        foreach (RaceDefinition race in races)
        {
            foreach (SubraceDefinition subrace in race.Subraces)
            {
                AddRequiredStringIssue(
                    issues,
                    subrace.Id,
                    "ruleset.races.subraces.id.required",
                    $"Ruleset race '{race.Id}' contains subrace with missing ID.");

                AddRequiredStringIssue(
                    issues,
                    subrace.Name,
                    "ruleset.races.subraces.name.required",
                    $"Ruleset race '{race.Id}' contains subrace with missing name.");
            }

            foreach (IGrouping<string, SubraceDefinition> duplicateGroup in race.Subraces
                .Where(subrace => !string.IsNullOrWhiteSpace(subrace.Id))
                .GroupBy(subrace => subrace.Id)
                .Where(group => group.Count() > 1))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "ruleset.races.subraces.duplicate_id",
                    $"Ruleset race '{race.Id}' contains duplicate subrace ID '{duplicateGroup.Key}'."));
            }
        }
    }

    private static void AddRequiredStringIssue(
    List<ValidationIssue> issues,
    string? value,
    string issueCode,
    string message)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        issues.Add(new ValidationIssue(
            ValidationSeverity.Error,
            issueCode,
            message));
    }

    private static void AddRequiredDefinitionStringIssues<TDefinition>(
        List<ValidationIssue> issues,
        IReadOnlyList<TDefinition> definitions,
        string missingIdIssueCode,
        string missingNameIssueCode,
        string definitionName,
        Func<TDefinition, string> getId,
        Func<TDefinition, string> getName)
    {
        foreach (TDefinition definition in definitions)
        {
            AddRequiredStringIssue(
                issues,
                getId(definition),
                missingIdIssueCode,
                $"Ruleset contains {definitionName} with missing ID.");

            AddRequiredStringIssue(
                issues,
                getName(definition),
                missingNameIssueCode,
                $"Ruleset contains {definitionName} with missing name.");
        }
    }
    private static void AddDuplicateIdIssues<TDefinition>(
        List<ValidationIssue> issues,
        IReadOnlyList<TDefinition> definitions,
        string issueCode,
        string definitionName,
        Func<TDefinition, string> getId)
    {
        foreach (IGrouping<string, TDefinition> duplicateGroup in definitions
            .GroupBy(getId)
            .Where(group => group.Count() > 1))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                issueCode,
                $"Ruleset contains duplicate {definitionName} ID '{duplicateGroup.Key}'."));
        }
    }
    private static void AddUnknownReferenceIssues<TDefinition>(
    List<ValidationIssue> issues,
    IReadOnlyList<TDefinition> definitions,
    Func<TDefinition, IReadOnlyList<string>> getReferencedIds,
    IReadOnlySet<string> validIds,
    string issueCode,
    string definitionName,
    Func<TDefinition, string> getDefinitionId,
    string referencedDefinitionName)
    {
        foreach (TDefinition definition in definitions)
        {
            foreach (string referencedId in getReferencedIds(definition))
            {
                if (validIds.Contains(referencedId))
                {
                    continue;
                }

                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    issueCode,
                    $"Ruleset {definitionName} '{getDefinitionId(definition)}' references unknown {referencedDefinitionName} ID '{referencedId}'."));
            }
        }
    }
    private static void AddUnknownOptionalReferenceIssues<TDefinition>(
    List<ValidationIssue> issues,
    IReadOnlyList<TDefinition> definitions,
    Func<TDefinition, string?> getReferencedId,
    IReadOnlySet<string> validIds,
    string issueCode,
    string definitionName,
    Func<TDefinition, string> getDefinitionId,
    string referencedDefinitionName)
    {
        foreach (TDefinition definition in definitions)
        {
            string? referencedId = getReferencedId(definition);

            if (string.IsNullOrWhiteSpace(referencedId)
                || validIds.Contains(referencedId))
            {
                continue;
            }

            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                issueCode,
                $"Ruleset {definitionName} '{getDefinitionId(definition)}' references unknown {referencedDefinitionName} ID '{referencedId}'."));
        }
    }
}

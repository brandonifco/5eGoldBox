using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Core.Characters;

public sealed partial class CharacterResolver
{
    public ValidationResult Validate(CharacterDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        List<ValidationIssue> issues = new();

        if (string.IsNullOrWhiteSpace(draft.Name))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "character.name.required",
                "Character name is required."));
        }

        ValidateRaceSelection(draft, issues);
        ValidateSubraceSelection(draft, issues);
        ValidateClassSelection(draft, issues);
        ValidateClassSkillChoices(draft, issues);
        ValidateBackgroundSelection(draft, issues);
        ValidateEquippedArmor(draft, issues);
        ValidateArmorProficiency(draft, issues);
        ValidateEquippedWeapons(draft, issues);
        ValidateWeaponProficiency(draft, issues);
        ValidateCarryingCapacity(draft, issues);
        ValidateInventoryItems(draft, issues);
        ValidateCurrency(draft, issues);
        ValidateWeaponAmmunition(draft, issues);
        ValidateTwoHandedWeaponsWithShield(draft, issues);
        ValidateSmallCharacterHeavyWeapons(draft, issues);

        if (draft.Level is < ProficiencyRules.MinimumLevel or > ProficiencyRules.MaximumLevel)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "character.level.out_of_range",
                $"Character level must be between {ProficiencyRules.MinimumLevel} and {ProficiencyRules.MaximumLevel}."));
        }

        foreach (Ability ability in Enum.GetValues<Ability>())
        {
            if (!draft.BaseAbilityScores.ContainsKey(ability))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    $"ability.{ability.ToString().ToLowerInvariant()}.missing",
                    $"{ability} score is required."));

                continue;
            }

            int score = draft.BaseAbilityScores[ability];

            if (score is < AbilityRules.MinimumScore or > AbilityRules.MaximumScore)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    $"ability.{ability.ToString().ToLowerInvariant()}.out_of_range",
                    $"{ability} score must be between {AbilityRules.MinimumScore} and {AbilityRules.MaximumScore}."));
            }
        }

        if (draft.AbilityScoreGenerationMethod == AbilityScoreGenerationMethod.PointBuy)
        {
            ValidatePointBuy(draft, issues);
        }

        if (draft.AbilityScoreGenerationMethod == AbilityScoreGenerationMethod.StandardArray)
        {
            ValidateStandardArray(draft, issues);
        }
        return new ValidationResult(issues);
    }

    private static void ValidatePointBuy(CharacterDraft draft, List<ValidationIssue> issues)
    {
        bool hasInvalidPointBuyScore = false;

        foreach (Ability ability in Enum.GetValues<Ability>())
        {
            if (!draft.BaseAbilityScores.TryGetValue(ability, out int score))
            {
                continue;
            }

            if (score is < PointBuyRules.MinimumScore or > PointBuyRules.MaximumScore)
            {
                hasInvalidPointBuyScore = true;

                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    $"ability.{ability.ToString().ToLowerInvariant()}.point_buy_out_of_range",
                    $"{ability} score must be between {PointBuyRules.MinimumScore} and {PointBuyRules.MaximumScore} for point buy."));
            }
        }

        bool hasAllAbilityScores = Enum
            .GetValues<Ability>()
            .All(ability => draft.BaseAbilityScores.ContainsKey(ability));

        if (!hasAllAbilityScores || hasInvalidPointBuyScore)
        {
            return;
        }

        int totalCost = PointBuyRules.GetTotalCost(draft.BaseAbilityScores);

        if (totalCost > PointBuyRules.MaximumTotalCost)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ability.point_buy.total_cost_too_high",
                $"Point-buy total cost must not exceed {PointBuyRules.MaximumTotalCost}. Actual cost: {totalCost}."));
        }
    }

    private static void ValidateStandardArray(CharacterDraft draft, List<ValidationIssue> issues)
    {
        bool hasAllAbilityScores = Enum
            .GetValues<Ability>()
            .All(ability => draft.BaseAbilityScores.ContainsKey(ability));

        if (!hasAllAbilityScores)
        {
            return;
        }

        if (!StandardArrayRules.IsValid(draft.BaseAbilityScores))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ability.standard_array.invalid",
                "Standard array characters must use the scores 15, 14, 13, 12, 10, and 8 exactly once each."));
        }
    }

    private void ValidateRaceSelection(CharacterDraft draft, List<ValidationIssue> issues)
    {
        if (_ruleset is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(draft.RaceId))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "character.race.required",
                "Character race is required."));

            return;
        }

        bool raceExists = _ruleset.Races.Any(race => race.Id == draft.RaceId);

        if (!raceExists)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "character.race.not_found",
                $"Race '{draft.RaceId}' was not found in ruleset '{_ruleset.Id}'."));
        }
    }

    private void ValidateSubraceSelection(CharacterDraft draft, List<ValidationIssue> issues)
    {
        if (_ruleset is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(draft.RaceId))
        {
            return;
        }

        RaceDefinition? selectedRace = GetSelectedRace(draft);

        if (selectedRace is null)
        {
            return;
        }

        if (selectedRace.Subraces.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(draft.SubraceId))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "character.subrace.not_allowed",
                    $"Race '{selectedRace.Id}' does not have subraces."));
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(draft.SubraceId))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "character.subrace.required",
                $"Character subrace is required for race '{selectedRace.Id}'."));

            return;
        }

        bool subraceExists = selectedRace.Subraces.Any(subrace => subrace.Id == draft.SubraceId);

        if (!subraceExists)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "character.subrace.not_found",
                $"Subrace '{draft.SubraceId}' was not found for race '{selectedRace.Id}'."));
        }
    }

    private void ValidateClassSelection(CharacterDraft draft, List<ValidationIssue> issues)
    {
        if (_ruleset is null || _ruleset.Classes.Count == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(draft.ClassId))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "character.class.required",
                "Character class is required."));

            return;
        }

        bool classExists = _ruleset.Classes.Any(characterClass => characterClass.Id == draft.ClassId);

        if (!classExists)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "character.class.not_found",
                $"Class '{draft.ClassId}' was not found in ruleset '{_ruleset.Id}'."));
        }
    }

    private void ValidateClassSkillChoices(CharacterDraft draft, List<ValidationIssue> issues)
    {
        ClassDefinition? selectedClass = GetSelectedClass(draft);

        if (selectedClass is null)
        {
            return;
        }

        int selectedSkillCount = draft.SelectedSkillIds.Count;

        if (selectedClass.NumberOfSkillChoices == 0)
        {
            if (selectedSkillCount > 0)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "character.skills.not_allowed",
                    $"Class '{selectedClass.Id}' does not allow class skill choices."));
            }

            return;
        }

        int distinctSelectedSkillCount = draft.SelectedSkillIds
            .Distinct()
            .Count();

        if (distinctSelectedSkillCount != selectedSkillCount)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "character.skills.duplicate",
                "Selected class skills must not contain duplicates."));
        }

        if (selectedSkillCount != selectedClass.NumberOfSkillChoices)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "character.skills.invalid_count",
                $"Class '{selectedClass.Id}' requires exactly {selectedClass.NumberOfSkillChoices} skill choice(s). Actual count: {selectedSkillCount}."));
        }

        foreach (string selectedSkillId in draft.SelectedSkillIds)
        {
            bool skillIsAvailable = selectedClass.SkillChoices.Contains(selectedSkillId);

            if (!skillIsAvailable)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "character.skills.not_available",
                    $"Skill '{selectedSkillId}' is not available to class '{selectedClass.Id}'."));
            }
        }
    }

    private void ValidateBackgroundSelection(CharacterDraft draft, List<ValidationIssue> issues)
    {
        if (_ruleset is null || _ruleset.Backgrounds.Count == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(draft.BackgroundId))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "character.background.required",
                "Character background is required."));

            return;
        }

        bool backgroundExists = _ruleset.Backgrounds.Any(background => background.Id == draft.BackgroundId);

        if (!backgroundExists)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "character.background.not_found",
                $"Background '{draft.BackgroundId}' was not found in ruleset '{_ruleset.Id}'."));
        }
    }

    private void ValidateEquippedArmor(CharacterDraft draft, List<ValidationIssue> issues)
    {
        if (_ruleset is null || _ruleset.Armors.Count == 0)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(draft.EquippedArmorId))
        {
            ArmorDefinition? equippedArmor = _ruleset.Armors.SingleOrDefault(
                armor => armor.Id == draft.EquippedArmorId);

            if (equippedArmor is null)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "character.armor.not_found",
                    $"Armor '{draft.EquippedArmorId}' was not found in ruleset '{_ruleset.Id}'."));
            }
            else if (equippedArmor.Category == ArmorCategory.Shield)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "character.armor.invalid_category",
                    $"Armor '{draft.EquippedArmorId}' cannot be equipped as body armor because it is a shield."));
            }
        }

        if (!string.IsNullOrWhiteSpace(draft.EquippedShieldId))
        {
            ArmorDefinition? equippedShield = _ruleset.Armors.SingleOrDefault(
                armor => armor.Id == draft.EquippedShieldId);

            if (equippedShield is null)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "character.shield.not_found",
                    $"Shield '{draft.EquippedShieldId}' was not found in ruleset '{_ruleset.Id}'."));
            }
            else if (equippedShield.Category != ArmorCategory.Shield)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "character.shield.invalid_category",
                    $"Armor '{draft.EquippedShieldId}' cannot be equipped as a shield."));
            }
        }
    }

    private void ValidateWeaponProficiency(CharacterDraft draft, List<ValidationIssue> issues)
    {
        if (_ruleset is null || _ruleset.Weapons.Count == 0)
        {
            return;
        }

        ClassDefinition? selectedClass = GetSelectedClass(draft);

        foreach (WeaponDefinition equippedWeapon in GetEquippedWeapons(draft))
        {
            if (IsProficientWithWeapon(equippedWeapon, selectedClass))
            {
                continue;
            }

            issues.Add(new ValidationIssue(
                ValidationSeverity.Warning,
                "character.weapon.not_proficient",
                $"Character is not proficient with equipped weapon '{equippedWeapon.Id}'."));
        }
    }

    private void ValidateInventoryItems(CharacterDraft draft, List<ValidationIssue> issues)
    {
        foreach (InventoryItemDraft inventoryItem in draft.InventoryItems)
        {
            if (inventoryItem.Quantity <= 0)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "character.inventory.quantity.invalid",
                    $"Inventory item '{inventoryItem.ItemId}' must have a quantity greater than 0."));
            }
        }

        int inventoryItemCount = draft.InventoryItems.Count;
        int distinctInventoryItemCount = draft.InventoryItems
            .Select(item => item.ItemId)
            .Distinct()
            .Count();

        if (distinctInventoryItemCount != inventoryItemCount)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "character.inventory.duplicate",
                "Inventory items must not contain duplicate item IDs."));
        }

        if (_ruleset is null || _ruleset.EquipmentItems.Count == 0)
        {
            return;
        }

        foreach (InventoryItemDraft inventoryItem in draft.InventoryItems)
        {
            bool itemExists = _ruleset.EquipmentItems.Any(
                item => item.Id == inventoryItem.ItemId);

            if (!itemExists)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "character.inventory.item_not_found",
                    $"Inventory item '{inventoryItem.ItemId}' was not found in ruleset '{_ruleset.Id}'."));
            }
        }
    }

    private void ValidateWeaponAmmunition(CharacterDraft draft, List<ValidationIssue> issues)
    {
        if (_ruleset is null)
        {
            return;
        }

        IReadOnlyList<WeaponDefinition> equippedWeapons = GetEquippedWeapons(draft);

        foreach (WeaponDefinition weapon in equippedWeapons)
        {
            if (weapon.AmmunitionItemId is null)
            {
                continue;
            }

            int ammunitionQuantity = draft.InventoryItems
                .Where(item => item.ItemId == weapon.AmmunitionItemId)
                .Where(item => item.Quantity > 0)
                .Sum(item => item.Quantity);

            if (ammunitionQuantity > 0)
            {
                continue;
            }

            issues.Add(new ValidationIssue(
                ValidationSeverity.Warning,
                "character.weapon.ammunition.missing",
                $"Equipped weapon '{weapon.Name}' requires ammunition item '{weapon.AmmunitionItemId}', but none is available."));
        }
    }

    private void ValidateTwoHandedWeaponsWithShield(CharacterDraft draft, List<ValidationIssue> issues)
    {
        if (_ruleset is null)
        {
            return;
        }

        ArmorDefinition? equippedShield = GetEquippedShield(draft);

        if (equippedShield is null)
        {
            return;
        }

        IReadOnlyList<WeaponDefinition> equippedWeapons = GetEquippedWeapons(draft);

        foreach (WeaponDefinition weapon in equippedWeapons)
        {
            if (!weapon.Properties.Contains(RuleIds.WeaponProperties.TwoHanded))
            {
                continue;
            }

            issues.Add(new ValidationIssue(
                ValidationSeverity.Warning,
                "character.weapon.two_handed.shield_equipped",
                $"Equipped weapon '{weapon.Name}' has the two-handed property and cannot be used while shield '{equippedShield.Name}' is equipped."));
        }
    }

    private void ValidateSmallCharacterHeavyWeapons(CharacterDraft draft, List<ValidationIssue> issues)
    {
        if (_ruleset is null)
        {
            return;
        }

        RaceDefinition? selectedRace = GetSelectedRace(draft);

        if (selectedRace?.Size != CharacterSize.Small)
        {
            return;
        }

        IReadOnlyList<WeaponDefinition> equippedWeapons = GetEquippedWeapons(draft);

        foreach (WeaponDefinition weapon in equippedWeapons)
        {
            if (!weapon.Properties.Contains(RuleIds.WeaponProperties.Heavy))
            {
                continue;
            }

            issues.Add(new ValidationIssue(
                ValidationSeverity.Warning,
                "character.weapon.heavy.small_size",
                $"Small character has disadvantage on attack rolls with heavy weapon '{weapon.Name}'."));
        }
    }

    private void ValidateEquippedWeapons(CharacterDraft draft, List<ValidationIssue> issues)
    {
        if (_ruleset is null || _ruleset.Weapons.Count == 0)
        {
            return;
        }

        int equippedWeaponCount = draft.EquippedWeaponIds.Count;
        int distinctEquippedWeaponCount = draft.EquippedWeaponIds
            .Distinct()
            .Count();

        if (distinctEquippedWeaponCount != equippedWeaponCount)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "character.weapons.duplicate",
                "Equipped weapons must not contain duplicates."));
        }

        foreach (string equippedWeaponId in draft.EquippedWeaponIds)
        {
            bool weaponExists = _ruleset.Weapons.Any(weapon => weapon.Id == equippedWeaponId);

            if (!weaponExists)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "character.weapon.not_found",
                    $"Weapon '{equippedWeaponId}' was not found in ruleset '{_ruleset.Id}'."));
            }
        }
    }

    private void ValidateArmorProficiency(CharacterDraft draft, List<ValidationIssue> issues)
    {
        if (_ruleset is null || _ruleset.Armors.Count == 0)
        {
            return;
        }

        ClassDefinition? selectedClass = GetSelectedClass(draft);

        ArmorDefinition? equippedArmor = GetEquippedArmor(draft);
        ArmorDefinition? equippedShield = GetEquippedShield(draft);

        if (equippedArmor is not null && !IsProficientWithArmor(equippedArmor, selectedClass))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Warning,
                "character.armor.not_proficient",
                $"Character is not proficient with equipped armor '{equippedArmor.Id}'."));
        }

        if (equippedShield is not null && !IsProficientWithArmor(equippedShield, selectedClass))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Warning,
                "character.shield.not_proficient",
                $"Character is not proficient with equipped shield '{equippedShield.Id}'."));
        }
    }

    private void ValidateCarryingCapacity(CharacterDraft draft, List<ValidationIssue> issues)
    {
        if (_ruleset is null)
        {
            return;
        }

        if (!draft.BaseAbilityScores.TryGetValue(Ability.Strength, out int baseStrength))
        {
            return;
        }

        if (baseStrength is < AbilityRules.MinimumScore or > AbilityRules.MaximumScore)
        {
            return;
        }

        RaceDefinition? selectedRace = GetSelectedRace(draft);
        SubraceDefinition? selectedSubrace = GetSelectedSubrace(draft, selectedRace);

        int strengthScore = baseStrength;

        if (selectedRace is not null)
        {
            foreach (AbilityScoreIncrease increase in selectedRace.AbilityScoreIncreases)
            {
                if (increase.Ability == Ability.Strength)
                {
                    strengthScore += increase.Amount;
                }
            }
        }

        if (selectedSubrace is not null)
        {
            foreach (AbilityScoreIncrease increase in selectedSubrace.AbilityScoreIncreases)
            {
                if (increase.Ability == Ability.Strength)
                {
                    strengthScore += increase.Amount;
                }
            }
        }

        int carryingCapacityPounds = strengthScore * 15;

        decimal equippedWeightPounds = CalculateEquippedWeight(
            GetEquippedArmor(draft),
            GetEquippedShield(draft),
            GetEquippedWeapons(draft));

        decimal inventoryWeightPounds = CalculateInventoryWeight(draft);

        decimal currencyWeightPounds = HasNegativeCurrencyAmount(draft.Currency)
            ? 0m
            : CalculateCurrencyWeight(draft.Currency);

        decimal totalCarriedWeightPounds = equippedWeightPounds
            + inventoryWeightPounds
            + currencyWeightPounds;

        if (totalCarriedWeightPounds <= carryingCapacityPounds)
        {
            return;
        }

        issues.Add(new ValidationIssue(
            ValidationSeverity.Warning,
            "character.carrying_capacity.exceeded",
            $"Total carried weight {totalCarriedWeightPounds} lb. exceeds carrying capacity {carryingCapacityPounds} lb."));
    }

    private static void ValidateCurrency(CharacterDraft draft, List<ValidationIssue> issues)
    {
        if (draft.Currency.CopperPieces < 0)
        {
            AddNegativeCurrencyIssue(issues, "CopperPieces");
        }

        if (draft.Currency.SilverPieces < 0)
        {
            AddNegativeCurrencyIssue(issues, "SilverPieces");
        }

        if (draft.Currency.ElectrumPieces < 0)
        {
            AddNegativeCurrencyIssue(issues, "ElectrumPieces");
        }

        if (draft.Currency.GoldPieces < 0)
        {
            AddNegativeCurrencyIssue(issues, "GoldPieces");
        }

        if (draft.Currency.PlatinumPieces < 0)
        {
            AddNegativeCurrencyIssue(issues, "PlatinumPieces");
        }
    }

    private static void AddNegativeCurrencyIssue(
        List<ValidationIssue> issues,
        string currencyPropertyName)
    {
        issues.Add(new ValidationIssue(
            ValidationSeverity.Error,
            "character.currency.amount.invalid",
            $"{currencyPropertyName} must not be negative."));
    }
}

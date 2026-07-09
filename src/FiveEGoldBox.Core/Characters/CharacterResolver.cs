using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Validation;
using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Core.Characters;

public sealed class CharacterResolver
{
    private readonly RulesetDefinition? _ruleset;

    public CharacterResolver()
    {
    }

    public CharacterResolver(RulesetDefinition ruleset)
    {
        ArgumentNullException.ThrowIfNull(ruleset);

        _ruleset = ruleset;
    }

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
        ValidateEquippedWeapons(draft, issues);

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

    public CharacterSnapshot Resolve(CharacterDraft draft)
    {
        ValidationResult validation = Validate(draft);

        if (!validation.IsValid)
        {
            string message = string.Join(
                Environment.NewLine,
                validation.Issues
                    .Where(issue => issue.Severity == ValidationSeverity.Error)
                    .Select(issue => $"{issue.Code}: {issue.Message}"));

            throw new InvalidOperationException(
                $"Cannot resolve invalid character draft:{Environment.NewLine}{message}");
        }

        RaceDefinition? selectedRace = GetSelectedRace(draft);
        SubraceDefinition? selectedSubrace = GetSelectedSubrace(draft, selectedRace);
        ClassDefinition? selectedClass = GetSelectedClass(draft);
        BackgroundDefinition? selectedBackground = GetSelectedBackground(draft);
        ArmorDefinition? equippedArmor = GetEquippedArmor(draft);
        ArmorDefinition? equippedShield = GetEquippedShield(draft);
        IReadOnlyList<WeaponDefinition> equippedWeapons = GetEquippedWeapons(draft);

        Dictionary<Ability, int> abilityScores = draft.BaseAbilityScores.ToDictionary(
            pair => pair.Key,
            pair => pair.Value);

        if (selectedRace is not null)
        {
            foreach (AbilityScoreIncrease increase in selectedRace.AbilityScoreIncreases)
            {
                abilityScores[increase.Ability] += increase.Amount;
            }
        }

        if (selectedSubrace is not null)
        {
            foreach (AbilityScoreIncrease increase in selectedSubrace.AbilityScoreIncreases)
            {
                abilityScores[increase.Ability] += increase.Amount;
            }
        }

        Dictionary<Ability, int> abilityModifiers = abilityScores.ToDictionary(
            pair => pair.Key,
            pair => AbilityRules.GetModifier(pair.Value));

        int armorClass = CalculateArmorClass(
            abilityModifiers[Ability.Dexterity],
            equippedArmor,
            equippedShield);


        IReadOnlyList<string> languages = (selectedRace?.Languages ?? Array.Empty<string>())
            .Concat(selectedSubrace?.Languages ?? Array.Empty<string>())
            .Concat(selectedBackground?.Languages ?? Array.Empty<string>())
            .Distinct()
            .ToArray();

        IReadOnlyList<string> traits = selectedRace is null
            ? Array.Empty<string>()
            : selectedRace.Traits
                .Concat(selectedSubrace?.Traits ?? Array.Empty<string>())
                .Distinct()
                .ToArray();

        IReadOnlyList<string> skillProficiencies = (selectedClass is null
                ? Array.Empty<string>()
                : draft.SelectedSkillIds)
            .Concat(selectedBackground?.SkillProficiencies ?? Array.Empty<string>())
            .Distinct()
            .ToArray();

        int proficiencyBonus = ProficiencyRules.GetBonus(draft.Level);

        IReadOnlyList<SavingThrowBonus> savingThrowBonuses = Enum
            .GetValues<Ability>()
            .Select(ability =>
            {
                bool isProficient = selectedClass?.SavingThrowProficiencies.Contains(ability) ?? false;
                int abilityModifier = abilityModifiers[ability];

                return new SavingThrowBonus
                {
                    Ability = ability,
                    AbilityModifier = abilityModifier,
                    IsProficient = isProficient,
                    ProficiencyBonus = isProficient ? proficiencyBonus : 0,
                    TotalBonus = abilityModifier + (isProficient ? proficiencyBonus : 0)
                };
            })
            .ToArray();

        IReadOnlyList<SkillBonus> skillBonuses = _ruleset is null
            ? Array.Empty<SkillBonus>()
            : _ruleset.Skills
                .Select(skill =>
                {
                    bool isProficient = skillProficiencies.Contains(skill.Id);
                    int abilityModifier = abilityModifiers[skill.Ability];

                    return new SkillBonus
                    {
                        SkillId = skill.Id,
                        SkillName = skill.Name,
                        Ability = skill.Ability,
                        AbilityModifier = abilityModifier,
                        IsProficient = isProficient,
                        ProficiencyBonus = isProficient ? proficiencyBonus : 0,
                        TotalBonus = abilityModifier + (isProficient ? proficiencyBonus : 0)
                    };
                })
                .ToArray();

        IReadOnlyList<WeaponAttack> weaponAttacks = equippedWeapons
            .Select(weapon => CreateWeaponAttack(
                weapon,
                selectedClass,
                abilityModifiers,
                proficiencyBonus))
            .ToArray();

        return new CharacterSnapshot
        {
            Name = draft.Name!.Trim(),
            Level = draft.Level,
            ClassId = selectedClass?.Id,
            ClassName = selectedClass?.Name,
            BackgroundId = selectedBackground?.Id,
            BackgroundName = selectedBackground?.Name,
            BackgroundFeatureId = selectedBackground?.FeatureId,
            HitDie = selectedClass?.HitDie,
            MaxHitPoints = selectedClass is null
                ? null
                : (int)selectedClass.HitDie + abilityModifiers[Ability.Constitution],
            RaceId = selectedRace?.Id,
            RaceName = selectedRace?.Name,
            SubraceId = selectedSubrace?.Id,
            SubraceName = selectedSubrace?.Name,
            SpeedFeet = selectedRace?.BaseSpeedFeet,
            EquippedArmorId = equippedArmor?.Id,
            EquippedArmorName = equippedArmor?.Name,
            EquippedShieldId = equippedShield?.Id,
            EquippedShieldName = equippedShield?.Name,
            ArmorClass = armorClass,
            EquippedWeaponIds = equippedWeapons
                .Select(weapon => weapon.Id)
                .ToArray(),
            EquippedWeaponNames = equippedWeapons
                .Select(weapon => weapon.Name)
                .ToArray(),
            WeaponAttacks = weaponAttacks,
            ProficiencyBonus = proficiencyBonus,
            AbilityScores = abilityScores,
            AbilityModifiers = abilityModifiers,
            SavingThrowBonuses = savingThrowBonuses,
            SavingThrowProficiencies = selectedClass?.SavingThrowProficiencies ?? Array.Empty<Ability>(),
            ArmorProficiencies = selectedClass?.ArmorProficiencies ?? Array.Empty<string>(),
            WeaponProficiencies = selectedClass?.WeaponProficiencies ?? Array.Empty<string>(),
            ToolProficiencies = (selectedClass?.ToolProficiencies ?? Array.Empty<string>())
                .Concat(selectedBackground?.ToolProficiencies ?? Array.Empty<string>())
                .Distinct()
                .ToArray(),
            SkillProficiencies = skillProficiencies,
            SkillBonuses = skillBonuses,
            Languages = languages,
            Traits = traits,
            ClassFeatures = selectedClass is null
                ? Array.Empty<string>()
                : selectedClass.FeaturesByLevel
                    .Where(pair => pair.Key <= draft.Level)
                    .OrderBy(pair => pair.Key)
                    .SelectMany(pair => pair.Value)
                    .Distinct()
                    .ToArray()
        };
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

    private RaceDefinition? GetSelectedRace(CharacterDraft draft)
    {
        if (_ruleset is null || string.IsNullOrWhiteSpace(draft.RaceId))
        {
            return null;
        }

        return _ruleset.Races.SingleOrDefault(race => race.Id == draft.RaceId);
    }

    private ClassDefinition? GetSelectedClass(CharacterDraft draft)
    {
        if (_ruleset is null || string.IsNullOrWhiteSpace(draft.ClassId))
        {
            return null;
        }

        return _ruleset.Classes.SingleOrDefault(characterClass => characterClass.Id == draft.ClassId);
    }

    private BackgroundDefinition? GetSelectedBackground(CharacterDraft draft)
    {
        if (_ruleset is null || string.IsNullOrWhiteSpace(draft.BackgroundId))
        {
            return null;
        }

        return _ruleset.Backgrounds.SingleOrDefault(background => background.Id == draft.BackgroundId);
    }
    private static SubraceDefinition? GetSelectedSubrace(
        CharacterDraft draft,
        RaceDefinition? selectedRace)
    {
        if (selectedRace is null || string.IsNullOrWhiteSpace(draft.SubraceId))
        {
            return null;
        }

        return selectedRace.Subraces.SingleOrDefault(subrace => subrace.Id == draft.SubraceId);
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

    private ArmorDefinition? GetEquippedArmor(CharacterDraft draft)
    {
        if (_ruleset is null || string.IsNullOrWhiteSpace(draft.EquippedArmorId))
        {
            return null;
        }

        return _ruleset.Armors.SingleOrDefault(armor => armor.Id == draft.EquippedArmorId);
    }

    private ArmorDefinition? GetEquippedShield(CharacterDraft draft)
    {
        if (_ruleset is null || string.IsNullOrWhiteSpace(draft.EquippedShieldId))
        {
            return null;
        }

        return _ruleset.Armors.SingleOrDefault(armor => armor.Id == draft.EquippedShieldId);
    }

    private IReadOnlyList<WeaponDefinition> GetEquippedWeapons(CharacterDraft draft)
    {
        if (_ruleset is null || draft.EquippedWeaponIds.Count == 0)
        {
            return Array.Empty<WeaponDefinition>();
        }

        return draft.EquippedWeaponIds
            .Select(equippedWeaponId => _ruleset.Weapons.SingleOrDefault(
                weapon => weapon.Id == equippedWeaponId))
            .Where(weapon => weapon is not null)
            .Cast<WeaponDefinition>()
            .ToArray();
    }

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

    private static WeaponAttack CreateWeaponAttack(
        WeaponDefinition weapon,
        ClassDefinition? selectedClass,
        IReadOnlyDictionary<Ability, int> abilityModifiers,
        int proficiencyBonus)
    {
        Ability attackAbility = GetWeaponAttackAbility(weapon, abilityModifiers);
        int abilityModifier = abilityModifiers[attackAbility];

        bool isProficient = IsProficientWithWeapon(weapon, selectedClass);
        int appliedProficiencyBonus = isProficient ? proficiencyBonus : 0;

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
            AttackBonus = abilityModifier + appliedProficiencyBonus,
            Damage = weapon.Damage,
            DamageType = weapon.DamageType,
            DamageBonus = abilityModifier,
            Properties = weapon.Properties,
            NormalRangeFeet = weapon.NormalRangeFeet,
            LongRangeFeet = weapon.LongRangeFeet
        };
    }

    private static Ability GetWeaponAttackAbility(
        WeaponDefinition weapon,
        IReadOnlyDictionary<Ability, int> abilityModifiers)
    {
        bool isFinesse = weapon.Properties.Contains("weapon_property.finesse");

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
            WeaponCategory.Simple => "weapon.simple",
            WeaponCategory.Martial => "weapon.martial",
            _ => throw new InvalidOperationException($"Unsupported weapon category '{weapon.Category}'.")
        };

        return selectedClass.WeaponProficiencies.Contains(categoryProficiencyId)
            || selectedClass.WeaponProficiencies.Contains(weapon.Id);
    }
}
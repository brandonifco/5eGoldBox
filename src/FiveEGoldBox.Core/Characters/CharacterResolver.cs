using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Core.Characters;

public sealed partial class CharacterResolver
{
    private readonly RulesetDefinition? _ruleset;
    private readonly RulesetIndex? _rulesetIndex;

    public CharacterResolver()
    {
    }

    public CharacterResolver(RulesetDefinition ruleset)
    {
        ArgumentNullException.ThrowIfNull(ruleset);

        _ruleset = ruleset;
        _rulesetIndex = new RulesetIndex(ruleset);
    }

    public CharacterResolver(ValidatedRuleset ruleset)
    {
        ArgumentNullException.ThrowIfNull(ruleset);

        _ruleset = ruleset.Definition;
        _rulesetIndex = ruleset.Index;
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
        CharacterSize size = selectedRace?.Size ?? CharacterSize.Medium;
        IReadOnlyList<CharacterSense> senses = ResolveSenses(
            selectedRace,
            selectedSubrace);
        IReadOnlyList<CharacterDamageResponse> damageResponses = ResolveDamageResponses(
            selectedRace,
            selectedSubrace);
        IReadOnlyList<CharacterConditionImmunity> conditionImmunities = ResolveConditionImmunities(
            selectedRace,
            selectedSubrace);
        ArmorDefinition? equippedArmor = GetEquippedArmor(draft);
        ArmorDefinition? equippedShield = GetEquippedShield(draft);
        IReadOnlyList<WeaponDefinition> equippedWeapons = GetEquippedWeapons(draft);

        IReadOnlyDictionary<Ability, int> abilityScores = CalculateAbilityScores(
            draft,
            selectedRace,
            selectedSubrace);

        IReadOnlyDictionary<Ability, int> abilityModifiers = CalculateAbilityModifiers(abilityScores);

        int? armorClass = CalculateArmorClass(
            abilityModifiers[Ability.Dexterity],
            equippedArmor,
            equippedShield);

        int? speedFeet = CalculateSpeedFeet(
            selectedRace,
            equippedArmor,
            abilityScores);

        IReadOnlyList<CharacterMovementSpeed> movementSpeeds = ResolveMovementSpeeds(
            selectedRace,
            selectedSubrace,
            speedFeet);

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

        IReadOnlyList<string> skillProficiencies = ResolveSkillProficiencies(
            draft,
            selectedClass,
            selectedBackground);

        int proficiencyBonus = ProficiencyRules.GetBonus(draft.Level);

        IReadOnlyList<SavingThrowBonus> savingThrowBonuses = ResolveSavingThrowBonuses(
            selectedClass,
            abilityModifiers,
            proficiencyBonus);

        IReadOnlyList<SkillBonus> skillBonuses = ResolveSkillBonuses(
            skillProficiencies,
            abilityModifiers,
            proficiencyBonus,
            equippedArmor);

        IReadOnlyList<WeaponAttack> weaponAttacks = equippedWeapons
            .Select(weapon => CreateWeaponAttack(
                weapon,
                selectedClass,
                abilityModifiers,
                proficiencyBonus,
                draft.InventoryItems,
                size))
            .ToArray();

        int passivePerception = CalculatePassivePerception(
            skillBonuses,
            abilityModifiers);
        int carryingCapacityPounds = abilityScores[Ability.Strength] * 15;
        int pushDragLiftPounds = abilityScores[Ability.Strength] * 30;

        decimal equippedWeightPounds = CalculateEquippedWeight(
            equippedArmor,
            equippedShield,
            equippedWeapons);

        IReadOnlyList<InventoryItemSnapshot> inventoryItems = ResolveInventoryItems(draft);

        decimal inventoryWeightPounds = inventoryItems
            .Sum(item => item.TotalWeightPounds);

        decimal currencyWeightPounds = HasNegativeCurrencyAmount(draft.Currency)
            ? 0m
            : CalculateCurrencyWeight(draft.Currency);

        int currencyValueInCopperPieces = CalculateCurrencyValueInCopperPieces(draft.Currency);

        decimal currencyValueInGoldPieces = currencyValueInCopperPieces / 100m;

        decimal totalCarriedWeightPounds = equippedWeightPounds
            + inventoryWeightPounds
            + currencyWeightPounds;

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
            HitDiceCount = selectedClass is null
                ? null
                : draft.Level,
            MaxHitPoints = CalculateMaxHitPoints(
                selectedClass,
                draft.Level,
                abilityModifiers),
            RaceId = selectedRace?.Id,
            RaceName = selectedRace?.Name,
            Size = size,
            SubraceId = selectedSubrace?.Id,
            SubraceName = selectedSubrace?.Name,
            SpeedFeet = speedFeet,
            MovementSpeeds = movementSpeeds,
            Senses = senses,
            DamageResponses = damageResponses,
            ConditionImmunities = conditionImmunities,
            CarryingCapacityPounds = abilityScores[Ability.Strength] * 15,
            PushDragLiftPounds = abilityScores[Ability.Strength] * 30,
            EquippedWeightPounds = equippedWeightPounds,
            InventoryWeightPounds = inventoryWeightPounds,
            CurrencyWeightPounds = currencyWeightPounds,
            TotalCarriedWeightPounds = totalCarriedWeightPounds,
            IsOverCarryingCapacity = totalCarriedWeightPounds > carryingCapacityPounds,
            InventoryItems = inventoryItems,
            CurrencyValueInCopperPieces = currencyValueInCopperPieces,
            CurrencyValueInGoldPieces = currencyValueInGoldPieces,
            Currency = draft.Currency,
            EquippedArmorId = equippedArmor?.Id,
            EquippedArmorName = equippedArmor?.Name,
            EquippedShieldId = equippedShield?.Id,
            EquippedShieldName = equippedShield?.Name,
            HasStealthDisadvantage = equippedArmor?.HasStealthDisadvantage ?? false,
            ArmorClass = armorClass,
            PassivePerception = passivePerception,
            InitiativeBonus = abilityModifiers[Ability.Dexterity],
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





}

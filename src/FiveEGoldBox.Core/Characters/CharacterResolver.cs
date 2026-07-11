using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Validation;
using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Core.Characters;

public sealed partial class CharacterResolver
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
    private static IReadOnlyList<CharacterConditionImmunity> ResolveConditionImmunities(
        RaceDefinition? selectedRace,
        SubraceDefinition? selectedSubrace)
    {
        HashSet<ConditionType> immunities = [];

        AddConditionImmunities(immunities, selectedRace?.ConditionImmunities);
        AddConditionImmunities(immunities, selectedSubrace?.ConditionImmunities);

        return immunities
            .OrderBy(condition => condition)
            .Select(condition => new CharacterConditionImmunity
            {
                Condition = condition
            })
            .ToArray();
    }

    private static void AddConditionImmunities(
        HashSet<ConditionType> immunities,
        IReadOnlyList<ConditionImmunityDefinition>? conditionImmunities)
    {
        if (conditionImmunities is null)
        {
            return;
        }

        foreach (ConditionImmunityDefinition conditionImmunity in conditionImmunities)
        {
            immunities.Add(conditionImmunity.Condition);
        }
    }
    private static IReadOnlyList<CharacterDamageResponse> ResolveDamageResponses(
        RaceDefinition? selectedRace,
        SubraceDefinition? selectedSubrace)
    {
        HashSet<(string DamageType, DamageResponseType ResponseType)> responses = [];

        AddDamageResponses(responses, selectedRace?.DamageResponses);
        AddDamageResponses(responses, selectedSubrace?.DamageResponses);

        return responses
            .OrderBy(response => response.DamageType)
            .ThenBy(response => response.ResponseType)
            .Select(response => new CharacterDamageResponse
            {
                DamageType = response.DamageType,
                ResponseType = response.ResponseType
            })
            .ToArray();
    }

    private static void AddDamageResponses(
        HashSet<(string DamageType, DamageResponseType ResponseType)> responses,
        IReadOnlyList<DamageResponseDefinition>? damageResponses)
    {
        if (damageResponses is null)
        {
            return;
        }

        foreach (DamageResponseDefinition damageResponse in damageResponses)
        {
            responses.Add((
                damageResponse.DamageType,
                damageResponse.ResponseType));
        }
    }
    private static IReadOnlyList<CharacterMovementSpeed> ResolveMovementSpeeds(
        RaceDefinition? selectedRace,
        SubraceDefinition? selectedSubrace,
        int? speedFeet)
    {
        Dictionary<MovementMode, int> speedsByMode = [];

        AddMovementSpeeds(speedsByMode, selectedRace?.MovementSpeeds);
        AddMovementSpeeds(speedsByMode, selectedSubrace?.MovementSpeeds);

        if (speedFeet is not null)
        {
            speedsByMode[MovementMode.Walk] = speedFeet.Value;
        }

        return speedsByMode
            .OrderBy(speed => speed.Key)
            .Select(speed => new CharacterMovementSpeed
            {
                Mode = speed.Key,
                SpeedFeet = speed.Value
            })
            .ToArray();
    }

    private static void AddMovementSpeeds(
        Dictionary<MovementMode, int> speedsByMode,
        IReadOnlyList<MovementSpeedDefinition>? movementSpeeds)
    {
        if (movementSpeeds is null)
        {
            return;
        }

        foreach (MovementSpeedDefinition movementSpeed in movementSpeeds)
        {
            if (!speedsByMode.TryGetValue(movementSpeed.Mode, out int existingSpeedFeet)
                || movementSpeed.SpeedFeet > existingSpeedFeet)
            {
                speedsByMode[movementSpeed.Mode] = movementSpeed.SpeedFeet;
            }
        }
    }
    private static IReadOnlyList<CharacterSense> ResolveSenses(
        RaceDefinition? selectedRace,
        SubraceDefinition? selectedSubrace)
    {
        Dictionary<SenseType, int> sensesByType = [];

        AddSenses(sensesByType, selectedRace?.Senses);
        AddSenses(sensesByType, selectedSubrace?.Senses);

        return sensesByType
            .OrderBy(sense => sense.Key)
            .Select(sense => new CharacterSense
            {
                Type = sense.Key,
                RangeFeet = sense.Value
            })
            .ToArray();
    }

    private static void AddSenses(
        Dictionary<SenseType, int> sensesByType,
        IReadOnlyList<SenseDefinition>? senses)
    {
        if (senses is null)
        {
            return;
        }

        foreach (SenseDefinition sense in senses)
        {
            if (!sensesByType.TryGetValue(sense.Type, out int existingRangeFeet)
                || sense.RangeFeet > existingRangeFeet)
            {
                sensesByType[sense.Type] = sense.RangeFeet;
            }
        }
    }
    private static bool HasNegativeCurrencyAmount(CurrencyAmount currency)
    {
        return currency.CopperPieces < 0
            || currency.SilverPieces < 0
            || currency.ElectrumPieces < 0
            || currency.GoldPieces < 0
            || currency.PlatinumPieces < 0;
    }
    private static decimal CalculateEquippedWeight(
        ArmorDefinition? equippedArmor,
        ArmorDefinition? equippedShield,
        IReadOnlyList<WeaponDefinition> equippedWeapons)
    {
        decimal totalWeight = 0m;

        if (equippedArmor is not null)
        {
            totalWeight += equippedArmor.WeightPounds;
        }

        if (equippedShield is not null)
        {
            totalWeight += equippedShield.WeightPounds;
        }

        totalWeight += equippedWeapons.Sum(weapon => weapon.WeightPounds);

        return totalWeight;
    }

    private static int? CalculateMaxHitPoints(
        ClassDefinition? selectedClass,
        int level,
        IReadOnlyDictionary<Ability, int> abilityModifiers)
    {
        if (selectedClass is null)
        {
            return null;
        }

        int constitutionModifier = abilityModifiers[Ability.Constitution];
        int firstLevelHitPoints = (int)selectedClass.HitDie + constitutionModifier;

        if (level == 1)
        {
            return firstLevelHitPoints;
        }

        int additionalHitPointsPerLevel =
            GetFixedHitPointsAfterFirstLevel(selectedClass.HitDie)
            + constitutionModifier;

        return firstLevelHitPoints
            + ((level - 1) * additionalHitPointsPerLevel);
    }

    private static int GetFixedHitPointsAfterFirstLevel(DieType hitDie)
    {
        return hitDie switch
        {
            DieType.D6 => 4,
            DieType.D8 => 5,
            DieType.D10 => 6,
            DieType.D12 => 7,
            _ => throw new InvalidOperationException($"Unsupported class hit die '{hitDie}'.")
        };
    }
    private IReadOnlyList<InventoryItemSnapshot> ResolveInventoryItems(CharacterDraft draft)
    {
        if (_ruleset is null || _ruleset.EquipmentItems.Count == 0)
        {
            return Array.Empty<InventoryItemSnapshot>();
        }

        return draft.InventoryItems
            .Select(inventoryItem =>
            {
                EquipmentItemDefinition? definition = _ruleset.EquipmentItems
                    .SingleOrDefault(item => item.Id == inventoryItem.ItemId);

                if (definition is null)
                {
                    return null;
                }

                return new InventoryItemSnapshot
                {
                    ItemId = definition.Id,
                    ItemName = definition.Name,
                    Quantity = inventoryItem.Quantity,
                    UnitWeightPounds = definition.WeightPounds,
                    TotalWeightPounds = definition.WeightPounds * inventoryItem.Quantity,
                    UnitValueInCopperPieces = definition.CostInCopperPieces,
                    TotalValueInCopperPieces = definition.CostInCopperPieces is null
                        ? null
                        : definition.CostInCopperPieces * inventoryItem.Quantity,
                    Tags = definition.Tags
                };
            })
            .Where(item => item is not null)
            .Cast<InventoryItemSnapshot>()
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




    private static int? CalculateSpeedFeet(
        RaceDefinition? selectedRace,
        ArmorDefinition? equippedArmor,
        IReadOnlyDictionary<Ability, int> abilityScores)
    {
        if (selectedRace is null)
        {
            return null;
        }

        int speedFeet = selectedRace.BaseSpeedFeet;

        if (equippedArmor?.StrengthRequirement is not int strengthRequirement)
        {
            return speedFeet;
        }

        int strengthScore = abilityScores[Ability.Strength];

        return strengthScore < strengthRequirement
            ? speedFeet - 10
            : speedFeet;
    }
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
            DisadvantageReasons = disadvantageReasons,
            AttackRollMode = attackRollMode,
            Damage = weapon.Damage,
            VersatileDamage = weapon.VersatileDamage,
            DamageType = weapon.DamageType,
            DamageBonus = damageBonus,
            Properties = weapon.Properties,
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
    }    private static bool IsProficientWithArmor(
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
    private decimal CalculateInventoryWeight(CharacterDraft draft)
    {
        if (_ruleset is null || _ruleset.EquipmentItems.Count == 0)
        {
            return 0m;
        }

        return draft.InventoryItems
            .Where(inventoryItem => inventoryItem.Quantity > 0)
            .Select(inventoryItem =>
            {
                EquipmentItemDefinition? definition = _ruleset.EquipmentItems
                    .SingleOrDefault(item => item.Id == inventoryItem.ItemId);

                return definition is null
                    ? 0m
                    : definition.WeightPounds * inventoryItem.Quantity;
            })
            .Sum();
    }   private static decimal CalculateCurrencyWeight(CurrencyAmount currency)
    {
        int totalCoins = currency.CopperPieces
            + currency.SilverPieces
            + currency.ElectrumPieces
            + currency.GoldPieces
            + currency.PlatinumPieces;

        return totalCoins / 50m;
    }
    private static int CalculateCurrencyValueInCopperPieces(CurrencyAmount currency)
    {
        return currency.CopperPieces
            + (currency.SilverPieces * 10)
            + (currency.ElectrumPieces * 50)
            + (currency.GoldPieces * 100)
            + (currency.PlatinumPieces * 1000);
    }
}
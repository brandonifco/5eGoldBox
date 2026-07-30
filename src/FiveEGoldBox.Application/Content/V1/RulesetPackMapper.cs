using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Application.Content.V1;

/// Turns one or more ruleset content packs into the single RulesetDefinition
/// the engine actually loads.
///
/// Content packs are additive: exactly one pack among those merged together
/// must declare the ruleset's own Id and Name (the "base" pack); every other
/// pack contributes only more races/classes/weapons/spells/etc. Every pack's
/// lists are unioned across every content type. Duplicate IDs within a type
/// are deliberately not checked here — RulesetValidator (via
/// ApplicationRulesetLoader.Load, which every caller of Merge is expected to
/// pass the result through) already rejects those, and reimplementing the
/// check here would just be a second, weaker copy of it.
internal static class RulesetPackMapper
{
    internal const int FormatVersion = 1;

    internal static RulesetDefinition Merge(
        IReadOnlyList<RulesetPackV1> packs)
    {
        ArgumentNullException.ThrowIfNull(packs);

        if (packs.Count == 0)
        {
            throw new ArgumentException(
                "At least one ruleset pack is required.",
                nameof(packs));
        }

        RulesetPackV1[] identityPacks = packs
            .Where(pack => pack.Id is not null || pack.Name is not null)
            .ToArray();

        if (identityPacks.Length != 1)
        {
            throw new ArgumentException(
                $"Exactly one pack must declare the ruleset's Id and Name; found {identityPacks.Length}.",
                nameof(packs));
        }

        RulesetPackV1 basePack = identityPacks[0];

        if (basePack.Id is null || basePack.Name is null)
        {
            throw new ArgumentException(
                "The pack declaring the ruleset's identity must supply both Id and Name.",
                nameof(packs));
        }

        return new RulesetDefinition
        {
            Id = basePack.Id,
            Name = basePack.Name,
            Races = packs
                .SelectMany(pack => pack.Races)
                .Select(ToRuntimeRace)
                .ToArray(),
            Classes = packs
                .SelectMany(pack => pack.Classes)
                .Select(ToRuntimeClass)
                .ToArray(),
            Backgrounds = packs
                .SelectMany(pack => pack.Backgrounds)
                .Select(ToRuntimeBackground)
                .ToArray(),
            Skills = packs
                .SelectMany(pack => pack.Skills)
                .Select(ToRuntimeSkill)
                .ToArray(),
            Armors = packs
                .SelectMany(pack => pack.Armors)
                .Select(ToRuntimeArmor)
                .ToArray(),
            Weapons = packs
                .SelectMany(pack => pack.Weapons)
                .Select(ToRuntimeWeapon)
                .ToArray(),
            Spells = packs
                .SelectMany(pack => pack.Spells)
                .Select(ToRuntimeSpell)
                .ToArray(),
            Effects = packs
                .SelectMany(pack => pack.Effects)
                .Select(ToRuntimeEffect)
                .ToArray(),
            Features = packs
                .SelectMany(pack => pack.Features)
                .Select(ToRuntimeFeature)
                .ToArray(),
            EquipmentItems = packs
                .SelectMany(pack => pack.EquipmentItems)
                .Select(ToRuntimeEquipmentItem)
                .ToArray()
        };
    }

    private static RaceDefinition ToRuntimeRace(
        RaceDefinitionV1 race)
    {
        return new RaceDefinition
        {
            Id = race.Id,
            Name = race.Name,
            Size = ToRuntimeCharacterSize(race.Size),
            BaseSpeedFeet = race.BaseSpeedFeet,
            AbilityScoreIncreases = race.AbilityScoreIncreases
                .Select(ToRuntimeAbilityScoreIncrease)
                .ToArray(),
            Languages = race.Languages.ToArray(),
            Traits = race.Traits.ToArray(),
            Subraces = race.Subraces
                .Select(ToRuntimeSubrace)
                .ToArray(),
            Senses = race.Senses
                .Select(ToRuntimeSense)
                .ToArray(),
            MovementSpeeds = race.MovementSpeeds
                .Select(ToRuntimeMovementSpeed)
                .ToArray(),
            DamageResponses = race.DamageResponses
                .Select(ToRuntimeDamageResponse)
                .ToArray(),
            ConditionImmunities = race.ConditionImmunities
                .Select(ToRuntimeConditionImmunity)
                .ToArray()
        };
    }

    private static SubraceDefinition ToRuntimeSubrace(
        SubraceDefinitionV1 subrace)
    {
        return new SubraceDefinition
        {
            Id = subrace.Id,
            Name = subrace.Name,
            AbilityScoreIncreases = subrace.AbilityScoreIncreases
                .Select(ToRuntimeAbilityScoreIncrease)
                .ToArray(),
            Languages = subrace.Languages.ToArray(),
            Traits = subrace.Traits.ToArray(),
            Senses = subrace.Senses
                .Select(ToRuntimeSense)
                .ToArray(),
            MovementSpeeds = subrace.MovementSpeeds
                .Select(ToRuntimeMovementSpeed)
                .ToArray(),
            DamageResponses = subrace.DamageResponses
                .Select(ToRuntimeDamageResponse)
                .ToArray(),
            ConditionImmunities = subrace.ConditionImmunities
                .Select(ToRuntimeConditionImmunity)
                .ToArray()
        };
    }

    private static AbilityScoreIncrease ToRuntimeAbilityScoreIncrease(
        AbilityScoreIncreaseV1 increase)
    {
        return new AbilityScoreIncrease(
            ToRuntimeAbility(increase.Ability),
            increase.Amount);
    }

    private static SenseDefinition ToRuntimeSense(
        SenseDefinitionV1 sense)
    {
        return new SenseDefinition
        {
            Type = ToRuntimeSenseType(sense.Type),
            RangeFeet = sense.RangeFeet
        };
    }

    private static MovementSpeedDefinition ToRuntimeMovementSpeed(
        MovementSpeedDefinitionV1 movementSpeed)
    {
        return new MovementSpeedDefinition
        {
            Mode = ToRuntimeMovementMode(movementSpeed.Mode),
            SpeedFeet = movementSpeed.SpeedFeet
        };
    }

    private static DamageResponseDefinition ToRuntimeDamageResponse(
        DamageResponseDefinitionV1 damageResponse)
    {
        return new DamageResponseDefinition
        {
            DamageType = damageResponse.DamageType,
            ResponseType = ToRuntimeDamageResponseType(
                damageResponse.ResponseType)
        };
    }

    private static ConditionImmunityDefinition ToRuntimeConditionImmunity(
        ConditionImmunityDefinitionV1 conditionImmunity)
    {
        return new ConditionImmunityDefinition
        {
            Condition = ToRuntimeConditionType(conditionImmunity.Condition)
        };
    }

    private static ClassDefinition ToRuntimeClass(
        ClassDefinitionV1 characterClass)
    {
        ArgumentNullException.ThrowIfNull(characterClass.FeaturesByLevel);
        ArgumentNullException.ThrowIfNull(characterClass.SpellSlotsByLevel);

        return new ClassDefinition
        {
            Id = characterClass.Id,
            Name = characterClass.Name,
            HitDie = ToRuntimeDieType(characterClass.HitDie),
            SavingThrowProficiencies = characterClass.SavingThrowProficiencies
                .Select(ToRuntimeAbility)
                .ToArray(),
            ArmorProficiencies = characterClass.ArmorProficiencies.ToArray(),
            WeaponProficiencies = characterClass.WeaponProficiencies.ToArray(),
            ToolProficiencies = characterClass.ToolProficiencies.ToArray(),
            SkillChoices = characterClass.SkillChoices.ToArray(),
            NumberOfSkillChoices = characterClass.NumberOfSkillChoices,
            FeaturesByLevel = characterClass.FeaturesByLevel
                .ToDictionary(
                    entry => entry.Key,
                    entry => (IReadOnlyList<string>)entry.Value.ToArray()),
            SpellcastingAbility = characterClass.SpellcastingAbility is null
                ? null
                : ToRuntimeAbility(characterClass.SpellcastingAbility.Value),
            SpellSlotsByLevel = characterClass.SpellSlotsByLevel
                .ToDictionary(entry => entry.Key, entry => entry.Value)
        };
    }

    private static BackgroundDefinition ToRuntimeBackground(
        BackgroundDefinitionV1 background)
    {
        return new BackgroundDefinition
        {
            Id = background.Id,
            Name = background.Name,
            SkillProficiencies = background.SkillProficiencies.ToArray(),
            ToolProficiencies = background.ToolProficiencies.ToArray(),
            Languages = background.Languages.ToArray(),
            FeatureId = background.FeatureId
        };
    }

    private static SkillDefinition ToRuntimeSkill(
        SkillDefinitionV1 skill)
    {
        return new SkillDefinition
        {
            Id = skill.Id,
            Name = skill.Name,
            Ability = ToRuntimeAbility(skill.Ability)
        };
    }

    private static ArmorDefinition ToRuntimeArmor(
        ArmorDefinitionV1 armor)
    {
        return new ArmorDefinition
        {
            Id = armor.Id,
            Name = armor.Name,
            Category = ToRuntimeArmorCategory(armor.Category),
            BaseArmorClass = armor.BaseArmorClass,
            AddsDexterityModifier = armor.AddsDexterityModifier,
            MaximumDexterityModifier = armor.MaximumDexterityModifier,
            ArmorClassBonus = armor.ArmorClassBonus,
            StrengthRequirement = armor.StrengthRequirement,
            HasStealthDisadvantage = armor.HasStealthDisadvantage,
            WeightPounds = armor.WeightPounds,
            CostInCopperPieces = armor.CostInCopperPieces
        };
    }

    private static WeaponDefinition ToRuntimeWeapon(
        WeaponDefinitionV1 weapon)
    {
        ArgumentNullException.ThrowIfNull(weapon.Damage);

        return new WeaponDefinition
        {
            Id = weapon.Id,
            Name = weapon.Name,
            Category = ToRuntimeWeaponCategory(weapon.Category),
            AttackKind = ToRuntimeWeaponAttackKind(weapon.AttackKind),
            Damage = ToRuntimeDamageDice(weapon.Damage),
            VersatileDamage = weapon.VersatileDamage is null
                ? null
                : ToRuntimeDamageDice(weapon.VersatileDamage),
            DamageType = weapon.DamageType,
            Properties = weapon.Properties.ToArray(),
            ReachFeet = weapon.ReachFeet,
            NormalRangeFeet = weapon.NormalRangeFeet,
            LongRangeFeet = weapon.LongRangeFeet,
            AmmunitionItemId = weapon.AmmunitionItemId,
            WeightPounds = weapon.WeightPounds,
            CostInCopperPieces = weapon.CostInCopperPieces
        };
    }

    private static DamageDice ToRuntimeDamageDice(
        DamageDiceV1 damageDice)
    {
        return new DamageDice
        {
            Count = damageDice.Count,
            Die = ToRuntimeDieType(damageDice.Die)
        };
    }

    private static SpellDefinition ToRuntimeSpell(
        SpellDefinitionV1 spell)
    {
        return new SpellDefinition
        {
            Id = spell.Id,
            Name = spell.Name,
            Cost = ToRuntimeSpellCostKind(spell.Cost),
            Level = spell.Level,
            CastingTime = ToRuntimeSpellCastingTime(spell.CastingTime),
            RangeKind = ToRuntimeSpellRangeKind(spell.RangeKind),
            RangeFeet = spell.RangeFeet,
            MaximumTargets = spell.MaximumTargets,
            Targets = ToRuntimeSpellTargetDisposition(spell.Targets),
            Resolution = ToRuntimeSpellResolutionKind(spell.Resolution),
            SaveAbility = spell.SaveAbility is null
                ? null
                : ToRuntimeAbility(spell.SaveAbility.Value),
            SaveOutcome = ToRuntimeSpellSaveOutcome(spell.SaveOutcome),
            Effects = spell.Effects
                .Select(ToRuntimeSpellEffect)
                .ToArray(),
            AppliedEffectId = spell.AppliedEffectId,
            RequiresConcentration = spell.RequiresConcentration,
            DurationRounds = spell.DurationRounds
        };
    }

    private static SpellEffectDefinition ToRuntimeSpellEffect(
        SpellEffectDefinitionV1 spellEffect)
    {
        ArgumentNullException.ThrowIfNull(spellEffect.Dice);

        return new SpellEffectDefinition
        {
            Kind = ToRuntimeSpellEffectKind(spellEffect.Kind),
            Dice = ToRuntimeDamageDice(spellEffect.Dice),
            Instances = spellEffect.Instances,
            AddsSpellcastingModifier = spellEffect.AddsSpellcastingModifier,
            DamageType = spellEffect.DamageType
        };
    }

    private static EffectDefinition ToRuntimeEffect(
        EffectDefinitionV1 effect)
    {
        return new EffectDefinition
        {
            Id = effect.Id,
            Name = effect.Name,
            Contributions = effect.Contributions
                .Select(ToRuntimeRollContribution)
                .ToArray()
        };
    }

    private static FeatureDefinition ToRuntimeFeature(
        FeatureDefinitionV1 feature)
    {
        return new FeatureDefinition
        {
            Id = feature.Id,
            Name = feature.Name,
            Contributions = feature.Contributions
                .Select(ToRuntimeRollContribution)
                .ToArray()
        };
    }

    private static RollContributionDefinition ToRuntimeRollContribution(
        RollContributionDefinitionV1 rollContribution)
    {
        return new RollContributionDefinition
        {
            Target = ToRuntimeRollContributionTarget(rollContribution.Target),
            FlatBonus = rollContribution.FlatBonus,
            Dice = rollContribution.Dice is null
                ? null
                : ToRuntimeDamageDice(rollContribution.Dice),
            Conditions = rollContribution.Conditions
                .Select(ToRuntimeRollContributionCondition)
                .ToArray()
        };
    }

    private static EquipmentItemDefinition ToRuntimeEquipmentItem(
        EquipmentItemDefinitionV1 equipmentItem)
    {
        return new EquipmentItemDefinition
        {
            Id = equipmentItem.Id,
            Name = equipmentItem.Name,
            WeightPounds = equipmentItem.WeightPounds,
            CostInCopperPieces = equipmentItem.CostInCopperPieces,
            Tags = equipmentItem.Tags.ToArray()
        };
    }

    internal static Core.Rules.Ability ToRuntimeAbility(
        AbilityV1 ability)
    {
        return ability switch
        {
            AbilityV1.Strength => Core.Rules.Ability.Strength,
            AbilityV1.Dexterity => Core.Rules.Ability.Dexterity,
            AbilityV1.Constitution => Core.Rules.Ability.Constitution,
            AbilityV1.Intelligence => Core.Rules.Ability.Intelligence,
            AbilityV1.Wisdom => Core.Rules.Ability.Wisdom,
            AbilityV1.Charisma => Core.Rules.Ability.Charisma,
            _ => throw new ArgumentOutOfRangeException(
                nameof(ability),
                ability,
                "Unsupported packed ability.")
        };
    }

    private static Core.Rules.CharacterSize ToRuntimeCharacterSize(
        CharacterSizeV1 size)
    {
        return size switch
        {
            CharacterSizeV1.Medium => Core.Rules.CharacterSize.Medium,
            CharacterSizeV1.Small => Core.Rules.CharacterSize.Small,
            _ => throw new ArgumentOutOfRangeException(
                nameof(size),
                size,
                "Unsupported packed character size.")
        };
    }

    private static Core.Rules.DieType ToRuntimeDieType(
        DieTypeV1 die)
    {
        return die switch
        {
            DieTypeV1.D4 => Core.Rules.DieType.D4,
            DieTypeV1.D6 => Core.Rules.DieType.D6,
            DieTypeV1.D8 => Core.Rules.DieType.D8,
            DieTypeV1.D10 => Core.Rules.DieType.D10,
            DieTypeV1.D12 => Core.Rules.DieType.D12,
            DieTypeV1.D20 => Core.Rules.DieType.D20,
            _ => throw new ArgumentOutOfRangeException(
                nameof(die),
                die,
                "Unsupported packed die type.")
        };
    }

    private static Core.Rules.ArmorCategory ToRuntimeArmorCategory(
        ArmorCategoryV1 category)
    {
        return category switch
        {
            ArmorCategoryV1.Light => Core.Rules.ArmorCategory.Light,
            ArmorCategoryV1.Medium => Core.Rules.ArmorCategory.Medium,
            ArmorCategoryV1.Heavy => Core.Rules.ArmorCategory.Heavy,
            ArmorCategoryV1.Shield => Core.Rules.ArmorCategory.Shield,
            _ => throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "Unsupported packed armor category.")
        };
    }

    private static Core.Rules.WeaponCategory ToRuntimeWeaponCategory(
        WeaponCategoryV1 category)
    {
        return category switch
        {
            WeaponCategoryV1.Simple => Core.Rules.WeaponCategory.Simple,
            WeaponCategoryV1.Martial => Core.Rules.WeaponCategory.Martial,
            _ => throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "Unsupported packed weapon category.")
        };
    }

    private static Core.Rules.WeaponAttackKind ToRuntimeWeaponAttackKind(
        WeaponAttackKindV1 attackKind)
    {
        return attackKind switch
        {
            WeaponAttackKindV1.Melee => Core.Rules.WeaponAttackKind.Melee,
            WeaponAttackKindV1.Ranged => Core.Rules.WeaponAttackKind.Ranged,
            _ => throw new ArgumentOutOfRangeException(
                nameof(attackKind),
                attackKind,
                "Unsupported packed weapon attack kind.")
        };
    }

    private static Core.Rules.ConditionType ToRuntimeConditionType(
        ConditionTypeV1 condition)
    {
        return condition switch
        {
            ConditionTypeV1.Blinded => Core.Rules.ConditionType.Blinded,
            ConditionTypeV1.Charmed => Core.Rules.ConditionType.Charmed,
            ConditionTypeV1.Deafened => Core.Rules.ConditionType.Deafened,
            ConditionTypeV1.Exhaustion => Core.Rules.ConditionType.Exhaustion,
            ConditionTypeV1.Frightened => Core.Rules.ConditionType.Frightened,
            ConditionTypeV1.Grappled => Core.Rules.ConditionType.Grappled,
            ConditionTypeV1.Incapacitated =>
                Core.Rules.ConditionType.Incapacitated,
            ConditionTypeV1.Invisible => Core.Rules.ConditionType.Invisible,
            ConditionTypeV1.Paralyzed => Core.Rules.ConditionType.Paralyzed,
            ConditionTypeV1.Petrified => Core.Rules.ConditionType.Petrified,
            ConditionTypeV1.Poisoned => Core.Rules.ConditionType.Poisoned,
            ConditionTypeV1.Prone => Core.Rules.ConditionType.Prone,
            ConditionTypeV1.Restrained => Core.Rules.ConditionType.Restrained,
            ConditionTypeV1.Stunned => Core.Rules.ConditionType.Stunned,
            ConditionTypeV1.Unconscious =>
                Core.Rules.ConditionType.Unconscious,
            _ => throw new ArgumentOutOfRangeException(
                nameof(condition),
                condition,
                "Unsupported packed condition.")
        };
    }

    private static Core.Rules.SenseType ToRuntimeSenseType(
        SenseTypeV1 senseType)
    {
        return senseType switch
        {
            SenseTypeV1.Darkvision => Core.Rules.SenseType.Darkvision,
            SenseTypeV1.Blindsight => Core.Rules.SenseType.Blindsight,
            SenseTypeV1.Tremorsense => Core.Rules.SenseType.Tremorsense,
            SenseTypeV1.Truesight => Core.Rules.SenseType.Truesight,
            _ => throw new ArgumentOutOfRangeException(
                nameof(senseType),
                senseType,
                "Unsupported packed sense type.")
        };
    }

    private static Core.Rules.MovementMode ToRuntimeMovementMode(
        MovementModeV1 mode)
    {
        return mode switch
        {
            MovementModeV1.Walk => Core.Rules.MovementMode.Walk,
            MovementModeV1.Climb => Core.Rules.MovementMode.Climb,
            MovementModeV1.Swim => Core.Rules.MovementMode.Swim,
            MovementModeV1.Fly => Core.Rules.MovementMode.Fly,
            MovementModeV1.Burrow => Core.Rules.MovementMode.Burrow,
            _ => throw new ArgumentOutOfRangeException(
                nameof(mode),
                mode,
                "Unsupported packed movement mode.")
        };
    }

    private static Core.Rules.DamageResponseType ToRuntimeDamageResponseType(
        DamageResponseTypeV1 responseType)
    {
        return responseType switch
        {
            DamageResponseTypeV1.Resistance =>
                Core.Rules.DamageResponseType.Resistance,
            DamageResponseTypeV1.Vulnerability =>
                Core.Rules.DamageResponseType.Vulnerability,
            DamageResponseTypeV1.Immunity =>
                Core.Rules.DamageResponseType.Immunity,
            _ => throw new ArgumentOutOfRangeException(
                nameof(responseType),
                responseType,
                "Unsupported packed damage response type.")
        };
    }

    private static SpellCostKind ToRuntimeSpellCostKind(
        SpellCostKindV1 cost)
    {
        return cost switch
        {
            SpellCostKindV1.Cantrip => SpellCostKind.Cantrip,
            SpellCostKindV1.Slot => SpellCostKind.Slot,
            _ => throw new ArgumentOutOfRangeException(
                nameof(cost),
                cost,
                "Unsupported packed spell cost kind.")
        };
    }

    private static SpellCastingTime ToRuntimeSpellCastingTime(
        SpellCastingTimeV1 castingTime)
    {
        return castingTime switch
        {
            SpellCastingTimeV1.Action => SpellCastingTime.Action,
            SpellCastingTimeV1.BonusAction => SpellCastingTime.BonusAction,
            SpellCastingTimeV1.Reaction => SpellCastingTime.Reaction,
            _ => throw new ArgumentOutOfRangeException(
                nameof(castingTime),
                castingTime,
                "Unsupported packed spell casting time.")
        };
    }

    private static SpellRangeKind ToRuntimeSpellRangeKind(
        SpellRangeKindV1 rangeKind)
    {
        return rangeKind switch
        {
            SpellRangeKindV1.SelfOnly => SpellRangeKind.SelfOnly,
            SpellRangeKindV1.Touch => SpellRangeKind.Touch,
            SpellRangeKindV1.Ranged => SpellRangeKind.Ranged,
            _ => throw new ArgumentOutOfRangeException(
                nameof(rangeKind),
                rangeKind,
                "Unsupported packed spell range kind.")
        };
    }

    private static SpellTargetDisposition ToRuntimeSpellTargetDisposition(
        SpellTargetDispositionV1 targets)
    {
        return targets switch
        {
            SpellTargetDispositionV1.Allies => SpellTargetDisposition.Allies,
            SpellTargetDispositionV1.Enemies =>
                SpellTargetDisposition.Enemies,
            SpellTargetDispositionV1.Any => SpellTargetDisposition.Any,
            _ => throw new ArgumentOutOfRangeException(
                nameof(targets),
                targets,
                "Unsupported packed spell target disposition.")
        };
    }

    private static SpellResolutionKind ToRuntimeSpellResolutionKind(
        SpellResolutionKindV1 resolution)
    {
        return resolution switch
        {
            SpellResolutionKindV1.SpellAttack =>
                SpellResolutionKind.SpellAttack,
            SpellResolutionKindV1.SavingThrow =>
                SpellResolutionKind.SavingThrow,
            SpellResolutionKindV1.Automatic => SpellResolutionKind.Automatic,
            _ => throw new ArgumentOutOfRangeException(
                nameof(resolution),
                resolution,
                "Unsupported packed spell resolution kind.")
        };
    }

    private static SpellSaveOutcome ToRuntimeSpellSaveOutcome(
        SpellSaveOutcomeV1 saveOutcome)
    {
        return saveOutcome switch
        {
            SpellSaveOutcomeV1.Negates => SpellSaveOutcome.Negates,
            SpellSaveOutcomeV1.HalvesDamage => SpellSaveOutcome.HalvesDamage,
            _ => throw new ArgumentOutOfRangeException(
                nameof(saveOutcome),
                saveOutcome,
                "Unsupported packed spell save outcome.")
        };
    }

    private static SpellEffectKind ToRuntimeSpellEffectKind(
        SpellEffectKindV1 kind)
    {
        return kind switch
        {
            SpellEffectKindV1.Damage => SpellEffectKind.Damage,
            SpellEffectKindV1.Healing => SpellEffectKind.Healing,
            _ => throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Unsupported packed spell effect kind.")
        };
    }

    private static RollContributionTarget ToRuntimeRollContributionTarget(
        RollContributionTargetV1 target)
    {
        return target switch
        {
            RollContributionTargetV1.AttackRoll =>
                RollContributionTarget.AttackRoll,
            RollContributionTargetV1.SavingThrow =>
                RollContributionTarget.SavingThrow,
            RollContributionTargetV1.DamageRoll =>
                RollContributionTarget.DamageRoll,
            _ => throw new ArgumentOutOfRangeException(
                nameof(target),
                target,
                "Unsupported packed roll contribution target.")
        };
    }

    private static RollContributionCondition
        ToRuntimeRollContributionCondition(
            RollContributionConditionV1 condition)
    {
        return condition switch
        {
            RollContributionConditionV1.AdvantageOrAdjacentEnemy =>
                RollContributionCondition.AdvantageOrAdjacentEnemy,
            RollContributionConditionV1.FinesseOrRangedWeapon =>
                RollContributionCondition.FinesseOrRangedWeapon,
            _ => throw new ArgumentOutOfRangeException(
                nameof(condition),
                condition,
                "Unsupported packed roll contribution condition.")
        };
    }
}

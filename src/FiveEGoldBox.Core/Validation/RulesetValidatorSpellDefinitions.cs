using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Validation;

public static partial class RulesetValidator
{
    private static void AddSpellDefinitionIssues(
        List<ValidationIssue> issues,
        IReadOnlyList<SpellDefinition> spells,
        IReadOnlyList<EffectDefinition> effects)
    {
        HashSet<string> declaredEffects = new(
            effects.Select(effect => effect.Id),
            StringComparer.Ordinal);

        foreach (SpellDefinition spell in spells)
        {
            if (spell.AppliedEffectId is not null
                && !declaredEffects.Contains(spell.AppliedEffectId))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "ruleset.spells.effect_unknown",
                    $"Ruleset spell '{spell.Id}' applies undeclared effect '{spell.AppliedEffectId}'."));
            }

            string subject = $"Ruleset spell '{spell.Id}'";

            AddSpellCostIssues(issues, spell, subject);
            AddSpellRangeIssues(issues, spell, subject);
            AddSpellResolutionIssues(issues, spell, subject);
            AddSpellEffectIssues(issues, spell, subject);
            AddSpellDurationIssues(issues, spell, subject);

            if (spell.MaximumTargets < 1)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "ruleset.spells.targets.invalid",
                    $"{subject} may target nobody."));
            }
        }
    }

    /// A cantrip is level zero and a slot spell is not, in both directions —
    /// otherwise a spell could claim to be free and still cost a slot.
    private static void AddSpellCostIssues(
        List<ValidationIssue> issues,
        SpellDefinition spell,
        string subject)
    {
        bool isCantrip = spell.Cost == SpellCostKind.Cantrip;

        if (isCantrip && spell.Level != 0)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.spells.level.cantrip_not_zero",
                $"{subject} is a cantrip but claims level {spell.Level}."));
        }

        if (!isCantrip && spell.Level < 1)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.spells.level.slot_not_positive",
                $"{subject} spends a slot but claims level {spell.Level}."));
        }
    }

    private static void AddSpellRangeIssues(
        List<ValidationIssue> issues,
        SpellDefinition spell,
        string subject)
    {
        if (spell.RangeKind == SpellRangeKind.Ranged)
        {
            if (spell.RangeFeet is null or <= 0)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "ruleset.spells.range.missing",
                    $"{subject} is ranged but states no reach."));
            }

            return;
        }

        if (spell.RangeFeet is not null)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.spells.range.unexpected",
                $"{subject} states a reach it cannot use."));
        }
    }

    private static void AddSpellResolutionIssues(
        List<ValidationIssue> issues,
        SpellDefinition spell,
        string subject)
    {
        bool savesAgainstIt =
            spell.Resolution == SpellResolutionKind.SavingThrow;

        if (savesAgainstIt && spell.SaveAbility is null)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.spells.save.ability_missing",
                $"{subject} is resisted by a saving throw but names no ability."));
        }

        if (!savesAgainstIt && spell.SaveAbility is not null)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.spells.save.ability_unexpected",
                $"{subject} names a saving-throw ability but is not resisted by one."));
        }

        if (spell.SaveAbility is not null
            && !Enum.IsDefined(spell.SaveAbility.Value))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.spells.save.ability_undefined",
                $"{subject} is resisted by an undefined ability."));
        }

        // Halving damage on a successful save only means something if the
        // spell deals damage and is saved against at all.
        if (spell.SaveOutcome == SpellSaveOutcome.HalvesDamage
            && (!savesAgainstIt
                || !spell.Effects.Any(effect =>
                    effect.Kind == SpellEffectKind.Damage)))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.spells.save.halves_nothing",
                $"{subject} halves damage on a save but has no damage to halve."));
        }
    }

    private static void AddSpellEffectIssues(
        List<ValidationIssue> issues,
        SpellDefinition spell,
        string subject)
    {
        if (spell.Effects.Count == 0 && spell.AppliedEffectId is null)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.spells.effects.none",
                $"{subject} does nothing."));
        }

        foreach (SpellEffectDefinition effect in spell.Effects)
        {
            if (!Enum.IsDefined(effect.Dice.Die))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "ruleset.spells.effects.die_undefined",
                    $"{subject} rolls undefined die '{(int)effect.Dice.Die}'."));
            }

            if (effect.Dice.Count < 0 || effect.Instances < 1)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "ruleset.spells.effects.quantities",
                    $"{subject} rolls an impossible number of dice."));
            }

            AddSpellDamageTypeIssues(issues, effect, subject);
        }
    }

    private static void AddSpellDamageTypeIssues(
        List<ValidationIssue> issues,
        SpellEffectDefinition effect,
        string subject)
    {
        bool deals = effect.Kind == SpellEffectKind.Damage;

        if (deals && string.IsNullOrWhiteSpace(effect.DamageType))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.spells.effects.damage_type_required",
                $"{subject} deals damage of no stated type."));
        }

        if (!deals && effect.DamageType is not null)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.spells.effects.damage_type_unexpected",
                $"{subject} heals but states a damage type."));
        }
    }

    /// Concentration is how an ongoing spell is sustained, so a spell that
    /// concentrates must last, and one that lasts must say for how long.
    private static void AddSpellDurationIssues(
        List<ValidationIssue> issues,
        SpellDefinition spell,
        string subject)
    {
        if (spell.RequiresConcentration && spell.DurationRounds is null)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.spells.duration.concentration_instantaneous",
                $"{subject} concentrates on something instantaneous."));
        }

        if (spell.DurationRounds is <= 0)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.spells.duration.invalid",
                $"{subject} lasts no time at all."));
        }

        if (spell.AppliedEffectId is not null
            && spell.DurationRounds is null)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.spells.duration.effect_instantaneous",
                $"{subject} installs an effect that expires immediately."));
        }
    }

    private static void AddEffectDefinitionIssues(
        List<ValidationIssue> issues,
        IReadOnlyList<EffectDefinition> effects)
    {
        foreach (EffectDefinition effect in effects)
        {
            AddRollContributionIssues(
                issues,
                effect.Contributions,
                "ruleset.effects",
                $"Ruleset effect '{effect.Id}'",
                // An effect that changes nothing is nothing. A feature that
                // changes nothing is a feature whose other legs are not built
                // yet, which is where Second Wind will sit.
                requireAtLeastOne: true);
        }
    }

    /// The same checks whether a spell's effect or a class feature is doing
    /// the contributing, because a contribution is a contribution. Only the
    /// issue code and the subject differ, so that a broken ruleset still says
    /// which thing is broken.
    private static void AddRollContributionIssues(
        List<ValidationIssue> issues,
        IReadOnlyList<RollContributionDefinition> contributions,
        string codePrefix,
        string subject,
        bool requireAtLeastOne)
    {
        if (requireAtLeastOne
            && contributions.Count == 0)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                $"{codePrefix}.contributes_nothing",
                $"{subject} changes nothing."));
        }

        foreach (RollContributionDefinition contribution in contributions)
        {
            if (!Enum.IsDefined(contribution.Target))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    $"{codePrefix}.target_undefined",
                    $"{subject} changes an undefined kind of roll."));
            }

            if (contribution.Dice is null
                && contribution.FlatBonus == 0)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    $"{codePrefix}.contribution_empty",
                    $"{subject} contributes neither dice nor a bonus."));
            }

            if (contribution.Dice is not null
                && !Enum.IsDefined(contribution.Dice.Die))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    $"{codePrefix}.die_undefined",
                    $"{subject} rolls undefined die '{(int)contribution.Dice.Die}'."));
            }

            if (contribution.Dice is not null
                && contribution.Dice.Count < 1)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    $"{codePrefix}.dice_count_invalid",
                    $"{subject} rolls {contribution.Dice.Count} dice."));
            }

            foreach (RollContributionCondition condition
                in contribution.Conditions)
            {
                if (!Enum.IsDefined(condition))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        $"{codePrefix}.condition_undefined",
                        $"{subject} asks an undefined condition '{(int)condition}'."));
                }
            }
        }
    }

    /// A feature is a contribution with a name, until something reads the
    /// other two legs the design gives it — a granted resource and a granted
    /// action. A feature declaring no contributions is therefore allowed: it
    /// is as inert as the bare string ID it replaced, and no more wrong.
    private static void AddFeatureDefinitionIssues(
        List<ValidationIssue> issues,
        IReadOnlyList<FeatureDefinition> features)
    {
        foreach (FeatureDefinition feature in features)
        {
            AddRollContributionIssues(
                issues,
                feature.Contributions,
                "ruleset.features",
                $"Ruleset feature '{feature.Id}'",
                requireAtLeastOne: false);
        }
    }
}

using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.ContentEditor.Models;

/// A mutable, two-way-bindable stand-in for SpellDefinition -- see
/// WeaponFormModel's header comment for why this exists.
public sealed class SpellFormModel
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public SpellCostKind Cost { get; set; } = SpellCostKind.Cantrip;

    public int Level { get; set; }

    public SpellCastingTime CastingTime { get; set; } = SpellCastingTime.Action;

    public SpellRangeKind RangeKind { get; set; } = SpellRangeKind.SelfOnly;

    /// Meaningful only when RangeKind == Ranged.
    public int? RangeFeet { get; set; }

    public int MaximumTargets { get; set; } = 1;

    public SpellTargetDisposition Targets { get; set; } = SpellTargetDisposition.Enemies;

    public SpellResolutionKind Resolution { get; set; } = SpellResolutionKind.Automatic;

    /// Meaningful only when Resolution == SavingThrow.
    public bool HasSaveAbility { get; set; }

    public Ability SaveAbility { get; set; } = Ability.Dexterity;

    public SpellSaveOutcome SaveOutcome { get; set; } = SpellSaveOutcome.Negates;

    public List<SpellEffectFormModel> Effects { get; set; } = [];

    /// "" means no applied effect (the None option in the picker). This
    /// editor does not manage Effects itself -- the picker is read-only,
    /// sourced from the file's current top-level Effects list.
    public string AppliedEffectId { get; set; } = "";

    public bool RequiresConcentration { get; set; }

    public int? DurationRounds { get; set; }

    public static SpellFormModel FromDefinition(
        SpellDefinition spell)
    {
        return new SpellFormModel
        {
            Id = spell.Id,
            Name = spell.Name,
            Cost = spell.Cost,
            Level = spell.Level,
            CastingTime = spell.CastingTime,
            RangeKind = spell.RangeKind,
            RangeFeet = spell.RangeFeet,
            MaximumTargets = spell.MaximumTargets,
            Targets = spell.Targets,
            Resolution = spell.Resolution,
            HasSaveAbility = spell.SaveAbility is not null,
            SaveAbility = spell.SaveAbility ?? Ability.Dexterity,
            SaveOutcome = spell.SaveOutcome,
            Effects = spell.Effects.Select(SpellEffectFormModel.FromDefinition).ToList(),
            AppliedEffectId = spell.AppliedEffectId ?? "",
            RequiresConcentration = spell.RequiresConcentration,
            DurationRounds = spell.DurationRounds
        };
    }

    public SpellDefinition ToDefinition()
    {
        return new SpellDefinition
        {
            Id = Id,
            Name = Name,
            Cost = Cost,
            Level = Level,
            CastingTime = CastingTime,
            RangeKind = RangeKind,
            RangeFeet = RangeFeet,
            MaximumTargets = MaximumTargets,
            Targets = Targets,
            Resolution = Resolution,
            SaveAbility = HasSaveAbility ? SaveAbility : null,
            SaveOutcome = SaveOutcome,
            Effects = Effects.Select(effect => effect.ToDefinition()).ToList(),
            AppliedEffectId = string.IsNullOrWhiteSpace(AppliedEffectId) ? null : AppliedEffectId,
            RequiresConcentration = RequiresConcentration,
            DurationRounds = DurationRounds
        };
    }
}

public sealed class SpellEffectFormModel
{
    public SpellEffectKind Kind { get; set; } = SpellEffectKind.Damage;

    public int DiceCount { get; set; } = 1;

    public DieType DiceDie { get; set; } = DieType.D6;

    public int Instances { get; set; } = 1;

    public bool AddsSpellcastingModifier { get; set; }

    /// Meaningful only for Kind == Damage.
    public string DamageType { get; set; } = "";

    public static SpellEffectFormModel FromDefinition(
        SpellEffectDefinition effect)
    {
        return new SpellEffectFormModel
        {
            Kind = effect.Kind,
            DiceCount = effect.Dice.Count,
            DiceDie = effect.Dice.Die,
            Instances = effect.Instances,
            AddsSpellcastingModifier = effect.AddsSpellcastingModifier,
            DamageType = effect.DamageType ?? ""
        };
    }

    public SpellEffectDefinition ToDefinition()
    {
        return new SpellEffectDefinition
        {
            Kind = Kind,
            Dice = new DamageDice { Count = DiceCount, Die = DiceDie },
            Instances = Instances,
            AddsSpellcastingModifier = AddsSpellcastingModifier,
            DamageType = string.IsNullOrWhiteSpace(DamageType) ? null : DamageType
        };
    }
}

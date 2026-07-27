using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Characters;

/// Turns an authored spell into a spell this caster can cast.
///
/// The weapon equivalent of this has existed since the beginning: a
/// `WeaponDefinition` plus who is holding it becomes a `WeaponAttack` with the
/// attack and damage bonuses already worked out, so an encounter never needs a
/// ruleset in hand. A spell is the same problem — attack bonus and save DC
/// depend on the caster, everything else is content — and until now nothing
/// did it, so a party's caster reached the battlefield with no spells at all.
internal static class SpellAttackResolver
{
    internal static SpellAttack Create(
        SpellDefinition spell,
        Ability spellcastingAbility,
        IReadOnlyDictionary<Ability, int> abilityModifiers,
        int proficiencyBonus,
        IReadOnlyDictionary<string, EffectDefinition> effectsById)
    {
        ArgumentNullException.ThrowIfNull(spell);
        ArgumentNullException.ThrowIfNull(abilityModifiers);
        ArgumentNullException.ThrowIfNull(effectsById);

        int abilityModifier = abilityModifiers[spellcastingAbility];
        int spellcastingBonus = checked(
            abilityModifier + proficiencyBonus);

        return new SpellAttack
        {
            SpellId = spell.Id,
            SpellName = spell.Name,
            Level = spell.Level,
            SlotResourceId = spell.Cost == SpellCostKind.Cantrip
                ? null
                : RuleIds.Resources.SpellSlot(spell.Level),
            CastingTime = spell.CastingTime,
            RangeKind = spell.RangeKind,
            RangeFeet = spell.RangeFeet,
            MaximumTargets = spell.MaximumTargets,
            Targets = spell.Targets,
            Resolution = spell.Resolution,
            SaveAbility = spell.SaveAbility,
            SaveOutcome = spell.SaveOutcome,
            AttackBonus = spellcastingBonus,

            // 5e's spell save DC. The same two numbers as the attack bonus,
            // which is why a caster who is good at hitting is equally hard to
            // resist.
            SaveDc = checked(8 + spellcastingBonus),
            Effects = spell.Effects
                .Select(effect => CreateEffect(effect, abilityModifier))
                .ToArray(),
            AppliedEffectId = spell.AppliedEffectId,
            AppliedContributions = ResolveContributions(
                spell.AppliedEffectId,
                effectsById),
            RequiresConcentration = spell.RequiresConcentration,
            DurationRounds = spell.DurationRounds
        };
    }

    /// The caster's modifier is folded in here rather than carried alongside,
    /// for the same reason a weapon's damage bonus is: the encounter adds up
    /// what it is given and does not ask who cast it.
    private static SpellAttackEffect CreateEffect(
        SpellEffectDefinition effect,
        int abilityModifier)
    {
        return new SpellAttackEffect
        {
            Kind = effect.Kind,
            Dice = effect.Dice,
            Instances = effect.Instances,
            FlatBonus = effect.AddsSpellcastingModifier
                ? abilityModifier
                : 0,
            DamageType = effect.DamageType
        };
    }

    /// What the effect this spell installs will contribute, resolved with the
    /// spell rather than looked up when it lands.
    private static IReadOnlyList<RollContributionDefinition>
        ResolveContributions(
            string? appliedEffectId,
            IReadOnlyDictionary<string, EffectDefinition> effectsById)
    {
        if (appliedEffectId is null)
        {
            return Array.Empty<RollContributionDefinition>();
        }

        return effectsById.TryGetValue(
            appliedEffectId,
            out EffectDefinition? effect)
            ? effect.Contributions
            : Array.Empty<RollContributionDefinition>();
    }
}

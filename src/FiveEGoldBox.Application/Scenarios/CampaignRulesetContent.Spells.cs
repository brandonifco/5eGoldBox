using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Application.Scenarios;

/// The six spells the scope baseline commits to.
///
/// They are not a sampler. Each one forces a mechanism the others do not — an
/// attack roll, a saving throw, touch range, a bonus action, automatic hits
/// repeated, and an ongoing effect held by concentration — and between them
/// they cover everything the first tier of 5e needs.
internal static partial class CampaignRulesetContent
{
    internal const string FireBoltId = "spell.fire-bolt";

    internal const string SacredFlameId = "spell.sacred-flame";

    internal const string CureWoundsId = "spell.cure-wounds";

    internal const string HealingWordId = "spell.healing-word";

    internal const string MagicMissileId = "spell.magic-missile";

    internal const string BlessId = "spell.bless";

    internal const string BlessEffectId = "effect.bless";

    /// What Bless does while it lasts. Separate from the spell that applies
    /// it, because installing an effect and being one are different things.
    private static IReadOnlyList<EffectDefinition> CreateEffects()
    {
        return
        [
            new EffectDefinition
            {
                Id = BlessEffectId,
                Name = "Blessed",
                Contributions =
                [
                    new RollContributionDefinition
                    {
                        Target = RollContributionTarget.AttackRoll,
                        Dice = new DamageDice { Count = 1, Die = DieType.D4 }
                    },
                    new RollContributionDefinition
                    {
                        Target = RollContributionTarget.SavingThrow,
                        Dice = new DamageDice { Count = 1, Die = DieType.D4 }
                    }
                ]
            }
        ];
    }

    private static IReadOnlyList<SpellDefinition> CreateSpells()
    {
        return
        [
            CreateFireBolt(),
            CreateSacredFlame(),
            CreateCureWounds(),
            CreateHealingWord(),
            CreateMagicMissile(),
            CreateBless()
        ];
    }

    /// Proves the spell attack roll: the caster rolls to hit, as with a
    /// weapon.
    private static SpellDefinition CreateFireBolt()
    {
        return new SpellDefinition
        {
            Id = FireBoltId,
            Name = "Fire Bolt",
            Cost = SpellCostKind.Cantrip,
            Level = 0,
            CastingTime = SpellCastingTime.Action,
            RangeKind = SpellRangeKind.Ranged,
            RangeFeet = 120,
            Targets = SpellTargetDisposition.Enemies,            Resolution = SpellResolutionKind.SpellAttack,
            Effects =
            [
                new SpellEffectDefinition
                {
                    Kind = SpellEffectKind.Damage,
                    Dice = new DamageDice { Count = 1, Die = DieType.D10 },
                    DamageType = "damage.fire"
                }
            ]
        };
    }

    /// Proves the other resolution: the target rolls to avoid it, and the
    /// caster rolls nothing at all.
    private static SpellDefinition CreateSacredFlame()
    {
        return new SpellDefinition
        {
            Id = SacredFlameId,
            Name = "Sacred Flame",
            Cost = SpellCostKind.Cantrip,
            Level = 0,
            CastingTime = SpellCastingTime.Action,
            RangeKind = SpellRangeKind.Ranged,
            RangeFeet = 60,
            Targets = SpellTargetDisposition.Enemies,            Resolution = SpellResolutionKind.SavingThrow,
            SaveAbility = Ability.Dexterity,
            SaveOutcome = SpellSaveOutcome.Negates,
            Effects =
            [
                new SpellEffectDefinition
                {
                    Kind = SpellEffectKind.Damage,
                    Dice = new DamageDice { Count = 1, Die = DieType.D8 },
                    DamageType = "damage.radiant"
                }
            ]
        };
    }

    /// Proves touch range and healing, and is the first thing to spend a slot.
    private static SpellDefinition CreateCureWounds()
    {
        return new SpellDefinition
        {
            Id = CureWoundsId,
            Name = "Cure Wounds",
            Cost = SpellCostKind.Slot,
            Level = 1,
            CastingTime = SpellCastingTime.Action,
            RangeKind = SpellRangeKind.Touch,
            Targets = SpellTargetDisposition.Allies,            Resolution = SpellResolutionKind.Automatic,
            Effects =
            [
                new SpellEffectDefinition
                {
                    Kind = SpellEffectKind.Healing,
                    Dice = new DamageDice { Count = 1, Die = DieType.D8 },
                    AddsSpellcastingModifier = true
                }
            ]
        };
    }

    /// Proves the bonus action, which the encounter has always modelled and
    /// nothing has ever used.
    private static SpellDefinition CreateHealingWord()
    {
        return new SpellDefinition
        {
            Id = HealingWordId,
            Name = "Healing Word",
            Cost = SpellCostKind.Slot,
            Level = 1,
            CastingTime = SpellCastingTime.BonusAction,
            RangeKind = SpellRangeKind.Ranged,
            RangeFeet = 60,
            Targets = SpellTargetDisposition.Allies,            Resolution = SpellResolutionKind.Automatic,
            Effects =
            [
                new SpellEffectDefinition
                {
                    Kind = SpellEffectKind.Healing,
                    Dice = new DamageDice { Count = 1, Die = DieType.D4 },
                    AddsSpellcastingModifier = true
                }
            ]
        };
    }

    /// Proves automatic hits and repeated damage instances. Three darts, each
    /// resolved on its own, so resistance applies to each rather than to the
    /// total.
    private static SpellDefinition CreateMagicMissile()
    {
        return new SpellDefinition
        {
            Id = MagicMissileId,
            Name = "Magic Missile",
            Cost = SpellCostKind.Slot,
            Level = 1,
            CastingTime = SpellCastingTime.Action,
            RangeKind = SpellRangeKind.Ranged,
            RangeFeet = 120,
            MaximumTargets = 3,
            Targets = SpellTargetDisposition.Enemies,            Resolution = SpellResolutionKind.Automatic,
            Effects =
            [
                new SpellEffectDefinition
                {
                    Kind = SpellEffectKind.Damage,
                    Dice = new DamageDice { Count = 1, Die = DieType.D4 },
                    Instances = 3,
                    DamageType = "damage.force"
                }
            ]
        };
    }

    /// Proves concentration, several targets, and an effect that outlives the
    /// casting — and, through the contributions that effect carries, that a
    /// feature can change a roll somebody else makes.
    private static SpellDefinition CreateBless()
    {
        return new SpellDefinition
        {
            Id = BlessId,
            Name = "Bless",
            Cost = SpellCostKind.Slot,
            Level = 1,
            CastingTime = SpellCastingTime.Action,
            RangeKind = SpellRangeKind.Ranged,
            RangeFeet = 30,
            MaximumTargets = 3,
            Targets = SpellTargetDisposition.Allies,            Resolution = SpellResolutionKind.Automatic,
            AppliedEffectId = BlessEffectId,
            RequiresConcentration = true,
            DurationRounds = 10
        };
    }
}

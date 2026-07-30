using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Application.Tests;

/// The six spells the baseline commits to. Each is pinned by the mechanism it
/// exists to force, because that is why these six and not six others.
public sealed class CampaignSpellContentTests
{
    [Fact]
    public void TheCampaignRulesetDeclaresExactlyTheCommittedSix()
    {
        Assert.Equal(
            [
                CampaignRulesetIds.FireBoltId,
                CampaignRulesetIds.SacredFlameId,
                CampaignRulesetIds.CureWoundsId,
                CampaignRulesetIds.HealingWordId,
                CampaignRulesetIds.MagicMissileId,
                CampaignRulesetIds.BlessId
            ],
            Spells().Select(spell => spell.Id));
    }

    [Fact]
    public void FireBolt_IsAnAtWillSpellAttack()
    {
        SpellDefinition spell = Spell(CampaignRulesetIds.FireBoltId);

        Assert.Equal(SpellCostKind.Cantrip, spell.Cost);
        Assert.Equal(SpellResolutionKind.SpellAttack, spell.Resolution);
        Assert.Equal(DieType.D10, Assert.Single(spell.Effects).Dice.Die);
    }

    [Fact]
    public void SacredFlame_IsResolvedByTheTargetsSavingThrow()
    {
        SpellDefinition spell = Spell(CampaignRulesetIds.SacredFlameId);

        Assert.Equal(SpellCostKind.Cantrip, spell.Cost);
        Assert.Equal(SpellResolutionKind.SavingThrow, spell.Resolution);
        Assert.Equal(Ability.Dexterity, spell.SaveAbility);
        Assert.Equal(SpellSaveOutcome.Negates, spell.SaveOutcome);
    }

    [Fact]
    public void CureWounds_HealsAtTouchRangeForASlot()
    {
        SpellDefinition spell = Spell(CampaignRulesetIds.CureWoundsId);

        Assert.Equal(SpellCostKind.Slot, spell.Cost);
        Assert.Equal(SpellRangeKind.Touch, spell.RangeKind);
        Assert.Null(spell.RangeFeet);

        SpellEffectDefinition effect = Assert.Single(spell.Effects);
        Assert.Equal(SpellEffectKind.Healing, effect.Kind);
        Assert.True(effect.AddsSpellcastingModifier);
    }

    /// The one spell that uses the bonus action the encounter has always
    /// modelled and nothing has ever spent.
    [Fact]
    public void HealingWord_IsCastAsABonusAction()
    {
        SpellDefinition spell = Spell(CampaignRulesetIds.HealingWordId);

        Assert.Equal(SpellCastingTime.BonusAction, spell.CastingTime);
        Assert.Equal(SpellRangeKind.Ranged, spell.RangeKind);
        Assert.Equal(
            SpellEffectKind.Healing,
            Assert.Single(spell.Effects).Kind);
    }

    [Fact]
    public void MagicMissile_HitsAutomaticallyAndSeveralTimesOver()
    {
        SpellDefinition spell = Spell(CampaignRulesetIds.MagicMissileId);

        Assert.Equal(SpellResolutionKind.Automatic, spell.Resolution);
        Assert.Equal(3, spell.MaximumTargets);
        Assert.Equal(3, Assert.Single(spell.Effects).Instances);
    }

    [Fact]
    public void Bless_IsHeldByConcentrationAndInstallsAnEffect()
    {
        SpellDefinition spell = Spell(CampaignRulesetIds.BlessId);

        Assert.True(spell.RequiresConcentration);
        Assert.Equal(CampaignRulesetIds.BlessEffectId, spell.AppliedEffectId);
        Assert.Equal(10, spell.DurationRounds);
        Assert.Equal(3, spell.MaximumTargets);
        Assert.Empty(spell.Effects);
    }

    /// Every die these spells roll is one the deterministic sequence can
    /// produce. The d4 Magic Missile and Healing Word need only became
    /// rollable in Phase 7.
    [Fact]
    public void EverySpellDieCanBeRolled()
    {
        Assert.All(
            Spells().SelectMany(spell => spell.Effects),
            effect => Assert.True(Enum.IsDefined(effect.Dice.Die)));
    }

    private static IReadOnlyList<SpellDefinition> Spells()
    {
        return RulesetRegistry
            .Resolve(RulesetRegistry.CampaignRulesetId)
            .Definition
            .Spells;
    }

    private static SpellDefinition Spell(string spellId)
    {
        return Assert.Single(Spells(), spell => spell.Id == spellId);
    }
}

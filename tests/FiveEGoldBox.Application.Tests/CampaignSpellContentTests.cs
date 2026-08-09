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
        Assert.Equal([CampaignRulesetIds.WizardClassId], spell.ClassIds);
    }

    [Fact]
    public void SacredFlame_IsResolvedByTheTargetsSavingThrow()
    {
        SpellDefinition spell = Spell(CampaignRulesetIds.SacredFlameId);

        Assert.Equal(SpellCostKind.Cantrip, spell.Cost);
        Assert.Equal(SpellResolutionKind.SavingThrow, spell.Resolution);
        Assert.Equal(Ability.Dexterity, spell.SaveAbility);
        Assert.Equal(SpellSaveOutcome.Negates, spell.SaveOutcome);
        Assert.Equal([CampaignRulesetIds.ClericClassId], spell.ClassIds);
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
        Assert.Equal([CampaignRulesetIds.ClericClassId], spell.ClassIds);
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
        Assert.Equal([CampaignRulesetIds.ClericClassId], spell.ClassIds);
    }

    [Fact]
    public void MagicMissile_HitsAutomaticallyAndSeveralTimesOver()
    {
        SpellDefinition spell = Spell(CampaignRulesetIds.MagicMissileId);

        Assert.Equal(SpellResolutionKind.Automatic, spell.Resolution);
        Assert.Equal(3, spell.MaximumTargets);
        Assert.Equal(3, Assert.Single(spell.Effects).Instances);
        Assert.Equal([CampaignRulesetIds.WizardClassId], spell.ClassIds);
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
        Assert.Equal([CampaignRulesetIds.ClericClassId], spell.ClassIds);
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

    /// Description is optional on the DTO (existing content that predates
    /// it still loads), but every spell this ruleset actually ships should
    /// carry real, original prose -- the same guarantee
    /// CampaignCharacterChoiceContentTests already holds races/classes/
    /// backgrounds/skills to, extended here since spells aren't part of
    /// CharacterCreationOptions and so aren't covered by that file.
    [Fact]
    public void EverySpell_HasADescription()
    {
        Assert.All(
            Spells(),
            spell => Assert.False(
                string.IsNullOrWhiteSpace(spell.Description),
                $"Spell '{spell.Id}' has no description, so a creation " +
                    "screen can only show its bare name."));
    }

    /// A spell with no ClassIds isn't a spell nobody happens to have picked
    /// yet -- it's one no character creation draft could ever legally
    /// prepare (CharacterCreationRules.CheckPreparedSpells rejects it
    /// regardless of class). Every shipped spell should be reachable by at
    /// least one of the ruleset's own casters.
    [Fact]
    public void EverySpell_NamesAtLeastOneClass()
    {
        Assert.All(
            Spells(),
            spell => Assert.False(
                spell.ClassIds.Count == 0,
                $"Spell '{spell.Id}' names no class, so no character " +
                    "could ever legally prepare it."));
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

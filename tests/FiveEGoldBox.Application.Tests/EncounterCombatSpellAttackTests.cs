using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

/// Spellcasting reaching the live decision surface: a caster is offered its
/// prepared spells, casting one resolves end to end through the same public
/// seam a weapon attack does, and damaging a concentrating target — whether
/// with a spell or a weapon — surfaces the concentration check.
public sealed class EncounterCombatSpellAttackTests
{
    [Fact]
    public void Create_ForACasterWithPreparedSpells_OffersSpellAttackOptions()
    {
        ApplicationSessionState source =
            EncounterCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "party-member.wizard");

        EncounterCombatDecision decision = GetDecision(source);

        Assert.Equal(2, decision.SpellAttacks.Count);
        Assert.Contains(
            decision.SpellAttacks,
            spell => spell.SpellId
                == CampaignRulesetIds.FireBoltId);
        Assert.Contains(
            decision.SpellAttacks,
            spell => spell.SpellId
                == CampaignRulesetIds.MagicMissileId);
    }

    [Fact]
    public void Execute_DamageSpell_ResolvesThroughThePublicSeam()
    {
        ApplicationSessionState source =
            EncounterCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "party-member.wizard");

        EncounterCombatDecision decision = GetDecision(source);
        EncounterCombatSpellAttackOption magicMissile = Assert.Single(
            decision.SpellAttacks,
            spell => spell.SpellId
                == CampaignRulesetIds.MagicMissileId);
        EncounterCombatTargetOption target =
            magicMissile.Targets.First(
                candidate => candidate.IsAvailable);

        EncounterCombatResolutionResult result =
            EncounterCombatRules.Execute(
                source,
                new CombatSpellAttackIntent
                {
                    ExpectedEncounterRevision =
                        decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    SpellId = CampaignRulesetIds.MagicMissileId,
                    TargetCombatantId = target.TargetCombatantId
                });

        Assert.NotNull(result.PrimaryStep);
        Assert.Equal(
            CombatStepKind.SpellAttack,
            result.PrimaryStep.Kind);
        Assert.NotNull(result.PrimaryStep.SpellAttack);
        Assert.True(result.PrimaryStep.SpellAttack!.TookEffect);
        Assert.True(result.PrimaryStep.SpellAttack.DamageDealt > 0);
        Assert.NotEmpty(result.PrimaryStep.Dice);
        Assert.Null(result.PrimaryStep.ConcentrationCheck);
    }

    [Fact]
    public void Execute_HealingSpell_ResolvesThroughThePublicSeam()
    {
        ApplicationSessionState source =
            EncounterCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "party-member.cleric");

        EncounterCombatDecision decision = GetDecision(source);
        EncounterCombatSpellAttackOption healingWord = Assert.Single(
            decision.SpellAttacks,
            spell => spell.SpellId
                == CampaignRulesetIds.HealingWordId);
        EncounterCombatTargetOption target =
            healingWord.Targets.First(
                candidate => candidate.IsAvailable);

        EncounterCombatResolutionResult result =
            EncounterCombatRules.Execute(
                source,
                new CombatSpellAttackIntent
                {
                    ExpectedEncounterRevision =
                        decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    SpellId = CampaignRulesetIds.HealingWordId,
                    TargetCombatantId = target.TargetCombatantId
                });

        Assert.NotNull(result.PrimaryStep);
        Assert.Equal(
            CombatStepKind.SpellAttack,
            result.PrimaryStep.Kind);
        Assert.True(result.PrimaryStep.SpellAttack!.TookEffect);
        Assert.True(result.PrimaryStep.SpellAttack.HealingDone > 0);
    }

    [Fact]
    public void Execute_ConcentrationSpell_AppliesTheEffect()
    {
        ApplicationSessionState source =
            EncounterCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "party-member.cleric");

        EncounterCombatDecision decision = GetDecision(source);
        EncounterCombatSpellAttackOption bless = Assert.Single(
            decision.SpellAttacks,
            spell => spell.SpellId
                == CampaignRulesetIds.BlessId);
        EncounterCombatTargetOption target =
            bless.Targets.Single(
                candidate => string.Equals(
                    candidate.TargetCombatantId,
                    "party-member.cleric",
                    StringComparison.Ordinal));

        Assert.True(target.IsAvailable);

        EncounterCombatResolutionResult result =
            EncounterCombatRules.Execute(
                source,
                new CombatSpellAttackIntent
                {
                    ExpectedEncounterRevision =
                        decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    SpellId = CampaignRulesetIds.BlessId,
                    TargetCombatantId = "party-member.cleric"
                });

        EncounterParticipantState cleric =
            EncounterCombatTestData.GetParticipant(
                result.State,
                "party-member.cleric");

        Assert.Single(cleric.ActiveEffects);
        Assert.Equal(
            CampaignRulesetIds.BlessEffectId,
            cleric.ConcentratingOnEffectId);
    }

    [Fact]
    public void Create_ForABlessAttack_OffersMultiTargetCombinations()
    {
        ApplicationSessionState source =
            EncounterCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "party-member.cleric");

        EncounterCombatDecision decision = GetDecision(source);
        EncounterCombatSpellAttackOption bless = Assert.Single(
            decision.SpellAttacks,
            spell => spell.SpellId
                == CampaignRulesetIds.BlessId);

        Assert.NotEmpty(bless.TargetCombinations);
        Assert.All(
            bless.TargetCombinations,
            combination => Assert.InRange(
                combination.TargetCombatantIds.Count,
                2,
                3));

        // FireBolt reaches only one target, so it should offer none.
        EncounterCombatDecision wizardDecision = GetDecision(
            EncounterCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "party-member.wizard"));
        EncounterCombatSpellAttackOption fireBolt = Assert.Single(
            wizardDecision.SpellAttacks,
            spell => spell.SpellId
                == CampaignRulesetIds.FireBoltId);

        Assert.Empty(fireBolt.TargetCombinations);
    }

    [Fact]
    public void Execute_MultiTargetConcentrationSpell_AppliesTheEffectToEveryNamedTarget()
    {
        ApplicationSessionState source =
            EncounterCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "party-member.cleric");

        EncounterCombatDecision decision = GetDecision(source);
        EncounterCombatSpellAttackOption bless = Assert.Single(
            decision.SpellAttacks,
            spell => spell.SpellId
                == CampaignRulesetIds.BlessId);
        EncounterCombatTargetCombinationOption combination =
            bless.TargetCombinations.First(
                candidate => candidate.TargetCombatantIds.Contains(
                    "party-member.cleric",
                    StringComparer.Ordinal));
        string primaryTarget = combination.TargetCombatantIds[0];
        string[] additionalTargets = combination.TargetCombatantIds
            .Skip(1)
            .ToArray();

        EncounterCombatResolutionResult result =
            EncounterCombatRules.Execute(
                source,
                new CombatSpellAttackIntent
                {
                    ExpectedEncounterRevision =
                        decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    SpellId = CampaignRulesetIds.BlessId,
                    TargetCombatantId = primaryTarget,
                    AdditionalTargetCombatantIds = additionalTargets
                });

        Assert.NotNull(result.PrimaryStep!.SpellAttack);
        Assert.Equal(
            combination.TargetCombatantIds.Count,
            result.PrimaryStep.SpellAttack!.EffectedCombatantIds.Count);

        foreach (string targetId in combination.TargetCombatantIds)
        {
            EncounterParticipantState target =
                EncounterCombatTestData.GetParticipant(
                    result.State,
                    targetId);

            Assert.Single(target.ActiveEffects);
        }
    }

    [Fact]
    public void Execute_DamageSpellIncapacitatingAConcentratingTarget_SurfacesConcentrationCheck()
    {
        ApplicationSessionState source =
            EncounterCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "party-member.wizard");

        EncounterCombatDecision decision = GetDecision(source);
        EncounterCombatSpellAttackOption magicMissile = Assert.Single(
            decision.SpellAttacks,
            spell => spell.SpellId
                == CampaignRulesetIds.MagicMissileId);
        EncounterCombatTargetOption targetOption =
            magicMissile.Targets.First(
                candidate => candidate.IsAvailable);

        source = MakeConcentratingAtOneHitPoint(
            source,
            targetOption.TargetCombatantId);

        EncounterCombatResolutionResult result =
            EncounterCombatRules.Execute(
                source,
                new CombatSpellAttackIntent
                {
                    ExpectedEncounterRevision =
                        decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    SpellId = CampaignRulesetIds.MagicMissileId,
                    TargetCombatantId = targetOption.TargetCombatantId
                });

        Assert.NotNull(result.PrimaryStep!.ConcentrationCheck);
        Assert.True(
            result.PrimaryStep.ConcentrationCheck!
                .BrokenByIncapacitation);
        Assert.True(result.PrimaryStep.ConcentrationCheck.EffectDropped);
    }

    [Fact]
    public void Execute_WeaponAttackIncapacitatingAConcentratingTarget_SurfacesConcentrationCheck()
    {
        ApplicationSessionState source =
            EncounterCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "party-member.fighter");

        EncounterCombatDecision decision = GetDecision(source);
        EncounterCombatWeaponAttackOption weapon =
            decision.WeaponAttacks.First(
                candidate => candidate.IsAvailable);
        EncounterCombatTargetOption targetOption =
            weapon.Targets.First(candidate => candidate.IsAvailable);

        source = MakeConcentratingAtOneHitPoint(
            source,
            targetOption.TargetCombatantId);

        EncounterCombatResolutionResult result =
            EncounterCombatRules.Execute(
                source,
                new CombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision =
                        decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    WeaponId = weapon.WeaponId,
                    TargetCombatantId = targetOption.TargetCombatantId
                });

        Assert.NotNull(result.PrimaryStep!.ConcentrationCheck);
        Assert.True(
            result.PrimaryStep.ConcentrationCheck!
                .BrokenByIncapacitation);
        Assert.True(result.PrimaryStep.ConcentrationCheck.EffectDropped);
    }

    /// Sets the target's hit points to one and gives it something to
    /// concentrate on, so that any nonzero damage from the caller
    /// guarantees an incapacitation-broken check regardless of what the
    /// dice come to.
    private static ApplicationSessionState MakeConcentratingAtOneHitPoint(
        ApplicationSessionState source,
        string targetCombatantId)
    {
        EncounterParticipantState target =
            EncounterCombatTestData.GetParticipant(
                source,
                targetCombatantId);

        target = target with
        {
            Combatant = target.Combatant with
            {
                Health = target.Combatant.Health with
                {
                    HitPoints = target.Combatant.Health.HitPoints
                        with
                    {
                        CurrentHitPoints = 1
                    }
                }
            },
            ActiveEffects =
            [
                new ActiveEffect
                {
                    EffectId = "effect.test-concentration",
                    SourceCombatantId = targetCombatantId,
                    RemainingRounds = 5,
                    RequiresConcentration = true
                }
            ],
            ConcentratingOnEffectId = "effect.test-concentration"
        };

        return EncounterCombatTestData.ReplaceParticipant(
            source,
            target);
    }

    private static EncounterCombatDecision GetDecision(
        ApplicationSessionState state)
    {
        return EncounterCombatRules.AdvanceToDecision(state)
            .ResultingDecision;
    }
}

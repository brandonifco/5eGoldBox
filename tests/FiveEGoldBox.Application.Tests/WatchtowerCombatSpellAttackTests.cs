using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

/// Spellcasting reaching the live decision surface: a caster is offered its
/// prepared spells, casting one resolves end to end through the same public
/// seam a weapon attack does, and damaging a concentrating target — whether
/// with a spell or a weapon — surfaces the concentration check.
public sealed class WatchtowerCombatSpellAttackTests
{
    [Fact]
    public void Create_ForACasterWithPreparedSpells_OffersSpellAttackOptions()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "party-member.wizard");

        WatchtowerCombatDecision decision = GetDecision(source);

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
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "party-member.wizard");

        WatchtowerCombatDecision decision = GetDecision(source);
        WatchtowerCombatSpellAttackOption magicMissile = Assert.Single(
            decision.SpellAttacks,
            spell => spell.SpellId
                == CampaignRulesetIds.MagicMissileId);
        WatchtowerCombatTargetOption target =
            magicMissile.Targets.First(
                candidate => candidate.IsAvailable);

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.Execute(
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
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "party-member.cleric");

        WatchtowerCombatDecision decision = GetDecision(source);
        WatchtowerCombatSpellAttackOption healingWord = Assert.Single(
            decision.SpellAttacks,
            spell => spell.SpellId
                == CampaignRulesetIds.HealingWordId);
        WatchtowerCombatTargetOption target =
            healingWord.Targets.First(
                candidate => candidate.IsAvailable);

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.Execute(
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
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "party-member.cleric");

        WatchtowerCombatDecision decision = GetDecision(source);
        WatchtowerCombatSpellAttackOption bless = Assert.Single(
            decision.SpellAttacks,
            spell => spell.SpellId
                == CampaignRulesetIds.BlessId);
        WatchtowerCombatTargetOption target =
            bless.Targets.Single(
                candidate => string.Equals(
                    candidate.TargetCombatantId,
                    "party-member.cleric",
                    StringComparison.Ordinal));

        Assert.True(target.IsAvailable);

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.Execute(
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
            WatchtowerCombatTestData.GetParticipant(
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
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "party-member.cleric");

        WatchtowerCombatDecision decision = GetDecision(source);
        WatchtowerCombatSpellAttackOption bless = Assert.Single(
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
        WatchtowerCombatDecision wizardDecision = GetDecision(
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "party-member.wizard"));
        WatchtowerCombatSpellAttackOption fireBolt = Assert.Single(
            wizardDecision.SpellAttacks,
            spell => spell.SpellId
                == CampaignRulesetIds.FireBoltId);

        Assert.Empty(fireBolt.TargetCombinations);
    }

    [Fact]
    public void Execute_MultiTargetConcentrationSpell_AppliesTheEffectToEveryNamedTarget()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "party-member.cleric");

        WatchtowerCombatDecision decision = GetDecision(source);
        WatchtowerCombatSpellAttackOption bless = Assert.Single(
            decision.SpellAttacks,
            spell => spell.SpellId
                == CampaignRulesetIds.BlessId);
        WatchtowerCombatTargetCombinationOption combination =
            bless.TargetCombinations.First(
                candidate => candidate.TargetCombatantIds.Contains(
                    "party-member.cleric",
                    StringComparer.Ordinal));
        string primaryTarget = combination.TargetCombatantIds[0];
        string[] additionalTargets = combination.TargetCombatantIds
            .Skip(1)
            .ToArray();

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.Execute(
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
                WatchtowerCombatTestData.GetParticipant(
                    result.State,
                    targetId);

            Assert.Single(target.ActiveEffects);
        }
    }

    [Fact]
    public void Execute_DamageSpellIncapacitatingAConcentratingTarget_SurfacesConcentrationCheck()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "party-member.wizard");

        WatchtowerCombatDecision decision = GetDecision(source);
        WatchtowerCombatSpellAttackOption magicMissile = Assert.Single(
            decision.SpellAttacks,
            spell => spell.SpellId
                == CampaignRulesetIds.MagicMissileId);
        WatchtowerCombatTargetOption targetOption =
            magicMissile.Targets.First(
                candidate => candidate.IsAvailable);

        source = MakeConcentratingAtOneHitPoint(
            source,
            targetOption.TargetCombatantId);

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.Execute(
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
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "party-member.fighter");

        WatchtowerCombatDecision decision = GetDecision(source);
        WatchtowerCombatWeaponAttackOption weapon =
            decision.WeaponAttacks.First(
                candidate => candidate.IsAvailable);
        WatchtowerCombatTargetOption targetOption =
            weapon.Targets.First(candidate => candidate.IsAvailable);

        source = MakeConcentratingAtOneHitPoint(
            source,
            targetOption.TargetCombatantId);

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.Execute(
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
            WatchtowerCombatTestData.GetParticipant(
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

        return WatchtowerCombatTestData.ReplaceParticipant(
            source,
            target);
    }

    private static WatchtowerCombatDecision GetDecision(
        ApplicationSessionState state)
    {
        return WatchtowerCombatRules.AdvanceToDecision(state)
            .ResultingDecision;
    }
}

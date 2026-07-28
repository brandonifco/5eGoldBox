using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

public sealed class EncounterSpellCastEvaluationTests
{
    [Fact]
    public void Evaluate_SpellAttackHit_RequiresPrimaryEffectDice()
    {
        EncounterSpellCastEvaluation result = Evaluate(
            SpellTestData.FireBolt,
            "combatant.enemy",
            firstAttackRoll: 10);

        Assert.True(result.TookEffect);
        Assert.True(result.WouldDealDamage);
        Assert.Equal(
            new[] { DieType.D10 },
            result.RequiredEffectDice);
    }

    [Fact]
    public void Evaluate_SpellAttackMiss_RequiresNoEffectDice()
    {
        EncounterSpellCastEvaluation result = Evaluate(
            SpellTestData.FireBolt,
            "combatant.enemy",
            firstAttackRoll: 1);

        Assert.False(result.TookEffect);
        Assert.False(result.WouldDealDamage);
        Assert.Empty(result.RequiredEffectDice);
    }

    [Fact]
    public void Evaluate_SpellAttackCriticalHit_DoublesEffectDiceCount()
    {
        EncounterSpellCastEvaluation result = Evaluate(
            SpellTestData.FireBolt,
            "combatant.enemy",
            firstAttackRoll: 20);

        Assert.Equal(
            AttackRollOutcome.CriticalHit,
            result.AttackRoll!.Outcome);
        Assert.Equal(
            new[] { DieType.D10, DieType.D10 },
            result.RequiredEffectDice);
    }

    [Fact]
    public void Evaluate_SavingThrowSpellNegates_SuccessfulSaveRequiresNoEffectDice()
    {
        EncounterSpellCastEvaluation result = Evaluate(
            SpellTestData.SacredFlame,
            "combatant.enemy",
            savingThrowRoll: 15);

        Assert.Equal(
            D20TestOutcome.Success,
            result.SavingThrow!.Test.Outcome);
        Assert.False(result.TookEffect);
        Assert.False(result.WouldDealDamage);
        Assert.Empty(result.RequiredEffectDice);
    }

    [Fact]
    public void Evaluate_SavingThrowSpellNegates_FailedSaveRequiresEffectDice()
    {
        EncounterSpellCastEvaluation result = Evaluate(
            SpellTestData.SacredFlame,
            "combatant.enemy",
            savingThrowRoll: 5);

        Assert.Equal(
            D20TestOutcome.Failure,
            result.SavingThrow!.Test.Outcome);
        Assert.True(result.TookEffect);
        Assert.True(result.WouldDealDamage);
        Assert.Equal(
            new[] { DieType.D8 },
            result.RequiredEffectDice);
    }

    [Fact]
    public void Evaluate_AutomaticDamageSpell_RequiresEffectDiceForEveryInstance()
    {
        EncounterSpellCastEvaluation result = Evaluate(
            SpellTestData.MagicMissile,
            "combatant.enemy");

        Assert.True(result.TookEffect);
        Assert.True(result.WouldDealDamage);
        Assert.Equal(
            new[] { DieType.D4, DieType.D4, DieType.D4 },
            result.RequiredEffectDice);
    }

    [Fact]
    public void Evaluate_AutomaticHealingSpell_RequiresEffectDiceButIsNotDamage()
    {
        EncounterSpellCastEvaluation result = Evaluate(
            SpellTestData.CureWounds,
            "combatant.ally");

        Assert.True(result.TookEffect);
        Assert.False(result.WouldDealDamage);
        Assert.Equal(
            new[] { DieType.D8 },
            result.RequiredEffectDice);
    }

    [Fact]
    public void Evaluate_AutomaticEffectOnlySpell_RequiresNoEffectDice()
    {
        EncounterSpellCastEvaluation result = Evaluate(
            SpellTestData.Bless,
            "combatant.ally");

        Assert.True(result.TookEffect);
        Assert.False(result.WouldDealDamage);
        Assert.Empty(result.RequiredEffectDice);
    }

    [Fact]
    public void Evaluate_ReportsAuthoritativeRevisionAndParticipants()
    {
        EncounterState state = SpellTestData.CreateEncounter();

        EncounterSpellCastEvaluation result = Evaluate(
            state,
            SpellTestData.MagicMissile,
            "combatant.enemy");

        Assert.Equal(state.Revision, result.EncounterRevision);
        Assert.Equal("combatant.caster", result.ActorCombatantId);
        Assert.Equal("combatant.enemy", result.TargetCombatantId);
        Assert.Equal(SpellTestData.MagicMissile, result.SpellId);
        Assert.True(result.Prerequisites.IsLegal);
    }

    private static EncounterSpellCastEvaluation Evaluate(
        string spellId,
        string targetCombatantId,
        int? firstAttackRoll = null,
        int? savingThrowRoll = null)
    {
        return Evaluate(
            SpellTestData.CreateEncounter(),
            spellId,
            targetCombatantId,
            firstAttackRoll,
            savingThrowRoll);
    }

    private static EncounterSpellCastEvaluation Evaluate(
        EncounterState state,
        string spellId,
        string targetCombatantId,
        int? firstAttackRoll = null,
        int? savingThrowRoll = null)
    {
        return EncounterSpellRules.Evaluate(
            state,
            new EncounterSpellCastEvaluationCommand
            {
                ExpectedRevision = state.Revision,
                ActorCombatantId = "combatant.caster",
                TargetCombatantId = targetCombatantId,
                SpellId = spellId,
                FirstAttackRoll = firstAttackRoll,
                SavingThrowRoll = savingThrowRoll
            });
    }
}

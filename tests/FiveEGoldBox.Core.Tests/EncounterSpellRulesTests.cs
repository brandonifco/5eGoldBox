using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

/// Casting the two committed cantrips. Fire Bolt is rolled to hit; Sacred
/// Flame is saved against. Every die is handed in, so each case is a fixed
/// arithmetic claim rather than a sample.
public sealed class EncounterSpellRulesTests
{
    /// Attack bonus 5 against armour class 12: a natural 7 reaches 12 and
    /// hits, and the d10 rolled 6.
    [Fact]
    public void Resolve_AnAttackSpellThatHits_DealsItsDamage()
    {
        EncounterSpellCastResult result = Cast(
            SpellTestData.FireBolt,
            attackRoll: 7,
            effectRolls: [6]);

        Assert.True(result.TookEffect);
        Assert.Equal(AttackRollOutcome.Hit, result.AttackRoll!.Outcome);
        Assert.Equal(6, result.DamageDealt);
        Assert.Equal(
            4,
            result.TargetDamage!.State.Health.HitPoints.CurrentHitPoints);
    }

    [Fact]
    public void Resolve_AnAttackSpellThatMisses_DealsNothing()
    {
        EncounterSpellCastResult result = Cast(
            SpellTestData.FireBolt,
            attackRoll: 2,
            effectRolls: [6]);

        Assert.False(result.TookEffect);
        Assert.Equal(AttackRollOutcome.Miss, result.AttackRoll!.Outcome);
        Assert.Equal(0, result.DamageDealt);
        Assert.Null(result.TargetDamage);
    }

    /// A natural twenty doubles the dice, so two d10s are wanted rather than
    /// one.
    [Fact]
    public void Resolve_ACriticalSpellAttack_DoublesItsDice()
    {
        EncounterSpellCastResult result = Cast(
            SpellTestData.FireBolt,
            attackRoll: 20,
            effectRolls: [6, 4]);

        Assert.Equal(
            AttackRollOutcome.CriticalHit,
            result.AttackRoll!.Outcome);
        Assert.Equal(10, result.DamageDealt);
    }

    /// Sacred Flame negates on a success, so a save means nothing at all
    /// happens — and the caster still rolled nothing.
    [Fact]
    public void Resolve_ASavingThrowSpellThatIsSaved_DoesNothing()
    {
        EncounterSpellCastResult result = Cast(
            SpellTestData.SacredFlame,
            savingThrowRoll: 20,
            effectRolls: [5]);

        Assert.Null(result.AttackRoll);
        Assert.Equal(D20TestOutcome.Success, result.SavingThrow!.Test.Outcome);
        Assert.False(result.TookEffect);
        Assert.Equal(0, result.DamageDealt);
    }

    [Fact]
    public void Resolve_ASavingThrowSpellThatIsFailed_DealsItsDamage()
    {
        EncounterSpellCastResult result = Cast(
            SpellTestData.SacredFlame,
            savingThrowRoll: 3,
            effectRolls: [5]);

        Assert.Equal(D20TestOutcome.Failure, result.SavingThrow!.Test.Outcome);
        Assert.True(result.TookEffect);
        Assert.Equal(5, result.DamageDealt);
    }

    /// Casting costs the turn whether or not the spell lands. A missed Fire
    /// Bolt is still a spent action.
    [Fact]
    public void Resolve_SpendsTheActionEvenOnAMiss()
    {
        EncounterSpellCastResult result = Cast(
            SpellTestData.FireBolt,
            attackRoll: 2,
            effectRolls: [6]);

        Assert.False(
            Caster(result.State).TurnResources.HasActionAvailable);
        Assert.True(
            Caster(result.State).TurnResources.HasBonusActionAvailable);
    }

    [Fact]
    public void Resolve_AdvancesTheRevisionExactlyOnce()
    {
        EncounterState state = SpellTestData.CreateEncounter();

        EncounterSpellCastResult hit = Resolve(state, Command(
            SpellTestData.FireBolt,
            state.Revision,
            attackRoll: 7,
            effectRolls: [6]));
        EncounterSpellCastResult miss = Resolve(state, Command(
            SpellTestData.FireBolt,
            state.Revision,
            attackRoll: 2,
            effectRolls: [6]));

        Assert.Equal(state.Revision + 1, hit.State.Revision);
        Assert.Equal(state.Revision + 1, miss.State.Revision);
    }

    [Fact]
    public void Resolve_WithAStaleRevision_Throws()
    {
        EncounterState state = SpellTestData.CreateEncounter();

        Assert.Throws<InvalidOperationException>(() =>
            Resolve(state, Command(
                SpellTestData.FireBolt,
                state.Revision + 5,
                attackRoll: 7,
                effectRolls: [6])));
    }

    /// The command must carry the roll its spell actually uses, and only that
    /// one — a saving-throw spell handed an attack roll is a caller error.
    [Fact]
    public void Resolve_WithTheWrongKindOfRoll_Throws()
    {
        EncounterState state = SpellTestData.CreateEncounter();

        Assert.Throws<ArgumentException>(() =>
            Resolve(state, Command(
                SpellTestData.SacredFlame,
                state.Revision,
                attackRoll: 7,
                effectRolls: [5])));
        Assert.Throws<ArgumentException>(() =>
            Resolve(state, Command(
                SpellTestData.FireBolt,
                state.Revision,
                savingThrowRoll: 7,
                effectRolls: [6])));
    }

    [Fact]
    public void Resolve_WithTooFewDice_Throws()
    {
        EncounterState state = SpellTestData.CreateEncounter();

        Assert.Throws<ArgumentException>(() =>
            Resolve(state, Command(
                SpellTestData.FireBolt,
                state.Revision,
                attackRoll: 20,
                effectRolls: [6])));
    }

    [Fact]
    public void Resolve_AnIllegalCast_Throws()
    {
        EncounterState state = SpellTestData.CreateEncounter();

        Assert.Throws<InvalidOperationException>(() =>
            EncounterSpellRules.Resolve(state, new EncounterSpellCastCommand
            {
                ExpectedRevision = state.Revision,
                ActorCombatantId = "combatant.caster",
                TargetCombatantId = "combatant.ally",
                SpellId = SpellTestData.FireBolt,
                FirstAttackRoll = 7,
                EffectRolls = [6]
            }));
    }

    private static EncounterParticipantState Caster(
        EncounterState state)
    {
        return state.Participants.Single(participant =>
            participant.Combatant.CombatantId == "combatant.caster");
    }

    private static EncounterSpellCastResult Cast(
        string spellId,
        IReadOnlyList<int> effectRolls,
        int? attackRoll = null,
        int? savingThrowRoll = null)
    {
        EncounterState state = SpellTestData.CreateEncounter();

        return Resolve(state, Command(
            spellId,
            state.Revision,
            effectRolls,
            attackRoll,
            savingThrowRoll));
    }

    private static EncounterSpellCastResult Resolve(
        EncounterState state,
        EncounterSpellCastCommand command)
    {
        return EncounterSpellRules.Resolve(state, command);
    }

    private static EncounterSpellCastCommand Command(
        string spellId,
        long revision,
        IReadOnlyList<int> effectRolls,
        int? attackRoll = null,
        int? savingThrowRoll = null)
    {
        return new EncounterSpellCastCommand
        {
            ExpectedRevision = revision,
            ActorCombatantId = "combatant.caster",
            TargetCombatantId = "combatant.enemy",
            SpellId = spellId,
            FirstAttackRoll = attackRoll,
            SavingThrowRoll = savingThrowRoll,
            EffectRolls = effectRolls
        };
    }
}

using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

/// Effects that outlive the casting, and the concentration that sustains
/// them: whether they arrive, who they land on, and when they stop. What they
/// then contribute to a roll is EncounterRollContributionTests.
public sealed class EncounterSpellEffectTests
{
    [Fact]
    public void Cast_ASustainedSpell_SettlesItsEffectOnItsTarget()
    {
        EncounterSpellCastResult result = Cast(
            SpellTestData.Bless,
            "combatant.ally");

        ActiveEffect effect = Assert.Single(
            Find(result.State, "combatant.ally").ActiveEffects);

        Assert.Equal(SpellTestData.BlessEffect, effect.EffectId);
        Assert.Equal("combatant.caster", effect.SourceCombatantId);
        Assert.Equal(10, effect.RemainingRounds);
        Assert.True(effect.RequiresConcentration);
        Assert.NotEmpty(effect.Contributions);
    }

    [Fact]
    public void Cast_ASustainedSpell_TakesTheCastersConcentration()
    {
        EncounterSpellCastResult result = Cast(
            SpellTestData.Bless,
            "combatant.ally");

        Assert.Equal(
            SpellTestData.BlessEffect,
            Find(result.State, "combatant.caster").ConcentratingOnEffectId);
    }

    /// Bless reaches three, and the result says who it settled on.
    [Fact]
    public void Cast_ReachesEveryTargetNamed()
    {
        EncounterSpellCastResult result = Cast(
            SpellTestData.Bless,
            "combatant.ally",
            additionalTargets: ["combatant.caster"]);

        Assert.Equal(
            ["combatant.ally", "combatant.caster"],
            result.EffectedCombatantIds);
        Assert.Single(Find(result.State, "combatant.ally").ActiveEffects);
        Assert.Single(Find(result.State, "combatant.caster").ActiveEffects);
    }

    [Fact]
    public void Cast_NamingMoreTargetsThanTheSpellReaches_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Cast(
                SpellTestData.Shield,
                "combatant.ally",
                additionalTargets: ["combatant.caster"]));
    }

    [Fact]
    public void Cast_NamingTheSameTargetTwice_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Cast(
                SpellTestData.Bless,
                "combatant.ally",
                additionalTargets: ["combatant.ally"]));
    }

    [Fact]
    public void Cast_NamingATargetItCannotReach_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Cast(
                SpellTestData.Bless,
                "combatant.ally",
                additionalTargets: ["combatant.enemy"]));
    }

    /// A caster sustains one thing at a time, so starting a second drops the
    /// first — and drops it everywhere, because an effect nobody is
    /// concentrating on is over.
    [Fact]
    public void Cast_ASecondSustainedSpell_DropsTheFirstEverywhere()
    {
        EncounterState blessed = Cast(
            SpellTestData.Bless,
            "combatant.ally",
            additionalTargets: ["combatant.caster"])
            .State;

        blessed = BeginANewTurn(blessed);

        EncounterSpellCastResult result = EncounterSpellRules.Resolve(
            blessed,
            new EncounterSpellCastCommand
            {
                ExpectedRevision = blessed.Revision,
                ActorCombatantId = "combatant.caster",
                TargetCombatantId = "combatant.ally",
                SpellId = SpellTestData.Shield,
                EffectRolls = []
            });

        Assert.DoesNotContain(
            Find(result.State, "combatant.ally").ActiveEffects,
            effect => effect.EffectId == SpellTestData.BlessEffect);
        Assert.DoesNotContain(
            Find(result.State, "combatant.caster").ActiveEffects,
            effect => effect.EffectId == SpellTestData.BlessEffect);
        Assert.Equal(
            SpellTestData.ShieldEffect,
            Find(result.State, "combatant.caster").ConcentratingOnEffectId);
    }

    /// Casting the same sustained spell again refreshes rather than stacks.
    [Fact]
    public void Cast_TheSameEffectTwice_LeavesOneOfIt()
    {
        EncounterState blessed = Cast(
            SpellTestData.Bless,
            "combatant.ally")
            .State;

        blessed = BeginANewTurn(blessed);

        EncounterSpellCastResult result = EncounterSpellRules.Resolve(
            blessed,
            new EncounterSpellCastCommand
            {
                ExpectedRevision = blessed.Revision,
                ActorCombatantId = "combatant.caster",
                TargetCombatantId = "combatant.ally",
                SpellId = SpellTestData.Bless,
                EffectRolls = []
            });

        Assert.Single(Find(result.State, "combatant.ally").ActiveEffects);
    }

    [Fact]
    public void Cast_DamagingAConcentratingTarget_SurfacesTheConcentrationCheck()
    {
        EncounterState state = SpellTestData.CreateEncounter();

        EncounterParticipantState[] participants =
            state.Participants.ToArray();
        int enemyIndex = Array.FindIndex(
            participants,
            participant => participant.Combatant.CombatantId
                == "combatant.enemy");

        participants[enemyIndex] = participants[enemyIndex] with
        {
            ActiveEffects =
            [
                new ActiveEffect
                {
                    EffectId = SpellTestData.BlessEffect,
                    SourceCombatantId = "combatant.enemy",
                    RemainingRounds = 5,
                    RequiresConcentration = true
                }
            ],
            ConcentratingOnEffectId = SpellTestData.BlessEffect
        };

        state = state with
        {
            Participants = Array.AsReadOnly(participants)
        };

        EncounterSpellCastResult result =
            EncounterSpellRules.Resolve(
                state,
                new EncounterSpellCastCommand
                {
                    ExpectedRevision = state.Revision,
                    ActorCombatantId = "combatant.caster",
                    TargetCombatantId = "combatant.enemy",
                    SpellId = SpellTestData.FireBolt,
                    FirstAttackRoll = 7,
                    EffectRolls = [6],
                    ConcentrationSavingThrowRoll = 1
                });

        Assert.NotNull(result.ConcentrationCheck);
        Assert.True(result.ConcentrationCheck!.EffectDropped);
        Assert.Empty(
            Find(result.State, "combatant.enemy")
                .ActiveEffects);
        Assert.Null(
            Find(result.State, "combatant.enemy")
                .ConcentratingOnEffectId);
    }

    [Fact]
    public void Cast_ASpellWithNoEffect_LeavesNothingBehind()
    {
        EncounterState state = SpellTestData.CreateEncounter();

        EncounterSpellCastResult result = EncounterSpellRules.Resolve(
            state,
            new EncounterSpellCastCommand
            {
                ExpectedRevision = state.Revision,
                ActorCombatantId = "combatant.caster",
                TargetCombatantId = "combatant.enemy",
                SpellId = SpellTestData.FireBolt,
                FirstAttackRoll = 7,
                EffectRolls = [6]
            });

        Assert.Empty(result.EffectedCombatantIds);
        Assert.All(
            result.State.Participants,
            participant => Assert.Empty(participant.ActiveEffects));
        Assert.Null(
            Find(result.State, "combatant.caster").ConcentratingOnEffectId);
    }

    private static EncounterSpellCastResult Cast(
        string spellId,
        string targetId,
        IReadOnlyList<string>? additionalTargets = null)
    {
        EncounterState state = SpellTestData.CreateEncounter(
            allyPosition: new GridPosition(2, 1));

        return EncounterSpellRules.Resolve(
            state,
            new EncounterSpellCastCommand
            {
                ExpectedRevision = state.Revision,
                ActorCombatantId = "combatant.caster",
                TargetCombatantId = targetId,
                AdditionalTargetCombatantIds =
                    additionalTargets ?? Array.Empty<string>(),
                SpellId = spellId,
                EffectRolls = []
            });
    }

    /// Casting twice takes two turns — the first cast spent the action, which
    /// is exactly what it should do.
    private static EncounterState BeginANewTurn(
        EncounterState state)
    {
        EncounterParticipantState[] participants =
            state.Participants.ToArray();
        int caster = Array.FindIndex(
            participants,
            participant => participant.Combatant.CombatantId
                == "combatant.caster");

        participants[caster] = participants[caster] with
        {
            TurnResources = CombatTurnResourceRules.StartTurn(
                movementSpeedFeet: 30)
        };

        return state with
        {
            Participants = Array.AsReadOnly(participants)
        };
    }

    private static EncounterParticipantState Find(
        EncounterState state,
        string combatantId)
    {
        return state.Participants.Single(participant =>
            participant.Combatant.CombatantId == combatantId);
    }
}

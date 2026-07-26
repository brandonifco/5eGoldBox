using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

/// The three spells that spend a slot. Cure Wounds heals at touch, Healing
/// Word heals as a bonus action, and Magic Missile deals its damage three
/// times over without rolling to hit.
public sealed class EncounterSpellSlotAndHealingTests
{
    [Fact]
    public void Resolve_ASlotSpell_SpendsOne()
    {
        EncounterSpellCastResult result = CastAtAlly(
            SpellTestData.CureWounds,
            effectRolls: [5]);

        Assert.Equal(1, SlotsLeft(result.State));
    }

    [Fact]
    public void Resolve_ACantrip_SpendsNoSlot()
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

        Assert.Equal(2, SlotsLeft(result.State));
    }

    /// A caster who has spent everything is offered the spell no longer.
    [Fact]
    public void Evaluate_WithNoSlotsLeft_IsUnavailable()
    {
        EncounterSpellPrerequisiteEvaluation result =
            EncounterSpellPrerequisiteRules.Evaluate(
                Exhausted(SpellTestData.CreateEncounter()),
                "combatant.caster",
                "combatant.ally",
                SpellTestData.CureWounds);

        Assert.False(result.IsLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason.SpellSlotUnavailable,
            result.UnavailabilityReason);
    }

    [Fact]
    public void Evaluate_ACantripWithNoSlotsLeft_IsStillAvailable()
    {
        Assert.True(
            EncounterSpellPrerequisiteRules.Evaluate(
                Exhausted(SpellTestData.CreateEncounter()),
                "combatant.caster",
                "combatant.enemy",
                SpellTestData.FireBolt)
                .IsLegal);
    }

    /// A d8 rolling 5 plus the caster's modifier of 3.
    [Fact]
    public void Resolve_CureWounds_HealsTheDiceAndTheModifier()
    {
        EncounterSpellCastResult result = CastAtAlly(
            SpellTestData.CureWounds,
            effectRolls: [5]);

        Assert.Equal(8, result.HealingDone);
        Assert.Equal(0, result.DamageDealt);
        Assert.Equal(10, AllyHitPoints(result.State));
    }

    [Fact]
    public void Resolve_HealingWord_SpendsTheBonusActionNotTheAction()
    {
        EncounterSpellCastResult result = CastAtAlly(
            SpellTestData.HealingWord,
            effectRolls: [4]);

        Assert.Equal(7, result.HealingDone);
        Assert.True(Caster(result.State).TurnResources.HasActionAvailable);
        Assert.False(
            Caster(result.State).TurnResources.HasBonusActionAvailable);
    }

    /// Three darts, each a d4 plus one, and none of them rolled to hit.
    [Fact]
    public void Resolve_MagicMissile_DealsEveryDartWithoutAnAttackRoll()
    {
        EncounterState state = SpellTestData.CreateEncounter();

        EncounterSpellCastResult result = EncounterSpellRules.Resolve(
            state,
            new EncounterSpellCastCommand
            {
                ExpectedRevision = state.Revision,
                ActorCombatantId = "combatant.caster",
                TargetCombatantId = "combatant.enemy",
                SpellId = SpellTestData.MagicMissile,
                EffectRolls = [3, 1, 4]
            });

        Assert.Null(result.AttackRoll);
        Assert.Null(result.SavingThrow);
        Assert.True(result.TookEffect);
        Assert.Equal(11, result.DamageDealt);
    }

    /// Each dart is resolved on its own, so resistance halves every one rather
    /// than halving their total. Three darts of 4 become 2 each, not 12 to 6.
    [Fact]
    public void Resolve_MagicMissileAgainstResistance_HalvesEachDart()
    {
        EncounterState state = ResistForce(SpellTestData.CreateEncounter());

        EncounterSpellCastResult result = EncounterSpellRules.Resolve(
            state,
            new EncounterSpellCastCommand
            {
                ExpectedRevision = state.Revision,
                ActorCombatantId = "combatant.caster",
                TargetCombatantId = "combatant.enemy",
                SpellId = SpellTestData.MagicMissile,
                EffectRolls = [3, 3, 3]
            });

        Assert.Equal(6, result.DamageDealt);
    }

    [Fact]
    public void Resolve_MagicMissileWithTooFewDarts_Throws()
    {
        EncounterState state = SpellTestData.CreateEncounter();

        Assert.Throws<ArgumentException>(() =>
            EncounterSpellRules.Resolve(
                state,
                new EncounterSpellCastCommand
                {
                    ExpectedRevision = state.Revision,
                    ActorCombatantId = "combatant.caster",
                    TargetCombatantId = "combatant.enemy",
                    SpellId = SpellTestData.MagicMissile,
                    EffectRolls = [3, 1]
                }));
    }

    private static EncounterSpellCastResult CastAtAlly(
        string spellId,
        IReadOnlyList<int> effectRolls)
    {
        EncounterState state = WoundAlly(
            SpellTestData.CreateEncounter(
                allyPosition: new GridPosition(2, 1)));

        return EncounterSpellRules.Resolve(
            state,
            new EncounterSpellCastCommand
            {
                ExpectedRevision = state.Revision,
                ActorCombatantId = "combatant.caster",
                TargetCombatantId = "combatant.ally",
                SpellId = spellId,
                EffectRolls = effectRolls
            });
    }

    private static int SlotsLeft(
        EncounterState state)
    {
        return Caster(state).CombatProfile.Resources
            .Single(resource =>
                resource.ResourceId == SpellTestData.FirstLevelSlot)
            .Remaining;
    }

    private static int AllyHitPoints(
        EncounterState state)
    {
        return Find(state, "combatant.ally")
            .Combatant.Health.HitPoints.CurrentHitPoints;
    }

    private static EncounterParticipantState Caster(
        EncounterState state)
    {
        return Find(state, "combatant.caster");
    }

    private static EncounterParticipantState Find(
        EncounterState state,
        string combatantId)
    {
        return state.Participants.Single(participant =>
            participant.Combatant.CombatantId == combatantId);
    }

    private static EncounterState Exhausted(
        EncounterState state)
    {
        return Replace(state, "combatant.caster", participant => participant
            with
            {
                CombatProfile = participant.CombatProfile with
                {
                    Resources = participant.CombatProfile.Resources
                        .Select(resource => resource with { Remaining = 0 })
                        .ToArray()
                }
            });
    }

    private static EncounterState WoundAlly(
        EncounterState state)
    {
        return Replace(state, "combatant.ally", participant => participant with
        {
            Combatant = participant.Combatant with
            {
                Health = participant.Combatant.Health with
                {
                    HitPoints = participant.Combatant.Health.HitPoints with
                    {
                        CurrentHitPoints = 2
                    }
                }
            }
        });
    }

    private static EncounterState ResistForce(
        EncounterState state)
    {
        return Replace(state, "combatant.enemy", participant => participant
            with
            {
                CombatProfile = participant.CombatProfile with
                {
                    DamageResponses =
                    [
                        new CharacterDamageResponse
                        {
                            DamageType = "damage.force",
                            ResponseType = DamageResponseType.Resistance
                        }
                    ]
                }
            });
    }

    private static EncounterState Replace(
        EncounterState state,
        string combatantId,
        Func<EncounterParticipantState, EncounterParticipantState> replace)
    {
        EncounterParticipantState[] participants =
            state.Participants.ToArray();
        int index = Array.FindIndex(
            participants,
            participant => participant.Combatant.CombatantId == combatantId);

        participants[index] = replace(participants[index]);

        return state with
        {
            Participants = Array.AsReadOnly(participants)
        };
    }
}

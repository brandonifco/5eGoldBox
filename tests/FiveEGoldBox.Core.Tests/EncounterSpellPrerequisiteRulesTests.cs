using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

/// Whether a spell may be cast, using the two cantrips the baseline commits
/// to. Fire Bolt is rolled to hit; Sacred Flame is saved against. Neither
/// spends a slot, so this covers both resolution paths without resources.
public sealed class EncounterSpellPrerequisiteRulesTests
{
    private const string FireBolt = SpellTestData.FireBolt;

    private const string SacredFlame = SpellTestData.SacredFlame;

    private const string CureWounds = SpellTestData.CureWounds;

    [Fact]
    public void Evaluate_ACantripInRange_IsLegal()
    {
        EncounterSpellPrerequisiteEvaluation result =
            Evaluate(SpellTestData.CreateEncounter(), FireBolt);

        Assert.True(result.IsLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason.None,
            result.UnavailabilityReason);
        Assert.Equal(15, result.DistanceFeet);
    }

    /// A spell the caster rolls to hit has a roll mode. One the target saves
    /// against does not, because the caster rolls nothing.
    [Fact]
    public void Evaluate_OnlyAnAttackSpellHasARollMode()
    {
        Assert.Equal(
            D20RollMode.Normal,
            Evaluate(SpellTestData.CreateEncounter(), FireBolt).AttackRollMode);
        Assert.Null(
            Evaluate(SpellTestData.CreateEncounter(), SacredFlame).AttackRollMode);
    }

    /// The target's state contributes to a spell attack exactly as it does to
    /// a weapon attack.
    [Fact]
    public void Evaluate_AgainstADownedTarget_HasAdvantage()
    {
        Assert.Equal(
            D20RollMode.Advantage,
            Evaluate(SpellTestData.DownTarget(SpellTestData.CreateEncounter()), FireBolt)
                .AttackRollMode);
    }

    [Fact]
    public void Evaluate_BeyondTheSpellsReach_IsOutOfRange()
    {
        EncounterSpellPrerequisiteEvaluation result = Evaluate(
            SpellTestData.CreateEncounter(targetPosition: new GridPosition(14, 1)),
            SacredFlame);

        Assert.False(result.IsLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason.TargetOutOfRange,
            result.UnavailabilityReason);
    }

    /// Cure Wounds reaches an adjacent ally and no further.
    [Fact]
    public void Evaluate_ATouchSpell_ReachesOnlyAnAdjacentTarget()
    {
        Assert.True(
            Evaluate(
                SpellTestData.CreateEncounter(
                    allyPosition: new GridPosition(2, 1)),
                CureWounds,
                targetId: "combatant.ally")
                .IsLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason.TargetOutOfRange,
            Evaluate(
                SpellTestData.CreateEncounter(
                    allyPosition: new GridPosition(1, 4)),
                CureWounds,
                targetId: "combatant.ally")
                .UnavailabilityReason);
    }

    /// Healing goes to an ally and harm goes to an enemy. Which is which is
    /// read from what the spell does rather than declared twice.
    [Fact]
    public void Evaluate_RejectsCastingAtTheWrongKindOfTarget()
    {
        Assert.Equal(
            EncounterActionUnavailabilityReason.TargetNotHostile,
            Evaluate(SpellTestData.CreateEncounter(), FireBolt, targetId: "combatant.ally")
                .UnavailabilityReason);
        Assert.Equal(
            EncounterActionUnavailabilityReason.TargetNotHostile,
            Evaluate(
                SpellTestData.CreateEncounter(allyPosition: new GridPosition(2, 1)),
                CureWounds,
                targetId: "combatant.enemy")
                .UnavailabilityReason);
    }

    [Fact]
    public void Evaluate_ASpellTheCasterDoesNotKnow_IsUnavailable()
    {
        Assert.Equal(
            EncounterActionUnavailabilityReason.SpellUnavailable,
            Evaluate(SpellTestData.CreateEncounter(), "spell.wish").UnavailabilityReason);
    }

    [Fact]
    public void Evaluate_WithNoActionLeft_IsUnavailable()
    {
        EncounterState state = SpellTestData.CreateEncounter();
        EncounterParticipantState[] participants =
            state.Participants.ToArray();
        participants[0] = participants[0] with
        {
            TurnResources = participants[0].TurnResources with
            {
                HasActionAvailable = false
            }
        };

        Assert.Equal(
            EncounterActionUnavailabilityReason.ActionUnavailable,
            Evaluate(
                state with
                {
                    Participants = Array.AsReadOnly(participants)
                },
                FireBolt)
                .UnavailabilityReason);
    }

    [Fact]
    public void DiscoverSpellCasts_ReportsLegalityPerCandidate()
    {
        EncounterActionDiscoveryResult result =
            EncounterActionDiscoveryRules.DiscoverSpellCasts(
                SpellTestData.CreateEncounter(),
                [
                    new EncounterSpellCastDiscoveryCandidate
                    {
                        ActionOptionId = "cast.fire-bolt",
                        ActorCombatantId = "combatant.caster",
                        TargetCombatantId = "combatant.enemy",
                        SpellId = FireBolt
                    },
                    new EncounterSpellCastDiscoveryCandidate
                    {
                        ActionOptionId = "cast.wish",
                        ActorCombatantId = "combatant.caster",
                        TargetCombatantId = "combatant.enemy",
                        SpellId = "spell.wish"
                    }
                ]);

        Assert.True(result.Evaluations[0].IsCommonlyLegal);
        Assert.False(result.Evaluations[1].IsCommonlyLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason.SpellUnavailable,
            result.Evaluations[1].UnavailabilityReason);
    }

    [Fact]
    public void DiscoverSpellCasts_WithDuplicateOptionIds_Throws()
    {
        EncounterSpellCastDiscoveryCandidate candidate = new()
        {
            ActionOptionId = "cast.fire-bolt",
            ActorCombatantId = "combatant.caster",
            TargetCombatantId = "combatant.enemy",
            SpellId = FireBolt
        };

        Assert.Throws<ArgumentException>(() =>
            EncounterActionDiscoveryRules.DiscoverSpellCasts(
                SpellTestData.CreateEncounter(),
                [candidate, candidate]));
    }

    private static EncounterSpellPrerequisiteEvaluation Evaluate(
        EncounterState state,
        string spellId,
        string targetId = "combatant.enemy")
    {
        return EncounterSpellPrerequisiteRules.Evaluate(
            state,
            "combatant.caster",
            targetId,
            spellId);
    }

}

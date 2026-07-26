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
    private const string FireBolt = "spell.fire-bolt";

    private const string SacredFlame = "spell.sacred-flame";

    private const string CureWounds = "spell.cure-wounds";

    [Fact]
    public void Evaluate_ACantripInRange_IsLegal()
    {
        EncounterSpellPrerequisiteEvaluation result =
            Evaluate(CreateEncounter(), FireBolt);

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
            Evaluate(CreateEncounter(), FireBolt).AttackRollMode);
        Assert.Null(
            Evaluate(CreateEncounter(), SacredFlame).AttackRollMode);
    }

    /// The target's state contributes to a spell attack exactly as it does to
    /// a weapon attack.
    [Fact]
    public void Evaluate_AgainstADownedTarget_HasAdvantage()
    {
        Assert.Equal(
            D20RollMode.Advantage,
            Evaluate(DownTarget(CreateEncounter()), FireBolt)
                .AttackRollMode);
    }

    [Fact]
    public void Evaluate_BeyondTheSpellsReach_IsOutOfRange()
    {
        EncounterSpellPrerequisiteEvaluation result = Evaluate(
            CreateEncounter(targetPosition: new GridPosition(14, 1)),
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
                CreateEncounter(
                    allyPosition: new GridPosition(2, 1)),
                CureWounds,
                targetId: "combatant.ally")
                .IsLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason.TargetOutOfRange,
            Evaluate(
                CreateEncounter(
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
            Evaluate(CreateEncounter(), FireBolt, targetId: "combatant.ally")
                .UnavailabilityReason);
        Assert.Equal(
            EncounterActionUnavailabilityReason.TargetNotHostile,
            Evaluate(
                CreateEncounter(allyPosition: new GridPosition(2, 1)),
                CureWounds,
                targetId: "combatant.enemy")
                .UnavailabilityReason);
    }

    [Fact]
    public void Evaluate_ASpellTheCasterDoesNotKnow_IsUnavailable()
    {
        Assert.Equal(
            EncounterActionUnavailabilityReason.SpellUnavailable,
            Evaluate(CreateEncounter(), "spell.wish").UnavailabilityReason);
    }

    [Fact]
    public void Evaluate_WithNoActionLeft_IsUnavailable()
    {
        EncounterState state = CreateEncounter();
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
                CreateEncounter(),
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
                CreateEncounter(),
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

    private static EncounterState DownTarget(
        EncounterState state)
    {
        EncounterParticipantState[] participants =
            state.Participants.ToArray();
        int index = Array.FindIndex(
            participants,
            participant => participant.Combatant.CombatantId
                == "combatant.enemy");

        participants[index] = participants[index] with
        {
            Combatant = participants[index].Combatant with
            {
                Health = participants[index].Combatant.Health with
                {
                    HitPoints = participants[index].Combatant.Health.HitPoints
                        with
                    {
                        CurrentHitPoints = 0,
                        TemporaryHitPoints = 0
                    }
                }
            }
        };

        return state with
        {
            Participants = Array.AsReadOnly(participants)
        };
    }

    private static EncounterState CreateEncounter(
        GridPosition? targetPosition = null,
        GridPosition? allyPosition = null)
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
                "combatant.caster",
                "side.party",
                new GridPosition(1, 1),
                CreateSpells()),
            CreateParticipant(
                "combatant.ally",
                "side.party",
                allyPosition ?? new GridPosition(1, 2),
                Array.Empty<SpellAttack>()),
            CreateParticipant(
                "combatant.enemy",
                "side.enemies",
                targetPosition ?? new GridPosition(4, 1),
                Array.Empty<SpellAttack>())
        ];

        return EncounterRules.Start(
            "encounter.test",
            new EncounterBattlefieldState
            {
                BattlefieldId = "battlefield.test",
                Width = 16,
                Height = 6,
                BlockedPositions = Array.Empty<GridPosition>(),
                CoverPositions = Array.Empty<EncounterCoverPosition>(),
                DifficultTerrainPositions = Array.Empty<GridPosition>()
            },
            participants,
            participants
                .Select((participant, index) => new InitiativeOrderEntry
                {
                    CombatantId = participant.Combatant.CombatantId,
                    Position = index + 1,
                    Initiative = InitiativeRules.ResolveInitiative(
                        D20RollMode.Normal,
                        firstRoll: 20 - index,
                        secondRoll: null,
                        initiativeBonus: 0),
                    HasTiedInitiative = false
                })
                .ToArray());
    }

    private static EncounterParticipantSetup CreateParticipant(
        string combatantId,
        string sideId,
        GridPosition position,
        IReadOnlyList<SpellAttack> spells)
    {
        return new EncounterParticipantSetup
        {
            Combatant = CombatantRules.Create(
                combatantId,
                maximumHitPoints: 10,
                CombatantZeroHitPointPolicy.DeathSavingThrows),
            CombatProfile = new EncounterCombatProfile
            {
                ArmorClass = 12,
                SpellAttacks = spells,
                SavingThrowBonuses = Enum.GetValues<Ability>()
                    .Select(ability => new SavingThrowBonus
                    {
                        Ability = ability,
                        AbilityModifier = 0,
                        IsProficient = false,
                        ProficiencyBonus = 0,
                        TotalBonus = 0
                    })
                    .ToArray()
            },
            SideId = sideId,
            MovementSpeedFeet = 30,
            StartingPosition = position
        };
    }

    private static IReadOnlyList<SpellAttack> CreateSpells()
    {
        return
        [
            new SpellAttack
            {
                SpellId = FireBolt,
                SpellName = "Fire Bolt",
                Level = 0,
                CastingTime = SpellCastingTime.Action,
                RangeKind = SpellRangeKind.Ranged,
                RangeFeet = 120,
                MaximumTargets = 1,
                Resolution = SpellResolutionKind.SpellAttack,
                SaveOutcome = SpellSaveOutcome.Negates,
                AttackBonus = 5,
                SaveDc = 13,
                Effects =
                [
                    new SpellAttackEffect
                    {
                        Kind = SpellEffectKind.Damage,
                        Dice = new DamageDice { Count = 1, Die = DieType.D10 },
                        Instances = 1,
                        FlatBonus = 0,
                        DamageType = "damage.fire"
                    }
                ]
            },
            new SpellAttack
            {
                SpellId = SacredFlame,
                SpellName = "Sacred Flame",
                Level = 0,
                CastingTime = SpellCastingTime.Action,
                RangeKind = SpellRangeKind.Ranged,
                RangeFeet = 60,
                MaximumTargets = 1,
                Resolution = SpellResolutionKind.SavingThrow,
                SaveAbility = Ability.Dexterity,
                SaveOutcome = SpellSaveOutcome.Negates,
                AttackBonus = 5,
                SaveDc = 13,
                Effects =
                [
                    new SpellAttackEffect
                    {
                        Kind = SpellEffectKind.Damage,
                        Dice = new DamageDice { Count = 1, Die = DieType.D8 },
                        Instances = 1,
                        FlatBonus = 0,
                        DamageType = "damage.radiant"
                    }
                ]
            },
            new SpellAttack
            {
                SpellId = CureWounds,
                SpellName = "Cure Wounds",
                Level = 1,
                CastingTime = SpellCastingTime.Action,
                RangeKind = SpellRangeKind.Touch,
                MaximumTargets = 1,
                Resolution = SpellResolutionKind.Automatic,
                SaveOutcome = SpellSaveOutcome.Negates,
                AttackBonus = 5,
                SaveDc = 13,
                Effects =
                [
                    new SpellAttackEffect
                    {
                        Kind = SpellEffectKind.Healing,
                        Dice = new DamageDice { Count = 1, Die = DieType.D8 },
                        Instances = 1,
                        FlatBonus = 3
                    }
                ]
            }
        ];
    }
}

using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

/// A caster, an ally and an enemy, with the two committed cantrips and one
/// touch spell. Shared so the legality tests and the resolution tests argue
/// about the same battlefield.
internal static class SpellTestData
{
    internal const string FireBolt = "spell.fire-bolt";

    internal const string SacredFlame = "spell.sacred-flame";

    internal const string CureWounds = "spell.cure-wounds";

    internal const string HealingWord = "spell.healing-word";

    internal const string MagicMissile = "spell.magic-missile";

    internal const string FirstLevelSlot = "resource.spell-slot.1";

    internal static EncounterState DownTarget(
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

    internal static EncounterState CreateEncounter(
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

    internal static EncounterParticipantSetup CreateParticipant(
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
                Resources = spells.Count == 0
                    ? Array.Empty<CombatantResource>()
                    :
                    [
                        new CombatantResource
                        {
                            ResourceId = FirstLevelSlot,
                            Remaining = 2,
                            Maximum = 2
                        }
                    ],
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

    internal static IReadOnlyList<SpellAttack> CreateSpells()
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
                SlotResourceId = FirstLevelSlot,
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
            },
            new SpellAttack
            {
                SpellId = HealingWord,
                SpellName = "Healing Word",
                Level = 1,
                SlotResourceId = FirstLevelSlot,
                CastingTime = SpellCastingTime.BonusAction,
                RangeKind = SpellRangeKind.Ranged,
                RangeFeet = 60,
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
                        Dice = new DamageDice { Count = 1, Die = DieType.D4 },
                        Instances = 1,
                        FlatBonus = 3
                    }
                ]
            },
            new SpellAttack
            {
                SpellId = MagicMissile,
                SpellName = "Magic Missile",
                Level = 1,
                SlotResourceId = FirstLevelSlot,
                CastingTime = SpellCastingTime.Action,
                RangeKind = SpellRangeKind.Ranged,
                RangeFeet = 120,
                MaximumTargets = 3,
                Resolution = SpellResolutionKind.Automatic,
                SaveOutcome = SpellSaveOutcome.Negates,
                AttackBonus = 5,
                SaveDc = 13,
                Effects =
                [
                    new SpellAttackEffect
                    {
                        Kind = SpellEffectKind.Damage,
                        Dice = new DamageDice { Count = 1, Die = DieType.D4 },
                        Instances = 3,
                        FlatBonus = 1,
                        DamageType = "damage.force"
                    }
                ]
            }
        ];
    }
}

using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

/// Sneak Attack, which is where the roll-contribution seam stops being about
/// spells. Nothing here is a spell, nothing installs an effect, and the extra
/// damage is declared on the rogue's own combat profile — the same seam,
/// reached from the other side.
///
/// The rogue attacks with a dagger for 1d8 plus 3, against armour class 15
/// with an attack bonus of 5, so a natural 10 hits and a 9 misses.
public sealed class EncounterDamageContributionTests
{
    [Fact]
    public void Evaluate_AMiss_OwesNoContributionDice()
    {
        EncounterWeaponAttackEvaluation evaluation = Evaluate(
            CreateEncounter(allyAdjacentToTarget: true),
            firstRoll: 9);

        Assert.Equal(AttackRollOutcome.Miss, evaluation.AttackRoll.Outcome);
        Assert.Null(evaluation.RequiredDamageDice);
        Assert.True(evaluation.DamageContributions.IsEmpty);
    }

    [Fact]
    public void Evaluate_AHitWithTheOpening_AsksForTheSneakAttackDie()
    {
        EncounterWeaponAttackEvaluation evaluation = Evaluate(
            CreateEncounter(allyAdjacentToTarget: true),
            firstRoll: 10);

        Assert.Equal(
            [DieType.D6],
            evaluation.DamageContributions.RequiredDice);
        Assert.Equal(
            RollContributionTarget.DamageRoll,
            evaluation.DamageContributions.Target);
    }

    /// The trigger, as 5e states it. Advantage qualifies on its own; a crowded
    /// target qualifies while nothing is spoiling the shot; disadvantage
    /// spoils it either way.
    [Theory]
    [InlineData(D20RollMode.Normal, false, false)]
    [InlineData(D20RollMode.Normal, true, true)]
    [InlineData(D20RollMode.Advantage, false, true)]
    [InlineData(D20RollMode.Advantage, true, true)]
    [InlineData(D20RollMode.Disadvantage, false, false)]
    [InlineData(D20RollMode.Disadvantage, true, false)]
    public void Evaluate_AsksForTheDieOnlyWhenTheRogueHasAnOpening(
        D20RollMode attackRollMode,
        bool allyAdjacentToTarget,
        bool expectsDie)
    {
        EncounterWeaponAttackEvaluation evaluation = Evaluate(
            CreateEncounter(
                attackRollMode: attackRollMode,
                allyAdjacentToTarget: allyAdjacentToTarget),
            firstRoll: 10,
            secondRoll: attackRollMode == D20RollMode.Normal
                ? null
                : 10);

        Assert.Equal(
            expectsDie,
            evaluation.DamageContributions.RequiredDice.Count == 1);
    }

    /// A rogue swinging something it cannot be sneaky with gets nothing, however
    /// good the opening.
    [Fact]
    public void Evaluate_WithoutAFinesseOrRangedWeapon_AsksForNothing()
    {
        EncounterWeaponAttackEvaluation evaluation = Evaluate(
            CreateEncounter(
                finesse: false,
                allyAdjacentToTarget: true),
            firstRoll: 10);

        Assert.Equal(AttackRollOutcome.Hit, evaluation.AttackRoll.Outcome);
        Assert.NotNull(evaluation.RequiredDamageDice);
        Assert.True(evaluation.DamageContributions.IsEmpty);
    }

    /// The attacker standing over its own target is not "another enemy of the
    /// target" — otherwise every melee attack would qualify and the condition
    /// would mean nothing.
    [Fact]
    public void Evaluate_TheAttackerDoesNotCountAsItsOwnOpening()
    {
        EncounterWeaponAttackEvaluation evaluation = Evaluate(
            CreateEncounter(),
            firstRoll: 10);

        Assert.True(evaluation.DamageContributions.IsEmpty);
    }

    [Fact]
    public void Evaluate_AnUnconsciousAlly_IsNoOpening()
    {
        EncounterWeaponAttackEvaluation evaluation = Evaluate(
            DownTheAlly(CreateEncounter(allyAdjacentToTarget: true)),
            firstRoll: 10);

        Assert.True(evaluation.DamageContributions.IsEmpty);
    }

    [Fact]
    public void Resolve_AddsTheContributionToTheDamageDealt()
    {
        EncounterWeaponAttackResult sneaky = Attack(
            CreateEncounter(allyAdjacentToTarget: true),
            firstRoll: 10,
            damageRolls: [5],
            damageContributionRolls: [4]);
        EncounterWeaponAttackResult plain = Attack(
            CreateEncounter(),
            firstRoll: 10,
            damageRolls: [5],
            damageContributionRolls: []);

        Assert.Equal(8, plain.Attack.Damage.FinalDamage);
        Assert.Equal(12, sneaky.Attack.Damage.FinalDamage);
    }

    /// Every die of the attack's damage is rolled twice on a critical hit, and
    /// Sneak Attack's die is the attack's. The flat bonus is not doubled, the
    /// same way the weapon's is not.
    [Fact]
    public void Evaluate_ACriticalHit_DoublesTheContributionDice()
    {
        EncounterWeaponAttackEvaluation evaluation = Evaluate(
            CreateEncounter(allyAdjacentToTarget: true),
            firstRoll: 20);

        Assert.Equal(
            AttackRollOutcome.CriticalHit,
            evaluation.AttackRoll.Outcome);
        Assert.Equal(2, evaluation.RequiredDamageDice!.Count);
        Assert.Equal(
            [DieType.D6, DieType.D6],
            evaluation.DamageContributions.RequiredDice);
        Assert.Equal(
            18,
            Attack(
                CreateEncounter(allyAdjacentToTarget: true),
                firstRoll: 20,
                damageRolls: [5, 4],
                damageContributionRolls: [3, 3])
                .Attack.Damage.FinalDamage);
    }

    /// Resistance halves the blow, and the sneaky part of it is part of the
    /// blow — so the contribution lands inside the total rather than beside it.
    [Fact]
    public void Resolve_ResistanceHalvesTheContributionWithEverythingElse()
    {
        EncounterWeaponAttackResult result = Attack(
            CreateEncounter(
                allyAdjacentToTarget: true,
                targetResistsPiercing: true),
            firstRoll: 10,
            damageRolls: [5],
            damageContributionRolls: [4]);

        Assert.Equal(6, result.Attack.Damage.FinalDamage);
    }

    [Theory]
    [InlineData(new int[0])]
    [InlineData(new[] { 1, 1 })]
    public void Resolve_GivenTheWrongNumberOfContributionDice_Throws(
        int[] damageContributionRolls)
    {
        Assert.Throws<ArgumentException>(() =>
            Attack(
                CreateEncounter(allyAdjacentToTarget: true),
                firstRoll: 10,
                damageRolls: [5],
                damageContributionRolls: damageContributionRolls));
    }

    private static EncounterWeaponAttackEvaluation Evaluate(
        EncounterState state,
        int firstRoll,
        int? secondRoll = null)
    {
        return EncounterWeaponAttackRules.Evaluate(
            state,
            new EncounterWeaponAttackEvaluationCommand
            {
                ExpectedRevision = state.Revision,
                ActorCombatantId = "combatant.rogue",
                TargetCombatantId = "combatant.target",
                WeaponId = "weapon.test",
                FirstAttackRoll = firstRoll,
                SecondAttackRoll = secondRoll
            });
    }

    private static EncounterWeaponAttackResult Attack(
        EncounterState state,
        int firstRoll,
        IReadOnlyList<int> damageRolls,
        IReadOnlyList<int> damageContributionRolls)
    {
        return EncounterWeaponAttackRules.Resolve(
            state,
            new EncounterWeaponAttackCommand
            {
                ExpectedRevision = state.Revision,
                ActorCombatantId = "combatant.rogue",
                TargetCombatantId = "combatant.target",
                WeaponId = "weapon.test",
                FirstAttackRoll = firstRoll,
                DamageRolls = damageRolls,
                DamageContributionRolls = damageContributionRolls
            });
    }

    private static EncounterState DownTheAlly(
        EncounterState state)
    {
        EncounterParticipantState[] participants =
            state.Participants.ToArray();
        int index = Array.FindIndex(
            participants,
            participant => participant.Combatant.CombatantId
                == "combatant.ally");

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

    /// Sneak Attack as a rogue carries it: an extra d6 of damage, on an attack
    /// that found an opening, with a weapon it can be sneaky with. Declared on
    /// the combat profile because it is part of what the rogue is.
    private static RollContributionDefinition CreateSneakAttack()
    {
        return new RollContributionDefinition
        {
            Target = RollContributionTarget.DamageRoll,
            Dice = new DamageDice
            {
                Count = 1,
                Die = DieType.D6
            },
            Conditions =
            [
                RollContributionCondition.AdvantageOrAdjacentEnemy,
                RollContributionCondition.FinesseOrRangedWeapon
            ]
        };
    }

    private static EncounterState CreateEncounter(
        bool finesse = true,
        D20RollMode attackRollMode = D20RollMode.Normal,
        bool allyAdjacentToTarget = false,
        bool targetResistsPiercing = false)
    {
        WeaponAttack weapon = new()
        {
            WeaponId = "weapon.test",
            WeaponName = finesse
                ? "Test Dagger"
                : "Test Maul",
            Category = WeaponCategory.Simple,
            AttackKind = WeaponAttackKind.Melee,
            AttackAbility = Ability.Dexterity,
            AbilityModifier = 3,
            IsProficient = true,
            ProficiencyBonus = 2,
            AttackBonus = 5,
            HasDisadvantage = attackRollMode == D20RollMode.Disadvantage,
            DisadvantageReasons = Array.Empty<string>(),
            AttackRollMode = attackRollMode,
            Damage = new DamageDice
            {
                Count = 1,
                Die = DieType.D8
            },
            DamageType = "damage.piercing",
            DamageBonus = 3,
            Properties = finesse
                ? [RuleIds.WeaponProperties.Finesse]
                : Array.Empty<string>(),
            ReachFeet = 5
        };

        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
                "combatant.rogue",
                "side.party",
                new GridPosition(1, 1),
                weapon,
                [CreateSneakAttack()],
                resistsPiercing: false),
            CreateParticipant(
                "combatant.target",
                "side.enemy",
                new GridPosition(2, 1),
                weapon,
                Array.Empty<RollContributionDefinition>(),
                targetResistsPiercing),
            CreateParticipant(
                "combatant.ally",
                "side.party",
                allyAdjacentToTarget
                    ? new GridPosition(3, 1)
                    : new GridPosition(1, 9),
                weapon,
                Array.Empty<RollContributionDefinition>(),
                resistsPiercing: false)
        ];

        return EncounterRules.Start(
            "encounter.sneak-attack",
            new EncounterBattlefieldState
            {
                BattlefieldId = "battlefield.sneak-attack",
                Width = 12,
                Height = 12,
                BlockedPositions = Array.Empty<GridPosition>(),
                CoverPositions = Array.Empty<EncounterCoverPosition>(),
                DifficultTerrainPositions = Array.Empty<GridPosition>()
            },
            participants,
            [
                CreateInitiative("combatant.rogue", 1, 20),
                CreateInitiative("combatant.target", 2, 10),
                CreateInitiative("combatant.ally", 3, 5)
            ]);
    }

    private static EncounterParticipantSetup CreateParticipant(
        string combatantId,
        string sideId,
        GridPosition position,
        WeaponAttack weapon,
        IReadOnlyList<RollContributionDefinition> contributions,
        bool resistsPiercing)
    {
        return new EncounterParticipantSetup
        {
            Combatant = CombatantRules.Create(
                combatantId,
                maximumHitPoints: 30,
                CombatantZeroHitPointPolicy.DeathSavingThrows),
            CombatProfile = new EncounterCombatProfile
            {
                ArmorClass = 15,
                WeaponAttacks = [weapon],
                Contributions = contributions,
                DamageResponses = resistsPiercing
                    ?
                    [
                        new CharacterDamageResponse
                        {
                            DamageType = "damage.piercing",
                            ResponseType = DamageResponseType.Resistance
                        }
                    ]
                    : Array.Empty<CharacterDamageResponse>()
            },
            SideId = sideId,
            MovementSpeedFeet = 30,
            StartingPosition = position
        };
    }

    private static InitiativeOrderEntry CreateInitiative(
        string combatantId,
        int position,
        int total)
    {
        return new InitiativeOrderEntry
        {
            CombatantId = combatantId,
            Initiative = InitiativeRules.ResolveInitiative(
                D20RollMode.Normal,
                total,
                secondRoll: null,
                initiativeBonus: 0),
            Position = position,
            HasTiedInitiative = false
        };
    }
}

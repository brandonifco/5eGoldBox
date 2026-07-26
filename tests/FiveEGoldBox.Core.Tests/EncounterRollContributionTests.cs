using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

/// Contributions reaching the rolls they name. Three places ask for them — a
/// weapon attack, a spell attack, and a saving throw — and each has to say
/// what it needs before anything is rolled, because a contribution that adds
/// dice changes how many the caller owes.
public sealed class EncounterRollContributionTests
{
    private const string BlessEffectId = "effect.bless";

    [Fact]
    public void WeaponAttack_AgainstAnUnaffectedAttacker_AsksForNothing()
    {
        EncounterWeaponAttackPrerequisiteEvaluation prerequisites =
            EncounterWeaponAttackPrerequisiteRules.Evaluate(
                CreateWeaponEncounter(),
                "combatant.actor",
                "combatant.target",
                "weapon.test");

        Assert.True(prerequisites.AttackRollContributions.IsEmpty);
        Assert.Equal(
            RollContributionTarget.AttackRoll,
            prerequisites.AttackRollContributions.Target);
    }

    [Fact]
    public void WeaponAttack_ByABlessedAttacker_AsksForTheExtraDieFirst()
    {
        EncounterWeaponAttackPrerequisiteEvaluation prerequisites =
            EncounterWeaponAttackPrerequisiteRules.Evaluate(
                Bless(CreateWeaponEncounter(), "combatant.actor"),
                "combatant.actor",
                "combatant.target",
                "weapon.test");

        Assert.Equal(
            [DieType.D4],
            prerequisites.AttackRollContributions.RequiredDice);
    }

    /// The point of the whole step: a roll that misses unblessed lands when
    /// the contribution is added to it.
    [Fact]
    public void WeaponAttack_ByABlessedAttacker_TurnsAMissIntoAHit()
    {
        Assert.Equal(
            AttackRollOutcome.Miss,
            Attack(
                CreateWeaponEncounter(),
                firstRoll: 9,
                contributionRolls: [],
                damageRolls: [])
                .Attack.AttackRoll.Outcome);
        Assert.Equal(
            AttackRollOutcome.Hit,
            Attack(
                Bless(CreateWeaponEncounter(), "combatant.actor"),
                firstRoll: 9,
                contributionRolls: [1])
                .Attack.AttackRoll.Outcome);
    }

    /// A hit that a contribution bought still rolls damage, so the arity
    /// question reaches the damage dice as well as the attack.
    [Fact]
    public void WeaponAttack_ByABlessedAttacker_DamagesWhatItNowHits()
    {
        EncounterState blessed = Bless(
            CreateWeaponEncounter(),
            "combatant.actor");

        EncounterWeaponAttackEvaluation evaluation =
            EncounterWeaponAttackRules.Evaluate(
                blessed,
                new EncounterWeaponAttackEvaluationCommand
                {
                    ExpectedRevision = blessed.Revision,
                    ActorCombatantId = "combatant.actor",
                    TargetCombatantId = "combatant.target",
                    WeaponId = "weapon.test",
                    FirstAttackRoll = 9,
                    ContributionRolls = [1]
                });

        Assert.NotNull(evaluation.RequiredDamageDice);
        Assert.Equal(
            8,
            Attack(blessed, firstRoll: 9, contributionRolls: [1])
                .Attack.Damage.FinalDamage);
    }

    /// Contributions land in the attack bonus rather than beside it, because
    /// that is what adding a d4 to an attack roll means.
    [Fact]
    public void WeaponAttack_CountsTheContributionInTheAttackTotal()
    {
        EncounterWeaponAttackResult unaffected = Attack(
            CreateWeaponEncounter(),
            firstRoll: 10,
            contributionRolls: []);
        EncounterWeaponAttackResult blessed = Attack(
            Bless(CreateWeaponEncounter(), "combatant.actor"),
            firstRoll: 10,
            contributionRolls: [3]);

        Assert.Equal(
            unaffected.Attack.AttackRoll.Total + 3,
            blessed.Attack.AttackRoll.Total);
    }

    [Theory]
    [InlineData(new int[0])]
    [InlineData(new[] { 1, 1 })]
    public void WeaponAttack_GivenTheWrongNumberOfContributionDice_Throws(
        int[] contributionRolls)
    {
        EncounterState blessed = Bless(
            CreateWeaponEncounter(),
            "combatant.actor");

        Assert.Throws<ArgumentException>(() =>
            Attack(
                blessed,
                firstRoll: 9,
                contributionRolls: contributionRolls));
    }

    [Fact]
    public void SpellAttack_ByABlessedCaster_AsksForTheExtraDie()
    {
        EncounterSpellPrerequisiteEvaluation prerequisites =
            EncounterSpellPrerequisiteRules.Evaluate(
                Bless(SpellTestData.CreateEncounter(), "combatant.caster"),
                "combatant.caster",
                "combatant.enemy",
                SpellTestData.FireBolt);

        Assert.Equal(
            [DieType.D4],
            prerequisites.AttackRollContributions.RequiredDice);
        Assert.True(prerequisites.SavingThrowContributions.IsEmpty);
    }

    [Fact]
    public void SpellAttack_ByABlessedCaster_TurnsAMissIntoAHit()
    {
        Assert.False(
            CastFireBolt(
                SpellTestData.CreateEncounter(),
                attackRoll: 6,
                contributionRolls: [])
                .TookEffect);
        Assert.True(
            CastFireBolt(
                Bless(SpellTestData.CreateEncounter(), "combatant.caster"),
                attackRoll: 6,
                contributionRolls: [1])
                .TookEffect);
    }

    [Fact]
    public void SpellAttack_GivenTheWrongNumberOfContributionDice_Throws()
    {
        EncounterState blessed = Bless(
            SpellTestData.CreateEncounter(),
            "combatant.caster");

        Assert.Throws<ArgumentException>(() =>
            CastFireBolt(blessed, attackRoll: 6, contributionRolls: []));
    }

    /// A spell nobody rolls to hit with has nothing for an attack
    /// contribution to reach, and randomness spent there is randomness the
    /// next roll does not get.
    [Fact]
    public void SpellCast_ContributingToARollTheSpellNeverMakes_Throws()
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
                    AttackContributionRolls = [1],
                    EffectRolls = [1, 2, 3]
                }));
    }

    /// The other side of the seam: the creature saving is the one the effect
    /// is on, so the evaluation of a spell against it says what it will roll.
    [Fact]
    public void SavingThrow_AgainstABlessedTarget_AsksTheTargetForItsDie()
    {
        EncounterSpellPrerequisiteEvaluation prerequisites =
            EncounterSpellPrerequisiteRules.Evaluate(
                Bless(SpellTestData.CreateEncounter(), "combatant.enemy"),
                "combatant.caster",
                "combatant.enemy",
                SpellTestData.SacredFlame);

        Assert.Equal(
            [DieType.D4],
            prerequisites.SavingThrowContributions.RequiredDice);
        Assert.True(prerequisites.AttackRollContributions.IsEmpty);
    }

    [Fact]
    public void SavingThrow_ByABlessedTarget_TurnsAFailureIntoASave()
    {
        Assert.True(
            CastSacredFlame(
                SpellTestData.CreateEncounter(),
                savingThrow: 12,
                contributionRolls: [])
                .TookEffect);
        Assert.False(
            CastSacredFlame(
                Bless(SpellTestData.CreateEncounter(), "combatant.enemy"),
                savingThrow: 12,
                contributionRolls: [1])
                .TookEffect);
    }

    /// The general saving-throw entry reads the same contributions, so a save
    /// resolved through it is not silently unblessed.
    [Fact]
    public void EncounterSavingThrow_ByABlessedTarget_CountsTheContribution()
    {
        EncounterState blessed = Bless(
            SpellTestData.CreateEncounter(),
            "combatant.enemy");

        EncounterSavingThrowResult result = Save(
            blessed,
            firstRoll: 12,
            contributionRolls: [1]);

        Assert.Equal(1, result.CombinedSavingThrowBonus);
        Assert.Equal(
            D20TestOutcome.Success,
            result.SavingThrow!.Test.Outcome);
        Assert.Equal(
            D20TestOutcome.Failure,
            Save(
                SpellTestData.CreateEncounter(),
                firstRoll: 12,
                contributionRolls: null)
                .SavingThrow!.Test.Outcome);
    }

    [Fact]
    public void EncounterSavingThrow_GivenNoDiceForAContribution_Throws()
    {
        EncounterState blessed = Bless(
            SpellTestData.CreateEncounter(),
            "combatant.enemy");

        Assert.Throws<ArgumentException>(() =>
            Save(blessed, firstRoll: 12, contributionRolls: null));
    }

    /// Stamps the effect on directly rather than casting it, so that what is
    /// under test is the seam rather than the spell that installs it — and so
    /// that a creature Bless could never legally reach can still be blessed.
    private static EncounterState Bless(
        EncounterState state,
        string combatantId)
    {
        EncounterParticipantState[] participants =
            state.Participants.ToArray();
        int index = Array.FindIndex(
            participants,
            participant => participant.Combatant.CombatantId == combatantId);

        participants[index] = participants[index] with
        {
            ActiveEffects =
            [
                new ActiveEffect
                {
                    EffectId = BlessEffectId,
                    SourceCombatantId = combatantId,
                    RemainingRounds = 10,
                    RequiresConcentration = true,
                    Contributions =
                    [
                        new RollContributionDefinition
                        {
                            Target = RollContributionTarget.AttackRoll,
                            Dice = new DamageDice
                            {
                                Count = 1,
                                Die = DieType.D4
                            }
                        },
                        new RollContributionDefinition
                        {
                            Target = RollContributionTarget.SavingThrow,
                            Dice = new DamageDice
                            {
                                Count = 1,
                                Die = DieType.D4
                            }
                        }
                    ]
                }
            ]
        };

        return state with
        {
            Participants = Array.AsReadOnly(participants)
        };
    }

    private static EncounterWeaponAttackResult Attack(
        EncounterState state,
        int firstRoll,
        IReadOnlyList<int> contributionRolls,
        IReadOnlyList<int>? damageRolls = null)
    {
        return EncounterWeaponAttackRules.Resolve(
            state,
            new EncounterWeaponAttackCommand
            {
                ExpectedRevision = state.Revision,
                ActorCombatantId = "combatant.actor",
                TargetCombatantId = "combatant.target",
                WeaponId = "weapon.test",
                FirstAttackRoll = firstRoll,
                ContributionRolls = contributionRolls,
                DamageRolls = damageRolls ?? [5]
            });
    }

    private static EncounterSpellCastResult CastFireBolt(
        EncounterState state,
        int attackRoll,
        IReadOnlyList<int> contributionRolls)
    {
        return EncounterSpellRules.Resolve(
            state,
            new EncounterSpellCastCommand
            {
                ExpectedRevision = state.Revision,
                ActorCombatantId = "combatant.caster",
                TargetCombatantId = "combatant.enemy",
                SpellId = SpellTestData.FireBolt,
                FirstAttackRoll = attackRoll,
                AttackContributionRolls = contributionRolls,
                EffectRolls = [6]
            });
    }

    private static EncounterSpellCastResult CastSacredFlame(
        EncounterState state,
        int savingThrow,
        IReadOnlyList<int> contributionRolls)
    {
        return EncounterSpellRules.Resolve(
            state,
            new EncounterSpellCastCommand
            {
                ExpectedRevision = state.Revision,
                ActorCombatantId = "combatant.caster",
                TargetCombatantId = "combatant.enemy",
                SpellId = SpellTestData.SacredFlame,
                SavingThrowRoll = savingThrow,
                SavingThrowContributionRolls = contributionRolls,
                EffectRolls = [5]
            });
    }

    private static EncounterSavingThrowResult Save(
        EncounterState state,
        int firstRoll,
        IReadOnlyList<int>? contributionRolls)
    {
        return EncounterSavingThrowRules.Resolve(
            state,
            "combatant.enemy",
            Ability.Constitution,
            D20RollMode.Normal,
            firstRoll,
            secondRoll: null,
            difficultyClass: 13,
            EncounterSavingThrowCoverPolicy.Ignored,
            originPosition: null,
            contributionRolls);
    }

    /// A longbow that hits an armour class of 15 on a natural 10 and misses on
    /// a 9, so a single point of contribution is the difference.
    private static EncounterState CreateWeaponEncounter()
    {
        WeaponAttack weapon = new()
        {
            WeaponId = "weapon.test",
            WeaponName = "Test Longbow",
            Category = WeaponCategory.Martial,
            AttackKind = WeaponAttackKind.Ranged,
            AttackAbility = Ability.Dexterity,
            AbilityModifier = 3,
            IsProficient = true,
            ProficiencyBonus = 2,
            AttackBonus = 5,
            HasDisadvantage = false,
            DisadvantageReasons = Array.Empty<string>(),
            AttackRollMode = D20RollMode.Normal,
            Damage = new DamageDice
            {
                Count = 1,
                Die = DieType.D8
            },
            DamageType = "damage.piercing",
            DamageBonus = 3,
            Properties = Array.Empty<string>(),
            NormalRangeFeet = 80,
            LongRangeFeet = 320
        };

        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
                "combatant.actor",
                "side.party",
                new GridPosition(1, 1),
                weapon),
            CreateParticipant(
                "combatant.target",
                "side.enemy",
                new GridPosition(4, 1),
                weapon)
        ];

        return EncounterRules.Start(
            "encounter.contributions",
            new EncounterBattlefieldState
            {
                BattlefieldId = "battlefield.contributions",
                Width = 12,
                Height = 12,
                BlockedPositions = Array.Empty<GridPosition>(),
                CoverPositions = Array.Empty<EncounterCoverPosition>(),
                DifficultTerrainPositions = Array.Empty<GridPosition>()
            },
            participants,
            [
                CreateInitiative("combatant.actor", 1, 20),
                CreateInitiative("combatant.target", 2, 10)
            ]);
    }

    private static EncounterParticipantSetup CreateParticipant(
        string combatantId,
        string sideId,
        GridPosition position,
        WeaponAttack weapon)
    {
        return new EncounterParticipantSetup
        {
            Combatant = CombatantRules.Create(
                combatantId,
                maximumHitPoints: 20,
                CombatantZeroHitPointPolicy.DeathSavingThrows),
            CombatProfile = new EncounterCombatProfile
            {
                ArmorClass = 15,
                WeaponAttacks = [weapon]
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

using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

public sealed class EncounterWeaponAttackRulesTests
{
    [Fact]
    public void Resolve_WhenAttackHits_AppliesDamageAndSpendsAction()
    {
        EncounterState state = CreateEncounter();

        EncounterWeaponAttackResult result =
            EncounterWeaponAttackRules.Resolve(
                state,
                CreateCommand(
                    state,
                    firstAttackRoll: 12,
                    damageRolls: [4]));

        EncounterParticipantState actor =
            FindParticipant(
                result.State,
                "combatant.hero");

        EncounterParticipantState target =
            FindParticipant(
                result.State,
                "combatant.enemy");

        Assert.Equal(2, result.State.Revision);
        Assert.Equal(5, result.DistanceFeet);
        Assert.Equal(
            AttackRollOutcome.Hit,
            result.Attack.AttackRoll.Outcome);
        Assert.Equal(17, result.Attack.AttackRoll.Total);
        Assert.Equal(7, result.Attack.Damage.FinalDamage);

        Assert.NotNull(result.TargetDamage);
        Assert.Equal(
            13,
            target.Combatant.Health.HitPoints
                .CurrentHitPoints);

        Assert.False(
            actor.TurnResources.HasActionAvailable);
        Assert.True(
            actor.TurnResources.HasBonusActionAvailable);
        Assert.True(
            actor.TurnResources.HasReactionAvailable);

        Assert.Equal(1, state.Revision);
        Assert.True(
            state.Participants[0]
                .TurnResources.HasActionAvailable);
        Assert.Equal(
            20,
            state.Participants[1]
                .Combatant.Health.HitPoints
                .CurrentHitPoints);
    }

    [Fact]
    public void Resolve_WhenAttackMisses_SpendsActionWithoutDamage()
    {
        EncounterState state = CreateEncounter();

        EncounterWeaponAttackResult result =
            EncounterWeaponAttackRules.Resolve(
                state,
                CreateCommand(
                    state,
                    firstAttackRoll: 5,
                    damageRolls: []));

        EncounterParticipantState actor =
            FindParticipant(
                result.State,
                "combatant.hero");

        EncounterParticipantState target =
            FindParticipant(
                result.State,
                "combatant.enemy");

        Assert.Equal(
            AttackRollOutcome.Miss,
            result.Attack.AttackRoll.Outcome);
        Assert.Equal(0, result.Attack.Damage.FinalDamage);
        Assert.Null(result.TargetDamage);
        Assert.Equal(
            20,
            target.Combatant.Health.HitPoints
                .CurrentHitPoints);
        Assert.False(
            actor.TurnResources.HasActionAvailable);
        Assert.Equal(2, result.State.Revision);
    }

    [Fact]
    public void Resolve_WhenAttackIsCritical_DoublesDamageDice()
    {
        EncounterState state = CreateEncounter();

        EncounterWeaponAttackResult result =
            EncounterWeaponAttackRules.Resolve(
                state,
                CreateCommand(
                    state,
                    firstAttackRoll: 20,
                    damageRolls: [4, 5]));

        Assert.Equal(
            AttackRollOutcome.CriticalHit,
            result.Attack.AttackRoll.Outcome);

        Assert.NotNull(
            result.Attack.Damage.DamageDice);
        Assert.Equal(
            2,
            result.Attack.Damage.DamageDice.Count);
        Assert.Equal(
            DieType.D8,
            result.Attack.Damage.DamageDice.Die);
        Assert.Equal(
            12,
            result.Attack.Damage.FinalDamage);

        Assert.NotNull(result.TargetDamage);
        Assert.True(
            result.TargetDamage
                .HealthDamage.IsCriticalHit);
        Assert.Equal(
            8,
            result.TargetDamage.State.Health
                .HitPoints.CurrentHitPoints);
    }

    [Fact]
    public void Resolve_WhenTargetHasResistance_AppliesResistance()
    {
        EncounterState state = CreateEncounter(
            enemyDamageResponses:
            [
                new CharacterDamageResponse
                {
                    DamageType = "damage.slashing",
                    ResponseType =
                        DamageResponseType.Resistance
                }
            ]);

        EncounterWeaponAttackResult result =
            EncounterWeaponAttackRules.Resolve(
                state,
                CreateCommand(
                    state,
                    firstAttackRoll: 12,
                    damageRolls: [4]));

        Assert.Contains(
            DamageResponseType.Resistance,
            result.Attack.Damage.ResponseTypes);
        Assert.Equal(
            7,
            result.Attack.Damage.DamageRoll!.Total);
        Assert.Equal(
            3,
            result.Attack.Damage.FinalDamage);
        Assert.Equal(
            17,
            result.TargetDamage!.State.Health
                .HitPoints.CurrentHitPoints);
    }

    [Fact]
    public void Resolve_WhenTargetIsDiagonallyAdjacent_UsesFiveFootDistance()
    {
        EncounterState state = CreateEncounter(
            enemyPosition:
                new GridPosition(2, 2));

        EncounterWeaponAttackResult result =
            EncounterWeaponAttackRules.Resolve(
                state,
                CreateCommand(
                    state,
                    firstAttackRoll: 5,
                    damageRolls: []));

        Assert.Equal(5, result.DistanceFeet);
    }

    [Fact]
    public void Resolve_WithExtendedReachWeapon_AllowsTargetTenFeetAway()
    {
        WeaponAttack weapon = CreateWeapon(
            reachFeet: 10);

        EncounterState state = CreateEncounter(
            heroWeapon: weapon,
            enemyPosition:
                new GridPosition(3, 1));

        EncounterWeaponAttackResult result =
            EncounterWeaponAttackRules.Resolve(
                state,
                CreateCommand(
                    state,
                    firstAttackRoll: 5,
                    damageRolls: []));

        Assert.Equal(10, result.DistanceFeet);
        Assert.Equal(2, result.State.Revision);
    }

    [Fact]
    public void Resolve_WhenTargetIsBeyondReach_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter(
            enemyPosition:
                new GridPosition(3, 1));

        Assert.Throws<InvalidOperationException>(() =>
            EncounterWeaponAttackRules.Resolve(
                state,
                CreateCommand(
                    state,
                    firstAttackRoll: 12,
                    damageRolls: [4])));

        AssertStateUnchanged(state);
    }

    [Fact]
    public void Resolve_WithDisadvantage_UsesLowerAttackRoll()
    {
        WeaponAttack weapon = CreateWeapon(
            attackRollMode:
                D20RollMode.Disadvantage);

        EncounterState state = CreateEncounter(
            heroWeapon: weapon);

        EncounterWeaponAttackResult result =
            EncounterWeaponAttackRules.Resolve(
                state,
                CreateCommand(
                    state,
                    firstAttackRoll: 20,
                    secondAttackRoll: 2,
                    damageRolls: []));

        Assert.Equal(
            D20RollMode.Disadvantage,
            result.Attack.AttackRoll.RollMode);
        Assert.Equal(
            2,
            result.Attack.AttackRoll.NaturalRoll);
        Assert.Equal(
            AttackRollOutcome.Miss,
            result.Attack.AttackRoll.Outcome);
    }

    [Fact]
    public void Resolve_WithStaleRevision_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter();

        EncounterWeaponAttackCommand command =
            CreateCommand(
                state,
                firstAttackRoll: 12,
                damageRolls: [4]) with
            {
                ExpectedRevision =
                    state.Revision + 1
            };

        Assert.Throws<InvalidOperationException>(() =>
            EncounterWeaponAttackRules.Resolve(
                state,
                command));

        AssertStateUnchanged(state);
    }

    [Fact]
    public void Resolve_WhenRevisionCannotIncrement_ThrowsBeforeTransition()
    {
        EncounterState state =
            CreateEncounter() with
            {
                Revision = long.MaxValue
            };

        Assert.Throws<OverflowException>(() =>
            EncounterWeaponAttackRules.Resolve(
                state,
                CreateCommand(
                    state,
                    firstAttackRoll: 12,
                    damageRolls: [4])));

        AssertStateUnchanged(
            state,
            expectedRevision: long.MaxValue);
    }

    [Fact]
    public void Resolve_WhenEncounterIsCompleted_ThrowsBeforeTransition()
    {
        EncounterState state =
            EncounterRules.DeclareOutcome(
                CreateEncounter(),
                EncounterLifecycleState.Victory);

        Assert.Throws<InvalidOperationException>(() =>
            EncounterWeaponAttackRules.Resolve(
                state,
                CreateCommand(
                    state,
                    firstAttackRoll: 12,
                    damageRolls: [4])));

        Assert.Equal(
            EncounterLifecycleState.Victory,
            state.LifecycleState);
        AssertStateUnchanged(
            state,
            expectedRevision: 2);
    }

    [Fact]
    public void Resolve_WhenActorIsNotParticipant_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter();

        EncounterWeaponAttackCommand command =
            CreateCommand(
                state,
                firstAttackRoll: 12,
                damageRolls: [4]) with
            {
                ActorCombatantId =
                    "combatant.outsider"
            };

        Assert.Throws<ArgumentException>(() =>
            EncounterWeaponAttackRules.Resolve(
                state,
                command));

        AssertStateUnchanged(state);
    }

    [Fact]
    public void Resolve_WhenTargetIsNotParticipant_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter();

        EncounterWeaponAttackCommand command =
            CreateCommand(
                state,
                firstAttackRoll: 12,
                damageRolls: [4]) with
            {
                TargetCombatantId =
                    "combatant.outsider"
            };

        Assert.Throws<ArgumentException>(() =>
            EncounterWeaponAttackRules.Resolve(
                state,
                command));

        AssertStateUnchanged(state);
    }

    [Fact]
    public void Resolve_WhenActorIsNotActive_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter();

        EncounterWeaponAttackCommand command =
            CreateCommand(
                state,
                firstAttackRoll: 12,
                damageRolls: [4]) with
            {
                ActorCombatantId =
                    "combatant.enemy",
                TargetCombatantId =
                    "combatant.hero"
            };

        Assert.Throws<InvalidOperationException>(() =>
            EncounterWeaponAttackRules.Resolve(
                state,
                command));

        AssertStateUnchanged(state);
    }

    [Fact]
    public void Resolve_WhenActorIsUnconscious_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter();

        EncounterParticipantState actor =
            FindParticipant(
                state,
                "combatant.hero");

        CombatantState unconsciousActor =
            CombatantRules.ResolveDamage(
                actor.Combatant,
                damageAmount: 20,
                isCriticalHit: false)
            .State;

        state = ReplaceParticipant(
            state,
            actor with
            {
                Combatant = unconsciousActor
            });

        Assert.Equal(
            CombatantLifecycleState.Dying,
            unconsciousActor.LifecycleState);

        Assert.Throws<InvalidOperationException>(() =>
            EncounterWeaponAttackRules.Resolve(
                state,
                CreateCommand(
                    state,
                    firstAttackRoll: 12,
                    damageRolls: [4])));

        AssertStateUnchanged(state);
    }

    [Fact]
    public void Resolve_WhenActionIsAlreadySpent_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter();

        EncounterParticipantState actor =
            FindParticipant(
                state,
                "combatant.hero");

        state = ReplaceParticipant(
            state,
            actor with
            {
                TurnResources =
                    CombatTurnResourceRules
                        .SpendAction(
                            actor.TurnResources)
            });

        Assert.Throws<InvalidOperationException>(() =>
            EncounterWeaponAttackRules.Resolve(
                state,
                CreateCommand(
                    state,
                    firstAttackRoll: 12,
                    damageRolls: [4])));

        Assert.Equal(1, state.Revision);
        Assert.False(
            FindParticipant(
                state,
                "combatant.hero")
            .TurnResources.HasActionAvailable);
    }

    [Fact]
    public void Resolve_WhenActorTargetsSelf_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter();

        EncounterWeaponAttackCommand command =
            CreateCommand(
                state,
                firstAttackRoll: 12,
                damageRolls: [4]) with
            {
                TargetCombatantId =
                    "combatant.hero"
            };

        Assert.Throws<ArgumentException>(() =>
            EncounterWeaponAttackRules.Resolve(
                state,
                command));

        AssertStateUnchanged(state);
    }

    [Fact]
    public void Resolve_WhenTargetIsOnSameSide_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter(
            includeAlly: true);

        EncounterWeaponAttackCommand command =
            CreateCommand(
                state,
                firstAttackRoll: 12,
                damageRolls: [4]) with
            {
                TargetCombatantId =
                    "combatant.ally"
            };

        Assert.Throws<InvalidOperationException>(() =>
            EncounterWeaponAttackRules.Resolve(
                state,
                command));

        AssertStateUnchanged(state);
    }

    [Fact]
    public void Resolve_WhenTargetIsTerminal_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter();

        EncounterParticipantState target =
            FindParticipant(
                state,
                "combatant.enemy");

        CombatantState deadTarget =
            CombatantRules.ResolveDamage(
                target.Combatant,
                damageAmount: 40,
                isCriticalHit: false)
            .State;

        state = ReplaceParticipant(
            state,
            target with
            {
                Combatant = deadTarget
            });

        Assert.True(deadTarget.IsTerminal);

        Assert.Throws<InvalidOperationException>(() =>
            EncounterWeaponAttackRules.Resolve(
                state,
                CreateCommand(
                    state,
                    firstAttackRoll: 12,
                    damageRolls: [4])));

        EncounterParticipantState unchangedActor =
            FindParticipant(
                state,
                "combatant.hero");

        EncounterParticipantState unchangedTarget =
            FindParticipant(
                state,
                "combatant.enemy");

        Assert.Equal(1, state.Revision);
        Assert.True(
            unchangedActor.TurnResources
                .HasActionAvailable);
        Assert.Equal(
            deadTarget,
            unchangedTarget.Combatant);
        Assert.Equal(
            0,
            unchangedTarget.Combatant.Health
                .HitPoints.CurrentHitPoints);
        Assert.True(
            unchangedTarget.Combatant.IsTerminal);
    }

    [Fact]
    public void Resolve_WhenWeaponIsUnavailable_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter();

        EncounterWeaponAttackCommand command =
            CreateCommand(
                state,
                firstAttackRoll: 12,
                damageRolls: [4]) with
            {
                WeaponId = "weapon.missing"
            };

        Assert.Throws<ArgumentException>(() =>
            EncounterWeaponAttackRules.Resolve(
                state,
                command));

        AssertStateUnchanged(state);
    }

    [Fact]
    public void Resolve_WithRangedWeapon_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter(
            heroWeapon:
                CreateWeapon(
                    attackKind:
                        WeaponAttackKind.Ranged));

        Assert.Throws<InvalidOperationException>(() =>
            EncounterWeaponAttackRules.Resolve(
                state,
                CreateCommand(
                    state,
                    firstAttackRoll: 12,
                    damageRolls: [4])));

        AssertStateUnchanged(state);
    }

    [Fact]
    public void Resolve_WhenHitHasWrongDamageRollCount_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<ArgumentException>(() =>
            EncounterWeaponAttackRules.Resolve(
                state,
                CreateCommand(
                    state,
                    firstAttackRoll: 12,
                    damageRolls: [])));

        AssertStateUnchanged(state);
    }

    [Fact]
    public void Resolve_WhenMissIncludesDamageRolls_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<ArgumentException>(() =>
            EncounterWeaponAttackRules.Resolve(
                state,
                CreateCommand(
                    state,
                    firstAttackRoll: 5,
                    damageRolls: [4])));

        AssertStateUnchanged(state);
    }

    [Fact]
    public void Resolve_WithNullDamageRolls_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter();

        EncounterWeaponAttackCommand command =
            CreateCommand(
                state,
                firstAttackRoll: 12,
                damageRolls: [4]) with
            {
                DamageRolls = null!
            };

        Assert.Throws<ArgumentNullException>(() =>
            EncounterWeaponAttackRules.Resolve(
                state,
                command));

        AssertStateUnchanged(state);
    }

    private static EncounterState CreateEncounter(
        WeaponAttack? heroWeapon = null,
        GridPosition? enemyPosition = null,
        IReadOnlyList<CharacterDamageResponse>?
            enemyDamageResponses = null,
        bool includeAlly = false)
    {
        WeaponAttack defaultWeapon =
            CreateWeapon();

        List<EncounterParticipantSetup> participants =
        [
            CreateParticipant(
                combatantId: "combatant.hero",
                sideId: "side.party",
                position: new GridPosition(1, 1),
                weapon:
                    heroWeapon
                    ?? defaultWeapon),
            CreateParticipant(
                combatantId: "combatant.enemy",
                sideId: "side.enemies",
                position:
                    enemyPosition
                    ?? new GridPosition(2, 1),
                weapon: defaultWeapon,
                damageResponses:
                    enemyDamageResponses)
        ];

        if (includeAlly)
        {
            participants.Add(
                CreateParticipant(
                    combatantId: "combatant.ally",
                    sideId: "side.party",
                    position:
                        new GridPosition(1, 2),
                    weapon: defaultWeapon));
        }

        InitiativeOrderEntry[] initiativeOrder =
            participants
                .Select(
                    (participant, index) =>
                        CreateInitiativeEntry(
                            participant.Combatant
                                .CombatantId,
                            position: index + 1,
                            total: 20 - index))
                .ToArray();

        return EncounterRules.Start(
            encounterId: "encounter.test",
            new EncounterBattlefieldState
            {
                BattlefieldId =
                    "battlefield.test",
                Width = 12,
                Height = 12,
                BlockedPositions =
                    Array.Empty<GridPosition>(),
                DifficultTerrainPositions =
                    Array.Empty<GridPosition>()
            },
            participants,
            initiativeOrder);
    }

    private static EncounterParticipantSetup
        CreateParticipant(
            string combatantId,
            string sideId,
            GridPosition position,
            WeaponAttack weapon,
            IReadOnlyList<CharacterDamageResponse>?
                damageResponses = null)
    {
        return new EncounterParticipantSetup
        {
            Combatant = CombatantRules.Create(
                combatantId,
                maximumHitPoints: 20,
                CombatantZeroHitPointPolicy
                    .DeathSavingThrows),
            CombatProfile =
                new EncounterCombatProfile
                {
                    ArmorClass = 15,
                    WeaponAttacks =
                    [
                        weapon
                    ],
                    DamageResponses =
                        damageResponses
                        ?? Array.Empty<
                            CharacterDamageResponse>()
                },
            SideId = sideId,
            MovementSpeedFeet = 30,
            StartingPosition = position
        };
    }

    private static WeaponAttack CreateWeapon(
        WeaponAttackKind attackKind =
            WeaponAttackKind.Melee,
        D20RollMode attackRollMode =
            D20RollMode.Normal,
        int? reachFeet = null)
    {
        return new WeaponAttack
        {
            WeaponId = "weapon.longsword",
            WeaponName = "Longsword",
            Category = WeaponCategory.Martial,
            AttackKind = attackKind,
            AttackAbility = Ability.Strength,
            AbilityModifier = 3,
            IsProficient = true,
            ProficiencyBonus = 2,
            AttackBonus = 5,
            HasDisadvantage =
                attackRollMode
                == D20RollMode.Disadvantage,
            DisadvantageReasons =
                Array.Empty<string>(),
            AttackRollMode = attackRollMode,
            Damage = new DamageDice
            {
                Count = 1,
                Die = DieType.D8
            },
            VersatileDamage = null,
            DamageType = "damage.slashing",
            DamageBonus = 3,
            Properties = Array.Empty<string>(),
            ReachFeet = reachFeet,
            NormalRangeFeet = null,
            LongRangeFeet = null,
            AmmunitionItemId = null,
            AmmunitionQuantityAvailable = null
        };
    }

    private static EncounterWeaponAttackCommand
        CreateCommand(
            EncounterState state,
            int firstAttackRoll,
            IReadOnlyList<int> damageRolls,
            int? secondAttackRoll = null)
    {
        return new EncounterWeaponAttackCommand
        {
            ExpectedRevision = state.Revision,
            ActorCombatantId =
                "combatant.hero",
            TargetCombatantId =
                "combatant.enemy",
            WeaponId = "weapon.longsword",
            FirstAttackRoll = firstAttackRoll,
            SecondAttackRoll =
                secondAttackRoll,
            DamageRolls = damageRolls
        };
    }

    private static InitiativeOrderEntry
        CreateInitiativeEntry(
            string combatantId,
            int position,
            int total)
    {
        return new InitiativeOrderEntry
        {
            CombatantId = combatantId,
            Initiative =
                InitiativeRules.ResolveInitiative(
                    D20RollMode.Normal,
                    firstRoll: total,
                    secondRoll: null,
                    initiativeBonus: 0),
            Position = position,
            HasTiedInitiative = false
        };
    }

    private static EncounterParticipantState
        FindParticipant(
            EncounterState state,
            string combatantId)
    {
        return Assert.Single(
            state.Participants,
            participant =>
                participant.Combatant.CombatantId
                == combatantId);
    }

    private static EncounterState ReplaceParticipant(
        EncounterState state,
        EncounterParticipantState replacement)
    {
        EncounterParticipantState[] participants =
            state.Participants.ToArray();

        int index = Array.FindIndex(
            participants,
            participant =>
                participant.Combatant.CombatantId
                == replacement.Combatant.CombatantId);

        Assert.True(index >= 0);

        participants[index] = replacement;

        return state with
        {
            Participants =
                Array.AsReadOnly(participants)
        };
    }

    private static void AssertStateUnchanged(
        EncounterState state,
        long expectedRevision = 1)
    {
        Assert.Equal(
            expectedRevision,
            state.Revision);

        Assert.True(
            FindParticipant(
                state,
                "combatant.hero")
            .TurnResources.HasActionAvailable);

        Assert.Equal(
            20,
            FindParticipant(
                state,
                "combatant.enemy")
            .Combatant.Health.HitPoints
            .CurrentHitPoints);
    }
}

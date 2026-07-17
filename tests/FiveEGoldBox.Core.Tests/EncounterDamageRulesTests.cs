using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

public sealed class EncounterDamageRulesTests
{
    [Fact]
    public void Resolve_WhenConsciousCombatantTakesDamage_AppliesDamageWithoutSpendingResources()
    {
        EncounterState state = CreateEncounter(
            hero: CreateInjuredCombatant(
                "combatant.hero",
                damageAmount: 4));

        EncounterParticipantState originalHero =
            FindParticipant(
                state,
                "combatant.hero");

        EncounterDamageResult result =
            EncounterDamageRules.Resolve(
                state,
                CreateCommand(
                    state,
                    targetCombatantId:
                        "combatant.hero",
                    damageAmount: 3));

        EncounterParticipantState resolvedHero =
            FindParticipant(
                result.State,
                "combatant.hero");

        Assert.Equal(
            "combatant.hero",
            result.TargetCombatantId);
        Assert.Equal(
            CombatantLifecycleState.Conscious,
            result.PreviousLifecycleState);
        Assert.Equal(
            CombatantLifecycleState.Conscious,
            result.LifecycleState);
        Assert.False(
            result.ClearedPendingDeathSavingThrow);

        Assert.Equal(
            3,
            result.CombatantDamage
                .HealthDamage.HitPointDamage
                .DamageAmount);
        Assert.Equal(
            3,
            resolvedHero.Combatant.Health
                .HitPoints.CurrentHitPoints);

        Assert.Equal(2, result.State.Revision);
        Assert.Equal(
            originalHero.TurnResources,
            resolvedHero.TurnResources);

        Assert.Equal(1, state.Revision);
        Assert.Equal(
            6,
            originalHero.Combatant.Health
                .HitPoints.CurrentHitPoints);
        Assert.True(
            originalHero.TurnResources
                .HasActionAvailable);
    }

    [Fact]
    public void Resolve_WhenNonactiveCombatantReachesZero_BecomesDyingWithoutPendingSave()
    {
        EncounterState state = CreateEncounter(
            enemy: CreateInjuredCombatant(
                "combatant.enemy",
                damageAmount: 5));

        EncounterDamageResult result =
            EncounterDamageRules.Resolve(
                state,
                CreateCommand(
                    state,
                    targetCombatantId:
                        "combatant.enemy",
                    damageAmount: 5));

        EncounterParticipantState resolvedEnemy =
            FindParticipant(
                result.State,
                "combatant.enemy");

        Assert.Equal(
            CombatantLifecycleState.Conscious,
            result.PreviousLifecycleState);
        Assert.Equal(
            CombatantLifecycleState.Dying,
            result.LifecycleState);
        Assert.False(
            result.ClearedPendingDeathSavingThrow);
        Assert.Equal(
            0,
            resolvedEnemy.Combatant.Health
                .HitPoints.CurrentHitPoints);
        Assert.Null(
            result.State
                .PendingDeathSavingThrowCombatantId);
    }

    [Fact]
    public void Resolve_WhenActiveCombatantReachesZero_DoesNotCreateMidturnPendingSave()
    {
        EncounterState state = CreateEncounter();

        EncounterDamageResult result =
            EncounterDamageRules.Resolve(
                state,
                CreateCommand(
                    state,
                    targetCombatantId:
                        "combatant.hero",
                    damageAmount: 10));

        Assert.Equal(
            "combatant.hero",
            result.State.ActiveCombatantId);
        Assert.Equal(
            CombatantLifecycleState.Dying,
            result.LifecycleState);
        Assert.Equal(
            CombatantLifecycleState.Dying,
            FindParticipant(
                result.State,
                "combatant.hero")
            .Combatant.LifecycleState);
        Assert.Null(
            result.State
                .PendingDeathSavingThrowCombatantId);
    }

    [Fact]
    public void Resolve_WhenActiveDyingCombatantTakesDamage_PreservesPendingSaveAndAddsFailure()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"));

        EncounterDamageResult result =
            EncounterDamageRules.Resolve(
                state,
                CreateCommand(
                    state,
                    targetCombatantId:
                        "combatant.hero",
                    damageAmount: 1));

        EncounterParticipantState resolvedHero =
            FindParticipant(
                result.State,
                "combatant.hero");

        Assert.Equal(
            CombatantLifecycleState.Dying,
            result.PreviousLifecycleState);
        Assert.Equal(
            CombatantLifecycleState.Dying,
            result.LifecycleState);
        Assert.False(
            result.ClearedPendingDeathSavingThrow);
        Assert.Equal(
            1,
            resolvedHero.Combatant.Health
                .DeathSavingThrows.FailureCount);
        Assert.Equal(
            "combatant.hero",
            result.State
                .PendingDeathSavingThrowCombatantId);

        Assert.Equal(
            0,
            FindParticipant(
                state,
                "combatant.hero")
            .Combatant.Health
            .DeathSavingThrows.FailureCount);
    }

    [Fact]
    public void Resolve_WhenCriticalDamageKillsActiveDyingCombatant_ClearsPendingSave()
    {
        CombatantState dying =
            CreateDyingCombatant(
                "combatant.hero");

        dying = dying with
        {
            Health = dying.Health with
            {
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        FailureCount = 1
                    }
            }
        };

        EncounterState state =
            CreateEncounter(hero: dying);

        EncounterDamageCommand command =
            CreateCommand(
                state,
                targetCombatantId:
                    "combatant.hero",
                damageAmount: 1)
            with
            {
                IsCriticalHit = true
            };

        EncounterDamageResult result =
            EncounterDamageRules.Resolve(
                state,
                command);

        Assert.Equal(
            CombatantLifecycleState.Dying,
            result.PreviousLifecycleState);
        Assert.Equal(
            CombatantLifecycleState.Dead,
            result.LifecycleState);
        Assert.True(
            result.ClearedPendingDeathSavingThrow);
        Assert.True(
            result.CombatantDamage
                .HealthDamage.IsCriticalHit);
        Assert.Equal(
            3,
            FindParticipant(
                result.State,
                "combatant.hero")
            .Combatant.Health
            .DeathSavingThrows.FailureCount);
        Assert.Null(
            result.State
                .PendingDeathSavingThrowCombatantId);
    }

    [Fact]
    public void Resolve_WhenActiveStableCombatantTakesDamage_BecomesDyingWithoutCreatingPendingSave()
    {
        EncounterState state = CreateEncounter(
            hero: CreateStableCombatant(
                "combatant.hero"));

        Assert.Null(
            state.PendingDeathSavingThrowCombatantId);

        EncounterDamageResult result =
            EncounterDamageRules.Resolve(
                state,
                CreateCommand(
                    state,
                    targetCombatantId:
                        "combatant.hero",
                    damageAmount: 1));

        EncounterParticipantState resolvedHero =
            FindParticipant(
                result.State,
                "combatant.hero");

        Assert.Equal(
            CombatantLifecycleState.Stable,
            result.PreviousLifecycleState);
        Assert.Equal(
            CombatantLifecycleState.Dying,
            result.LifecycleState);
        Assert.False(
            result.ClearedPendingDeathSavingThrow);
        Assert.False(
            resolvedHero.Combatant.Health
                .DeathSavingThrows.IsStable);
        Assert.Equal(
            1,
            resolvedHero.Combatant.Health
                .DeathSavingThrows.FailureCount);
        Assert.Null(
            result.State
                .PendingDeathSavingThrowCombatantId);
    }

    [Fact]
    public void Resolve_WhenActiveDyingCombatantSuffersMassiveDamage_ClearsPendingSave()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"));

        EncounterDamageResult result =
            EncounterDamageRules.Resolve(
                state,
                CreateCommand(
                    state,
                    targetCombatantId:
                        "combatant.hero",
                    damageAmount: 10));

        Assert.Equal(
            CombatantLifecycleState.Dead,
            result.LifecycleState);
        Assert.True(
            result.ClearedPendingDeathSavingThrow);
        Assert.True(
            FindParticipant(
                result.State,
                "combatant.hero")
            .Combatant.Health.IsInstantlyDead);
        Assert.Null(
            result.State
                .PendingDeathSavingThrowCombatantId);
    }

    [Fact]
    public void Resolve_WhenConsciousCombatantSuffersMassiveDamage_BecomesDead()
    {
        EncounterState state = CreateEncounter();

        EncounterDamageResult result =
            EncounterDamageRules.Resolve(
                state,
                CreateCommand(
                    state,
                    targetCombatantId:
                        "combatant.enemy",
                    damageAmount: 20));

        Assert.Equal(
            CombatantLifecycleState.Conscious,
            result.PreviousLifecycleState);
        Assert.Equal(
            CombatantLifecycleState.Dead,
            result.LifecycleState);
        Assert.False(
            result.ClearedPendingDeathSavingThrow);
        Assert.True(
            FindParticipant(
                result.State,
                "combatant.enemy")
            .Combatant.Health.IsInstantlyDead);
        Assert.Null(
            result.State
                .PendingDeathSavingThrowCombatantId);
    }

    [Fact]
    public void Resolve_WhenDefeatedPolicyCombatantReachesZero_BecomesDefeated()
    {
        EncounterState state = CreateEncounter(
            enemy: CombatantRules.Create(
                combatantId:
                    "combatant.enemy",
                maximumHitPoints: 10,
                CombatantZeroHitPointPolicy
                    .Defeated));

        EncounterDamageResult result =
            EncounterDamageRules.Resolve(
                state,
                CreateCommand(
                    state,
                    targetCombatantId:
                        "combatant.enemy",
                    damageAmount: 10));

        EncounterParticipantState resolvedEnemy =
            FindParticipant(
                result.State,
                "combatant.enemy");

        Assert.Equal(
            CombatantLifecycleState.Defeated,
            result.LifecycleState);
        Assert.True(
            resolvedEnemy.Combatant.IsTerminal);
        Assert.Null(
            result.CombatantDamage
                .HealthDamage.ZeroHitPointDamage);
        Assert.Equal(
            0,
            resolvedEnemy.Combatant.Health
                .DeathSavingThrows.FailureCount);
    }

    [Fact]
    public void Resolve_WhenAnotherCombatantTakesDamage_PreservesActivePendingSave()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"));

        EncounterDamageResult result =
            EncounterDamageRules.Resolve(
                state,
                CreateCommand(
                    state,
                    targetCombatantId:
                        "combatant.enemy",
                    damageAmount: 2));

        Assert.False(
            result.ClearedPendingDeathSavingThrow);
        Assert.Equal(
            "combatant.hero",
            result.State
                .PendingDeathSavingThrowCombatantId);
        Assert.Equal(
            CombatantLifecycleState.Dying,
            FindParticipant(
                result.State,
                "combatant.hero")
            .Combatant.LifecycleState);
        Assert.Equal(
            8,
            FindParticipant(
                result.State,
                "combatant.enemy")
            .Combatant.Health.HitPoints
            .CurrentHitPoints);
    }

    [Fact]
    public void Resolve_WithZeroDamageOnActiveDyingCombatant_PreservesPendingSave()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"));

        EncounterDamageResult result =
            EncounterDamageRules.Resolve(
                state,
                CreateCommand(
                    state,
                    targetCombatantId:
                        "combatant.hero",
                    damageAmount: 0));

        Assert.Equal(2, result.State.Revision);
        Assert.Equal(
            CombatantLifecycleState.Dying,
            result.LifecycleState);
        Assert.False(
            result.ClearedPendingDeathSavingThrow);
        Assert.Equal(
            0,
            FindParticipant(
                result.State,
                "combatant.hero")
            .Combatant.Health
            .DeathSavingThrows.FailureCount);
        Assert.Equal(
            "combatant.hero",
            result.State
                .PendingDeathSavingThrowCombatantId);
    }

    [Fact]
    public void Resolve_WithCriticalHit_PropagatesCriticalHitFlag()
    {
        EncounterState state = CreateEncounter();

        EncounterDamageCommand command =
            CreateCommand(
                state,
                targetCombatantId:
                    "combatant.enemy",
                damageAmount: 2)
            with
            {
                IsCriticalHit = true
            };

        EncounterDamageResult result =
            EncounterDamageRules.Resolve(
                state,
                command);

        Assert.True(
            result.CombatantDamage
                .HealthDamage.IsCriticalHit);
        Assert.Equal(
            8,
            FindParticipant(
                result.State,
                "combatant.enemy")
            .Combatant.Health.HitPoints
            .CurrentHitPoints);
    }

    [Theory]
    [InlineData(CombatantLifecycleState.Dead)]
    [InlineData(CombatantLifecycleState.Defeated)]
    public void Resolve_WhenTargetIsTerminal_ThrowsWithoutTransition(
        CombatantLifecycleState lifecycleState)
    {
        EncounterState state = CreateEncounter();

        state = ReplaceParticipant(
            state,
            "combatant.enemy",
            participant => participant with
            {
                Combatant =
                    CreateTerminalCombatant(
                        "combatant.enemy",
                        lifecycleState)
            });

        Assert.Throws<InvalidOperationException>(() =>
            EncounterDamageRules.Resolve(
                state,
                CreateCommand(
                    state,
                    targetCombatantId:
                        "combatant.enemy",
                    damageAmount: 1)));

        Assert.Equal(1, state.Revision);
        Assert.Equal(
            lifecycleState,
            FindParticipant(
                state,
                "combatant.enemy")
            .Combatant.LifecycleState);
    }

    [Theory]
    [InlineData(EncounterLifecycleState.Victory)]
    [InlineData(EncounterLifecycleState.Defeat)]
    public void Resolve_WhenEncounterIsComplete_Throws(
        EncounterLifecycleState outcome)
    {
        EncounterState state =
            EncounterRules.DeclareOutcome(
                CreateEncounter(),
                outcome);

        Assert.Throws<InvalidOperationException>(() =>
            EncounterDamageRules.Resolve(
                state,
                CreateCommand(
                    state,
                    targetCombatantId:
                        "combatant.enemy",
                    damageAmount: 1)));
    }

    [Fact]
    public void Resolve_WithStaleRevision_ThrowsWithoutTransition()
    {
        EncounterState state = CreateEncounter();

        EncounterDamageCommand command =
            CreateCommand(
                state,
                targetCombatantId:
                    "combatant.enemy",
                damageAmount: 2)
            with
            {
                ExpectedRevision =
                    state.Revision + 1
            };

        Assert.Throws<InvalidOperationException>(() =>
            EncounterDamageRules.Resolve(
                state,
                command));

        Assert.Equal(1, state.Revision);
        Assert.Equal(
            10,
            FindParticipant(
                state,
                "combatant.enemy")
            .Combatant.Health.HitPoints
            .CurrentHitPoints);
    }

    [Fact]
    public void Resolve_WhenTargetIsNotParticipant_Throws()
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<ArgumentException>(() =>
            EncounterDamageRules.Resolve(
                state,
                CreateCommand(
                    state,
                    targetCombatantId:
                        "combatant.outsider",
                    damageAmount: 1)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Resolve_WithInvalidExpectedRevision_Throws(
        long expectedRevision)
    {
        EncounterState state = CreateEncounter();

        EncounterDamageCommand command = new()
        {
            ExpectedRevision = expectedRevision,
            TargetCombatantId =
                "combatant.enemy",
            DamageAmount = 1,
            IsCriticalHit = false
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterDamageRules.Resolve(
                state,
                command));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Resolve_WithBlankTargetCombatantId_Throws(
        string targetCombatantId)
    {
        EncounterState state = CreateEncounter();

        EncounterDamageCommand command = new()
        {
            ExpectedRevision = state.Revision,
            TargetCombatantId =
                targetCombatantId,
            DamageAmount = 1,
            IsCriticalHit = false
        };

        Assert.Throws<ArgumentException>(() =>
            EncounterDamageRules.Resolve(
                state,
                command));
    }

    [Fact]
    public void Resolve_WithNullTargetCombatantId_Throws()
    {
        EncounterState state = CreateEncounter();

        EncounterDamageCommand command = new()
        {
            ExpectedRevision = state.Revision,
            TargetCombatantId = null!,
            DamageAmount = 1,
            IsCriticalHit = false
        };

        Assert.Throws<ArgumentException>(() =>
            EncounterDamageRules.Resolve(
                state,
                command));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Resolve_WithNegativeDamageAmount_Throws(
        int damageAmount)
    {
        EncounterState state = CreateEncounter();

        EncounterDamageCommand command = new()
        {
            ExpectedRevision = state.Revision,
            TargetCombatantId =
                "combatant.enemy",
            DamageAmount = damageAmount,
            IsCriticalHit = false
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterDamageRules.Resolve(
                state,
                command));
    }

    [Fact]
    public void Resolve_WithNullState_Throws()
    {
        EncounterDamageCommand command = new()
        {
            ExpectedRevision = 1,
            TargetCombatantId =
                "combatant.enemy",
            DamageAmount = 1,
            IsCriticalHit = false
        };

        Assert.Throws<ArgumentNullException>(() =>
            EncounterDamageRules.Resolve(
                null!,
                command));
    }

    [Fact]
    public void Resolve_WithNullCommand_Throws()
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<ArgumentNullException>(() =>
            EncounterDamageRules.Resolve(
                state,
                null!));
    }

    [Fact]
    public void Resolve_WhenRevisionWouldOverflow_ThrowsWithoutTransition()
    {
        EncounterState state = CreateEncounter()
            with
        {
            Revision = long.MaxValue
        };

        Assert.Throws<OverflowException>(() =>
            EncounterDamageRules.Resolve(
                state,
                CreateCommand(
                    state,
                    targetCombatantId:
                        "combatant.enemy",
                    damageAmount: 1)));

        Assert.Equal(
            long.MaxValue,
            state.Revision);
        Assert.Equal(
            10,
            FindParticipant(
                state,
                "combatant.enemy")
            .Combatant.Health.HitPoints
            .CurrentHitPoints);
    }

    [Fact]
    public void Resolve_WithInvalidEncounterState_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter() with
        {
            PendingDeathSavingThrowCombatantId =
                "combatant.enemy"
        };

        Assert.Throws<ArgumentException>(() =>
            EncounterDamageRules.Resolve(
                state,
                CreateCommand(
                    state,
                    targetCombatantId:
                        "combatant.hero",
                    damageAmount: 1)));

        Assert.Equal(1, state.Revision);
    }

    private static EncounterDamageCommand
        CreateCommand(
            EncounterState state,
            string targetCombatantId,
            int damageAmount)
    {
        return new EncounterDamageCommand
        {
            ExpectedRevision = state.Revision,
            TargetCombatantId =
                targetCombatantId,
            DamageAmount = damageAmount,
            IsCriticalHit = false
        };
    }

    private static EncounterState CreateEncounter(
        CombatantState? hero = null,
        CombatantState? enemy = null)
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
                hero
                    ?? CombatantRules.Create(
                        combatantId:
                            "combatant.hero",
                        maximumHitPoints: 10,
                        CombatantZeroHitPointPolicy
                            .DeathSavingThrows),
                sideId: "side.party",
                movementSpeedFeet: 30,
                position: new GridPosition(1, 1)),
            CreateParticipant(
                enemy
                    ?? CombatantRules.Create(
                        combatantId:
                            "combatant.enemy",
                        maximumHitPoints: 10,
                        CombatantZeroHitPointPolicy
                            .DeathSavingThrows),
                sideId: "side.enemies",
                movementSpeedFeet: 25,
                position: new GridPosition(2, 1))
        ];

        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(
                combatantId: "combatant.hero",
                position: 1,
                total: 15),
            CreateInitiativeEntry(
                combatantId: "combatant.enemy",
                position: 2,
                total: 10)
        ];

        return EncounterRules.Start(
            encounterId: "encounter.test",
            CreateBattlefield(),
            participants,
            initiativeOrder);
    }

    private static EncounterParticipantSetup
        CreateParticipant(
            CombatantState combatant,
            string sideId,
            int movementSpeedFeet,
            GridPosition position)
    {
        return new EncounterParticipantSetup
        {
            Combatant = combatant,
            CombatProfile =
                new EncounterCombatProfile
                {
                    ArmorClass = 10
                },
            SideId = sideId,
            MovementSpeedFeet =
                movementSpeedFeet,
            StartingPosition = position
        };
    }

    private static EncounterBattlefieldState
        CreateBattlefield()
    {
        return new EncounterBattlefieldState
        {
            BattlefieldId =
                "battlefield.test",
            Width = 5,
            Height = 5,
            BlockedPositions =
                Array.Empty<GridPosition>(),
            DifficultTerrainPositions =
                Array.Empty<GridPosition>()
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
        return state.Participants.Single(
            participant => string.Equals(
                participant.Combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal));
    }

    private static EncounterState ReplaceParticipant(
        EncounterState state,
        string combatantId,
        Func<
            EncounterParticipantState,
            EncounterParticipantState> replacement)
    {
        EncounterParticipantState[] participants =
            state.Participants.ToArray();

        int participantIndex = Array.FindIndex(
            participants,
            participant => string.Equals(
                participant.Combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal));

        if (participantIndex < 0)
        {
            throw new InvalidOperationException(
                $"Combatant '{combatantId}' was not found.");
        }

        participants[participantIndex] =
            replacement(
                participants[participantIndex]);

        return state with
        {
            Participants =
                Array.AsReadOnly(participants)
        };
    }

    private static CombatantState
        CreateInjuredCombatant(
            string combatantId,
            int damageAmount)
    {
        return CombatantRules.ResolveDamage(
            CombatantRules.Create(
                combatantId,
                maximumHitPoints: 10,
                CombatantZeroHitPointPolicy
                    .DeathSavingThrows),
            damageAmount,
            isCriticalHit: false)
        .State;
    }

    private static CombatantState
        CreateDyingCombatant(
            string combatantId)
    {
        return CreateInjuredCombatant(
            combatantId,
            damageAmount: 10);
    }

    private static CombatantState
        CreateStableCombatant(
            string combatantId)
    {
        CombatantState dying =
            CreateDyingCombatant(
                combatantId);

        return dying with
        {
            Health = dying.Health with
            {
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        IsStable = true
                    }
            }
        };
    }

    private static CombatantState
        CreateTerminalCombatant(
            string combatantId,
            CombatantLifecycleState lifecycleState)
    {
        return lifecycleState switch
        {
            CombatantLifecycleState.Dead =>
                CombatantRules.ResolveDamage(
                    CombatantRules.Create(
                        combatantId,
                        maximumHitPoints: 10,
                        CombatantZeroHitPointPolicy
                            .DeathSavingThrows),
                    damageAmount: 20,
                    isCriticalHit: false)
                .State,

            CombatantLifecycleState.Defeated =>
                CombatantRules.ResolveDamage(
                    CombatantRules.Create(
                        combatantId,
                        maximumHitPoints: 10,
                        CombatantZeroHitPointPolicy
                            .Defeated),
                    damageAmount: 10,
                    isCriticalHit: false)
                .State,

            _ => throw new ArgumentOutOfRangeException(
                nameof(lifecycleState),
                lifecycleState,
                "Unsupported terminal lifecycle state.")
        };
    }
}

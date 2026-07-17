using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

public sealed class EncounterDeathSavingThrowRulesTests
{
    [Fact]
    public void Start_WhenActiveCombatantIsDying_MarksDeathSavingThrowPending()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"));

        Assert.Equal(
            "combatant.hero",
            state.ActiveCombatantId);
        Assert.Equal(
            CombatantLifecycleState.Dying,
            state.Participants[0]
                .Combatant.LifecycleState);
        Assert.Equal(
            "combatant.hero",
            state.PendingDeathSavingThrowCombatantId);
    }

    [Fact]
    public void Resolve_WithSuccessfulRoll_RecordsSuccessAndClearsPendingState()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"));

        EncounterDeathSavingThrowResult result =
            EncounterDeathSavingThrowRules.Resolve(
                state,
                CreateCommand(
                    state,
                    actorCombatantId: "combatant.hero",
                    firstRoll: 10));

        DeathSavingThrowResult deathSavingThrow =
            result.CombatantDeathSavingThrow
                .HealthDeathSavingThrow
                .DeathSavingThrow;

        Assert.Equal(
            "combatant.hero",
            result.ActorCombatantId);
        Assert.Equal(
            CombatantLifecycleState.Dying,
            result.PreviousLifecycleState);
        Assert.Equal(
            CombatantLifecycleState.Dying,
            result.LifecycleState);
        Assert.Equal(
            DeathSavingThrowOutcome.Success,
            deathSavingThrow.Outcome);
        Assert.Equal(
            1,
            deathSavingThrow.State.SuccessCount);
        Assert.Equal(
            0,
            deathSavingThrow.State.FailureCount);

        Assert.Equal(2, result.State.Revision);
        Assert.Equal(
            "combatant.hero",
            result.State.ActiveCombatantId);
        Assert.Null(
            result.State
                .PendingDeathSavingThrowCombatantId);
        Assert.Equal(
            1,
            result.State.Participants[0]
                .Combatant.Health
                .DeathSavingThrows.SuccessCount);

        Assert.Equal(1, state.Revision);
        Assert.Equal(
            "combatant.hero",
            state.PendingDeathSavingThrowCombatantId);
        Assert.Equal(
            0,
            state.Participants[0]
                .Combatant.Health
                .DeathSavingThrows.SuccessCount);
    }

    [Fact]
    public void Resolve_WithThirdSuccess_StabilizesCombatant()
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
                        SuccessCount = 2,
                        FailureCount = 1
                    }
            }
        };

        EncounterState state =
            CreateEncounter(hero: dying);

        EncounterDeathSavingThrowResult result =
            EncounterDeathSavingThrowRules.Resolve(
                state,
                CreateCommand(
                    state,
                    actorCombatantId: "combatant.hero",
                    firstRoll: 10));

        Assert.Equal(
            DeathSavingThrowOutcome.Stabilized,
            result.CombatantDeathSavingThrow
                .HealthDeathSavingThrow
                .DeathSavingThrow.Outcome);
        Assert.Equal(
            CombatantLifecycleState.Stable,
            result.LifecycleState);
        Assert.Equal(
            CombatantLifecycleState.Stable,
            result.State.Participants[0]
                .Combatant.LifecycleState);
        Assert.Null(
            result.State
                .PendingDeathSavingThrowCombatantId);
    }

    [Fact]
    public void Resolve_WithThirdFailure_KillsCombatant()
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
                        FailureCount = 2
                    }
            }
        };

        EncounterState state =
            CreateEncounter(hero: dying);

        EncounterDeathSavingThrowResult result =
            EncounterDeathSavingThrowRules.Resolve(
                state,
                CreateCommand(
                    state,
                    actorCombatantId: "combatant.hero",
                    firstRoll: 9));

        Assert.Equal(
            DeathSavingThrowOutcome.Dead,
            result.CombatantDeathSavingThrow
                .HealthDeathSavingThrow
                .DeathSavingThrow.Outcome);
        Assert.Equal(
            CombatantLifecycleState.Dead,
            result.LifecycleState);
        Assert.True(
            result.State.Participants[0]
                .Combatant.IsTerminal);
        Assert.Null(
            result.State
                .PendingDeathSavingThrowCombatantId);
    }

    [Fact]
    public void Resolve_WithNaturalTwenty_RegainsHitPointAndBecomesConscious()
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
                        SuccessCount = 2,
                        FailureCount = 2
                    }
            }
        };

        EncounterState state =
            CreateEncounter(hero: dying);

        EncounterDeathSavingThrowResult result =
            EncounterDeathSavingThrowRules.Resolve(
                state,
                CreateCommand(
                    state,
                    actorCombatantId: "combatant.hero",
                    firstRoll: 20));

        Assert.Equal(
            DeathSavingThrowOutcome.RegainedHitPoint,
            result.CombatantDeathSavingThrow
                .HealthDeathSavingThrow
                .DeathSavingThrow.Outcome);
        Assert.Equal(
            CombatantLifecycleState.Conscious,
            result.LifecycleState);
        Assert.Equal(
            1,
            result.State.Participants[0]
                .Combatant.Health.HitPoints
                .CurrentHitPoints);
        Assert.Equal(
            0,
            result.State.Participants[0]
                .Combatant.Health
                .DeathSavingThrows.SuccessCount);
        Assert.Equal(
            0,
            result.State.Participants[0]
                .Combatant.Health
                .DeathSavingThrows.FailureCount);
    }

    [Fact]
    public void Resolve_WithNaturalOne_AddsTwoFailures()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"));

        EncounterDeathSavingThrowCommand command =
            CreateCommand(
                state,
                actorCombatantId: "combatant.hero",
                firstRoll: 1);

        command = command with
        {
            SavingThrowBonus = 100
        };

        EncounterDeathSavingThrowResult result =
            EncounterDeathSavingThrowRules.Resolve(
                state,
                command);

        DeathSavingThrowResult deathSavingThrow =
            result.CombatantDeathSavingThrow
                .HealthDeathSavingThrow
                .DeathSavingThrow;

        Assert.Equal(
            DeathSavingThrowOutcome.Failure,
            deathSavingThrow.Outcome);
        Assert.Equal(1, deathSavingThrow.NaturalRoll);
        Assert.Equal(101, deathSavingThrow.Total);
        Assert.Equal(
            2,
            deathSavingThrow.State.FailureCount);
    }

    [Fact]
    public void Resolve_AfterSaveWasAlreadyResolved_ThrowsWithoutTransition()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"));

        EncounterDeathSavingThrowResult firstResult =
            EncounterDeathSavingThrowRules.Resolve(
                state,
                CreateCommand(
                    state,
                    actorCombatantId: "combatant.hero",
                    firstRoll: 10));

        EncounterState resolvedState =
            firstResult.State;

        Assert.Throws<InvalidOperationException>(() =>
            EncounterDeathSavingThrowRules.Resolve(
                resolvedState,
                CreateCommand(
                    resolvedState,
                    actorCombatantId:
                        "combatant.hero",
                    firstRoll: 10)));

        Assert.Equal(2, resolvedState.Revision);
        Assert.Equal(
            1,
            resolvedState.Participants[0]
                .Combatant.Health
                .DeathSavingThrows.SuccessCount);
        Assert.Null(
            resolvedState
                .PendingDeathSavingThrowCombatantId);
    }

    [Fact]
    public void TurnAdvancement_WithPendingDeathSavingThrow_ThrowsWithoutTransition()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"));

        EncounterTurnAdvancementCommand command =
            new()
            {
                ExpectedRevision = state.Revision,
                ActorCombatantId =
                    "combatant.hero"
            };

        Assert.Throws<InvalidOperationException>(() =>
            EncounterTurnAdvancementRules.Resolve(
                state,
                command));

        Assert.Equal(1, state.Revision);
        Assert.Equal(
            "combatant.hero",
            state.ActiveCombatantId);
        Assert.Equal(
            "combatant.hero",
            state.PendingDeathSavingThrowCombatantId);
    }

    [Fact]
    public void TurnAdvancement_AfterSaveWasResolved_AllowsTurnToEnd()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"));

        EncounterDeathSavingThrowResult saveResult =
            EncounterDeathSavingThrowRules.Resolve(
                state,
                CreateCommand(
                    state,
                    actorCombatantId: "combatant.hero",
                    firstRoll: 10));

        EncounterTurnAdvancementResult
            advancementResult =
                EncounterTurnAdvancementRules.Resolve(
                    saveResult.State,
                    new EncounterTurnAdvancementCommand
                    {
                        ExpectedRevision =
                            saveResult.State.Revision,
                        ActorCombatantId =
                            "combatant.hero"
                    });

        Assert.Equal(
            "combatant.enemy",
            advancementResult.ActiveCombatantId);
        Assert.Equal(3, advancementResult.State.Revision);
        Assert.Null(
            advancementResult.State
                .PendingDeathSavingThrowCombatantId);
    }

    [Fact]
    public void TurnAdvancement_WhenDyingCombatantBecomesActive_MarksSavePending()
    {
        EncounterState state = CreateEncounter(
            enemy: CreateDyingCombatant(
                "combatant.enemy"));

        Assert.Null(
            state.PendingDeathSavingThrowCombatantId);

        EncounterTurnAdvancementResult result =
            EncounterTurnAdvancementRules.Resolve(
                state,
                new EncounterTurnAdvancementCommand
                {
                    ExpectedRevision =
                        state.Revision,
                    ActorCombatantId =
                        "combatant.hero"
                });

        Assert.Equal(
            "combatant.enemy",
            result.ActiveCombatantId);
        Assert.Equal(
            "combatant.enemy",
            result.State
                .PendingDeathSavingThrowCombatantId);
    }

    [Fact]
    public void TurnAdvancement_WhenStableCombatantBecomesActive_DoesNotMarkSavePending()
    {
        EncounterState state = CreateEncounter(
            enemy: CreateStableCombatant(
                "combatant.enemy"));

        EncounterTurnAdvancementResult result =
            EncounterTurnAdvancementRules.Resolve(
                state,
                new EncounterTurnAdvancementCommand
                {
                    ExpectedRevision =
                        state.Revision,
                    ActorCombatantId =
                        "combatant.hero"
                });

        Assert.Equal(
            "combatant.enemy",
            result.ActiveCombatantId);
        Assert.Equal(
            CombatantLifecycleState.Stable,
            result.State.Participants[1]
                .Combatant.LifecycleState);
        Assert.Null(
            result.State
                .PendingDeathSavingThrowCombatantId);
    }

    [Fact]
    public void TurnAdvancement_WhenDyingCombatantTurnReturns_RearmsSavePending()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"));

        EncounterDeathSavingThrowResult saveResult =
            EncounterDeathSavingThrowRules.Resolve(
                state,
                CreateCommand(
                    state,
                    actorCombatantId: "combatant.hero",
                    firstRoll: 10));

        EncounterTurnAdvancementResult enemyTurn =
            EncounterTurnAdvancementRules.Resolve(
                saveResult.State,
                new EncounterTurnAdvancementCommand
                {
                    ExpectedRevision =
                        saveResult.State.Revision,
                    ActorCombatantId =
                        "combatant.hero"
                });

        EncounterTurnAdvancementResult heroTurn =
            EncounterTurnAdvancementRules.Resolve(
                enemyTurn.State,
                new EncounterTurnAdvancementCommand
                {
                    ExpectedRevision =
                        enemyTurn.State.Revision,
                    ActorCombatantId =
                        "combatant.enemy"
                });

        Assert.Equal(
            "combatant.hero",
            heroTurn.ActiveCombatantId);
        Assert.Equal(
            CombatantLifecycleState.Dying,
            heroTurn.State.Participants[0]
                .Combatant.LifecycleState);
        Assert.Equal(
            "combatant.hero",
            heroTurn.State
                .PendingDeathSavingThrowCombatantId);
    }

    [Fact]
    public void Resolve_WithStaleRevision_ThrowsWithoutTransition()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"));

        EncounterDeathSavingThrowCommand command =
            CreateCommand(
                state,
                actorCombatantId: "combatant.hero",
                firstRoll: 10)
            with
            {
                ExpectedRevision =
                    state.Revision + 1
            };

        Assert.Throws<InvalidOperationException>(() =>
            EncounterDeathSavingThrowRules.Resolve(
                state,
                command));

        Assert.Equal(1, state.Revision);
        Assert.Equal(
            0,
            state.Participants[0]
                .Combatant.Health
                .DeathSavingThrows.SuccessCount);
    }

    [Fact]
    public void Resolve_WhenActorIsNotParticipant_Throws()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"));

        Assert.Throws<ArgumentException>(() =>
            EncounterDeathSavingThrowRules.Resolve(
                state,
                CreateCommand(
                    state,
                    actorCombatantId:
                        "combatant.outsider",
                    firstRoll: 10)));
    }

    [Fact]
    public void Resolve_WhenActorIsNotActive_Throws()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"),
            enemy: CreateDyingCombatant(
                "combatant.enemy"));

        Assert.Throws<InvalidOperationException>(() =>
            EncounterDeathSavingThrowRules.Resolve(
                state,
                CreateCommand(
                    state,
                    actorCombatantId:
                        "combatant.enemy",
                    firstRoll: 10)));

        Assert.Equal(
            "combatant.hero",
            state.ActiveCombatantId);
    }

    [Theory]
    [InlineData(CombatantLifecycleState.Conscious)]
    [InlineData(CombatantLifecycleState.Stable)]
    public void Resolve_WhenActiveCombatantIsNotDying_Throws(
        CombatantLifecycleState lifecycleState)
    {
        CombatantState hero =
            CreateCombatantWithLifecycle(
                "combatant.hero",
                lifecycleState);

        EncounterState state =
            CreateEncounter(hero: hero);

        Assert.Throws<InvalidOperationException>(() =>
            EncounterDeathSavingThrowRules.Resolve(
                state,
                CreateCommand(
                    state,
                    actorCombatantId:
                        "combatant.hero",
                    firstRoll: 10)));
    }

    [Fact]
    public void Resolve_WhenNoSaveIsPending_Throws()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"))
            with
        {
            PendingDeathSavingThrowCombatantId =
                    null
        };

        Assert.Throws<InvalidOperationException>(() =>
            EncounterDeathSavingThrowRules.Resolve(
                state,
                CreateCommand(
                    state,
                    actorCombatantId:
                        "combatant.hero",
                    firstRoll: 10)));
    }

    [Theory]
    [InlineData(EncounterLifecycleState.Victory)]
    [InlineData(EncounterLifecycleState.Defeat)]
    public void Resolve_WhenEncounterIsComplete_Throws(
        EncounterLifecycleState lifecycleState)
    {
        EncounterState state =
            EncounterRules.DeclareOutcome(
                CreateEncounter(
                    hero: CreateDyingCombatant(
                        "combatant.hero")),
                lifecycleState);

        Assert.Throws<InvalidOperationException>(() =>
            EncounterDeathSavingThrowRules.Resolve(
                state,
                CreateCommand(
                    state,
                    actorCombatantId:
                        "combatant.hero",
                    firstRoll: 10)));
    }

    [Fact]
    public void Resolve_WithAdvantage_UsesHigherRoll()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"));

        EncounterDeathSavingThrowCommand command =
            CreateCommand(
                state,
                actorCombatantId: "combatant.hero",
                firstRoll: 4)
            with
            {
                RollMode = D20RollMode.Advantage,
                SecondRoll = 14
            };

        EncounterDeathSavingThrowResult result =
            EncounterDeathSavingThrowRules.Resolve(
                state,
                command);

        DeathSavingThrowResult deathSavingThrow =
            result.CombatantDeathSavingThrow
                .HealthDeathSavingThrow
                .DeathSavingThrow;

        Assert.Equal(14, deathSavingThrow.NaturalRoll);
        Assert.Equal(
            DeathSavingThrowOutcome.Success,
            deathSavingThrow.Outcome);
    }

    [Fact]
    public void Resolve_WithAdvantageAndMissingSecondRoll_ThrowsWithoutTransition()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"));

        EncounterDeathSavingThrowCommand command =
            CreateCommand(
                state,
                actorCombatantId: "combatant.hero",
                firstRoll: 10)
            with
            {
                RollMode = D20RollMode.Advantage,
                SecondRoll = null
            };

        Assert.Throws<ArgumentException>(() =>
            EncounterDeathSavingThrowRules.Resolve(
                state,
                command));

        Assert.Equal(1, state.Revision);
        Assert.Equal(
            "combatant.hero",
            state.PendingDeathSavingThrowCombatantId);
        Assert.Equal(
            0,
            state.Participants[0]
                .Combatant.Health
                .DeathSavingThrows.SuccessCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Resolve_WithInvalidExpectedRevision_Throws(
        long expectedRevision)
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"));

        EncounterDeathSavingThrowCommand command =
            CreateCommand(
                state,
                actorCombatantId: "combatant.hero",
                firstRoll: 10)
            with
            {
                ExpectedRevision = expectedRevision
            };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterDeathSavingThrowRules.Resolve(
                state,
                command));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Resolve_WithBlankActorCombatantId_Throws(
        string actorCombatantId)
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"));

        EncounterDeathSavingThrowCommand command =
            CreateCommand(
                state,
                actorCombatantId,
                firstRoll: 10);

        Assert.Throws<ArgumentException>(() =>
            EncounterDeathSavingThrowRules.Resolve(
                state,
                command));
    }

    [Fact]
    public void Resolve_WithNullActorCombatantId_Throws()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"));

        EncounterDeathSavingThrowCommand command =
            CreateCommand(
                state,
                actorCombatantId: "combatant.hero",
                firstRoll: 10)
            with
            {
                ActorCombatantId = null!
            };

        Assert.Throws<ArgumentException>(() =>
            EncounterDeathSavingThrowRules.Resolve(
                state,
                command));
    }

    [Fact]
    public void Resolve_WithUnsupportedRollMode_Throws()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"));

        EncounterDeathSavingThrowCommand command =
            CreateCommand(
                state,
                actorCombatantId: "combatant.hero",
                firstRoll: 10)
            with
            {
                RollMode = (D20RollMode)999
            };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterDeathSavingThrowRules.Resolve(
                state,
                command));
    }

    [Fact]
    public void Resolve_WhenRevisionWouldOverflow_ThrowsWithoutTransition()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"))
            with
        {
            Revision = long.MaxValue
        };

        Assert.Throws<OverflowException>(() =>
            EncounterDeathSavingThrowRules.Resolve(
                state,
                CreateCommand(
                    state,
                    actorCombatantId:
                        "combatant.hero",
                    firstRoll: 10)));

        Assert.Equal(long.MaxValue, state.Revision);
        Assert.Equal(
            "combatant.hero",
            state.PendingDeathSavingThrowCombatantId);
    }

    [Fact]
    public void Resolve_WithNullState_Throws()
    {
        EncounterDeathSavingThrowCommand command =
            new()
            {
                ExpectedRevision = 1,
                ActorCombatantId =
                    "combatant.hero",
                RollMode = D20RollMode.Normal,
                FirstRoll = 10,
                SecondRoll = null,
                SavingThrowBonus = 0
            };

        Assert.Throws<ArgumentNullException>(() =>
            EncounterDeathSavingThrowRules.Resolve(
                null!,
                command));
    }

    [Fact]
    public void Resolve_WithNullCommand_Throws()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"));

        Assert.Throws<ArgumentNullException>(() =>
            EncounterDeathSavingThrowRules.Resolve(
                state,
                null!));
    }

    [Fact]
    public void Resolve_WithBlankPendingCombatantId_RejectsInvalidState()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"))
            with
        {
            PendingDeathSavingThrowCombatantId =
                    " "
        };

        Assert.Throws<ArgumentException>(() =>
            EncounterDeathSavingThrowRules.Resolve(
                state,
                CreateCommand(
                    state,
                    actorCombatantId:
                        "combatant.hero",
                    firstRoll: 10)));
    }

    [Fact]
    public void Resolve_WithPendingSaveForNonActiveCombatant_RejectsInvalidState()
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"),
            enemy: CreateDyingCombatant(
                "combatant.enemy"))
            with
        {
            PendingDeathSavingThrowCombatantId =
                    "combatant.enemy"
        };

        Assert.Throws<ArgumentException>(() =>
            EncounterDeathSavingThrowRules.Resolve(
                state,
                CreateCommand(
                    state,
                    actorCombatantId:
                        "combatant.hero",
                    firstRoll: 10)));
    }

    [Fact]
    public void Resolve_WithPendingSaveForConsciousCombatant_RejectsInvalidState()
    {
        EncounterState state = CreateEncounter()
            with
        {
            PendingDeathSavingThrowCombatantId =
                    "combatant.hero"
        };

        Assert.Throws<ArgumentException>(() =>
            EncounterDeathSavingThrowRules.Resolve(
                state,
                CreateCommand(
                    state,
                    actorCombatantId:
                        "combatant.hero",
                    firstRoll: 10)));
    }

    private static EncounterState CreateEncounter(
        CombatantState? hero = null,
        CombatantState? enemy = null)
    {
        hero ??= CombatantRules.Create(
            combatantId: "combatant.hero",
            maximumHitPoints: 10,
            CombatantZeroHitPointPolicy
                .DeathSavingThrows);

        enemy ??= CombatantRules.Create(
            combatantId: "combatant.enemy",
            maximumHitPoints: 10,
            CombatantZeroHitPointPolicy
                .DeathSavingThrows);

        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
                hero,
                sideId: "side.party",
                movementSpeedFeet: 30,
                position: new GridPosition(1, 1)),
            CreateParticipant(
                enemy,
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
            CombatProfile = new EncounterCombatProfile
            {
                ArmorClass = 10
            },
            SideId = sideId,
            MovementSpeedFeet = movementSpeedFeet,
            StartingPosition = position
        };
    }

    private static EncounterBattlefieldState
        CreateBattlefield()
    {
        return new EncounterBattlefieldState
        {
            BattlefieldId = "battlefield.test",
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

    private static EncounterDeathSavingThrowCommand
        CreateCommand(
            EncounterState state,
            string actorCombatantId,
            int firstRoll)
    {
        return new EncounterDeathSavingThrowCommand
        {
            ExpectedRevision = state.Revision,
            ActorCombatantId = actorCombatantId,
            RollMode = D20RollMode.Normal,
            FirstRoll = firstRoll,
            SecondRoll = null,
            SavingThrowBonus = 0
        };
    }

    private static CombatantState
        CreateDyingCombatant(
            string combatantId)
    {
        return CombatantRules.ResolveDamage(
            CombatantRules.Create(
                combatantId,
                maximumHitPoints: 10,
                CombatantZeroHitPointPolicy
                    .DeathSavingThrows),
            damageAmount: 10,
            isCriticalHit: false)
        .State;
    }

    private static CombatantState
        CreateStableCombatant(
            string combatantId)
    {
        CombatantState dying =
            CreateDyingCombatant(combatantId);

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
        CreateCombatantWithLifecycle(
            string combatantId,
            CombatantLifecycleState lifecycleState)
    {
        return lifecycleState switch
        {
            CombatantLifecycleState.Conscious =>
                CombatantRules.Create(
                    combatantId,
                    maximumHitPoints: 10,
                    CombatantZeroHitPointPolicy
                        .DeathSavingThrows),

            CombatantLifecycleState.Dying =>
                CreateDyingCombatant(
                    combatantId),

            CombatantLifecycleState.Stable =>
                CreateStableCombatant(
                    combatantId),

            _ => throw new ArgumentOutOfRangeException(
                nameof(lifecycleState),
                lifecycleState,
                "Unsupported lifecycle state.")
        };
    }

    [Theory]
    [InlineData(EncounterLifecycleState.Victory)]
    [InlineData(EncounterLifecycleState.Defeat)]
    public void DeclareOutcome_WithPendingDeathSavingThrow_ClearsPendingState(
    EncounterLifecycleState outcome)
    {
        EncounterState state = CreateEncounter(
            hero: CreateDyingCombatant(
                "combatant.hero"));

        EncounterState result =
            EncounterRules.DeclareOutcome(
                state,
                outcome);

        Assert.Equal(outcome, result.LifecycleState);
        Assert.Equal(2, result.Revision);
        Assert.Null(
            result.PendingDeathSavingThrowCombatantId);

        Assert.Equal(
            "combatant.hero",
            state.PendingDeathSavingThrowCombatantId);
        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);
    }

    [Fact]
    public void Resolve_WithPendingSaveOnCompletedEncounter_RejectsInvalidState()
    {
        EncounterState state =
            EncounterRules.DeclareOutcome(
                CreateEncounter(
                    hero: CreateDyingCombatant(
                        "combatant.hero")),
                EncounterLifecycleState.Defeat)
            with
            {
                PendingDeathSavingThrowCombatantId =
                    "combatant.hero"
            };

        Assert.Throws<ArgumentException>(() =>
            EncounterDeathSavingThrowRules.Resolve(
                state,
                CreateCommand(
                    state,
                    actorCombatantId:
                        "combatant.hero",
                    firstRoll: 10)));
    }
}

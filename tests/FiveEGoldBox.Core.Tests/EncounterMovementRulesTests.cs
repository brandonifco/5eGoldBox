using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

public sealed class EncounterMovementRulesTests
{
    [Fact]
    public void Resolve_WithNormalPath_MovesActorAndSpendsMovement()
    {
        EncounterState state = CreateEncounter();

        EncounterMovementResult result =
            EncounterMovementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    [
                        new GridPosition(2, 1),
                        new GridPosition(3, 1)
                    ]));

        EncounterParticipantState actor =
            FindParticipant(
                result.State,
                "combatant.hero");

        Assert.Equal("combatant.hero", result.ActorCombatantId);
        Assert.Equal(
            new GridPosition(1, 1),
            result.StartingPosition);
        Assert.Equal(
            new GridPosition(3, 1),
            result.EndingPosition);
        Assert.Equal(10, result.MovementSpentFeet);
        Assert.Equal(2, result.Path.Count);
        Assert.Equal(2, result.State.Revision);

        Assert.Equal(
            new GridPosition(3, 1),
            actor.Position);
        Assert.Equal(
            10,
            actor.TurnResources.MovementSpentFeet);
        Assert.Equal(
            20,
            actor.TurnResources.MovementRemainingFeet);

        Assert.Equal(1, state.Revision);
        Assert.Equal(
            new GridPosition(1, 1),
            FindParticipant(
                state,
                "combatant.hero")
            .Position);
        Assert.Equal(
            0,
            FindParticipant(
                state,
                "combatant.hero")
            .TurnResources.MovementSpentFeet);
    }

    [Fact]
    public void Resolve_WithDiagonalPath_ChargesFiveFeetPerSquare()
    {
        EncounterState state = CreateEncounter();

        EncounterMovementResult result =
            EncounterMovementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    [
                        new GridPosition(2, 2),
                        new GridPosition(3, 3)
                    ]));

        Assert.Equal(10, result.MovementSpentFeet);
        Assert.Equal(
            new GridPosition(3, 3),
            result.EndingPosition);

        EncounterParticipantState actor =
            FindParticipant(
                result.State,
                "combatant.hero");

        Assert.Equal(
            10,
            actor.TurnResources.MovementSpentFeet);
        Assert.Equal(
            20,
            actor.TurnResources.MovementRemainingFeet);
    }

    [Fact]
    public void Resolve_WhenPathEntersDifficultTerrain_ChargesDoubleMovement()
    {
        EncounterState state = CreateEncounter(
            difficultTerrainPositions:
            [
                new GridPosition(2, 1)
            ]);

        EncounterMovementResult result =
            EncounterMovementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    [
                        new GridPosition(2, 1),
                        new GridPosition(3, 1)
                    ]));

        Assert.Equal(15, result.MovementSpentFeet);

        EncounterParticipantState actor =
            FindParticipant(
                result.State,
                "combatant.hero");

        Assert.Equal(
            15,
            actor.TurnResources.MovementSpentFeet);
        Assert.Equal(
            15,
            actor.TurnResources.MovementRemainingFeet);
    }

    [Fact]
    public void Resolve_WhenMovementWasPreviouslySpent_AccumulatesMovement()
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
                    CombatTurnResourceRules.SpendMovement(
                        actor.TurnResources,
                        movementFeet: 10)
            });

        EncounterMovementResult result =
            EncounterMovementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    [
                        new GridPosition(2, 1),
                        new GridPosition(3, 1)
                    ]));

        EncounterParticipantState resolvedActor =
            FindParticipant(
                result.State,
                "combatant.hero");

        Assert.Equal(10, result.MovementSpentFeet);
        Assert.Equal(
            20,
            resolvedActor.TurnResources.MovementSpentFeet);
        Assert.Equal(
            10,
            resolvedActor.TurnResources.MovementRemainingFeet);
    }

    [Fact]
    public void Resolve_ProtectsResultPathFromSourceMutation()
    {
        EncounterState state = CreateEncounter();

        GridPosition[] path =
        [
            new GridPosition(2, 1),
            new GridPosition(3, 1)
        ];

        EncounterMovementResult result =
            EncounterMovementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    path));

        path[0] = new GridPosition(9, 9);

        Assert.Equal(
            new GridPosition(2, 1),
            result.Path[0]);
        Assert.Equal(
            new GridPosition(3, 1),
            result.Path[1]);
    }

    [Fact]
    public void Resolve_WhenEncounterIsCompleted_ThrowsBeforeTransition()
    {
        EncounterState state =
            EncounterRules.Complete(
                CreateEncounter(),
                winningSideId: "side.party");

        Assert.Throws<InvalidOperationException>(() =>
            EncounterMovementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    [
                        new GridPosition(2, 1)
                    ])));

        AssertStateUnchanged(
            state,
            expectedRevision: 2);
    }

    [Fact]
    public void Resolve_WithStaleRevision_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter();

        EncounterMovementCommand command =
            CreateCommand(
                state,
                [
                    new GridPosition(2, 1)
                ]) with
            {
                ExpectedRevision =
                    state.Revision + 1
            };

        Assert.Throws<InvalidOperationException>(() =>
            EncounterMovementRules.Resolve(
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
            EncounterMovementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    [
                        new GridPosition(2, 1)
                    ])));

        AssertStateUnchanged(
            state,
            expectedRevision: long.MaxValue);
    }

    [Fact]
    public void Resolve_WhenActorIsNotParticipant_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter();

        EncounterMovementCommand command =
            CreateCommand(
                state,
                [
                    new GridPosition(2, 1)
                ]) with
            {
                ActorCombatantId =
                    "combatant.outsider"
            };

        Assert.Throws<ArgumentException>(() =>
            EncounterMovementRules.Resolve(
                state,
                command));

        AssertStateUnchanged(state);
    }

    [Fact]
    public void Resolve_WhenActorIsNotActive_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter();

        EncounterMovementCommand command =
            CreateCommand(
                state,
                [
                    new GridPosition(9, 10)
                ]) with
            {
                ActorCombatantId =
                    "combatant.enemy"
            };

        Assert.Throws<InvalidOperationException>(() =>
            EncounterMovementRules.Resolve(
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
                damageAmount: 10,
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
            EncounterMovementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    [
                        new GridPosition(2, 1)
                    ])));

        Assert.Equal(1, state.Revision);
        Assert.Equal(
            new GridPosition(1, 1),
            FindParticipant(
                state,
                "combatant.hero")
            .Position);
        Assert.Equal(
            CombatantLifecycleState.Dying,
            FindParticipant(
                state,
                "combatant.hero")
            .Combatant.LifecycleState);
    }

    [Fact]
    public void Resolve_WhenActorHasNoMovementRemaining_ThrowsBeforeTransition()
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
                    CombatTurnResourceRules.SpendMovement(
                        actor.TurnResources,
                        movementFeet: 30)
            });

        Assert.Throws<InvalidOperationException>(() =>
            EncounterMovementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    [
                        new GridPosition(2, 1)
                    ])));

        Assert.Equal(1, state.Revision);
        Assert.Equal(
            new GridPosition(1, 1),
            FindParticipant(
                state,
                "combatant.hero")
            .Position);
        Assert.Equal(
            30,
            FindParticipant(
                state,
                "combatant.hero")
            .TurnResources.MovementSpentFeet);
    }

    [Fact]
    public void Resolve_WhenPathExceedsRemainingMovement_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<InvalidOperationException>(() =>
            EncounterMovementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    [
                        new GridPosition(2, 1),
                        new GridPosition(3, 1),
                        new GridPosition(4, 1),
                        new GridPosition(5, 1),
                        new GridPosition(6, 1),
                        new GridPosition(7, 1),
                        new GridPosition(8, 1)
                    ])));

        AssertStateUnchanged(state);
    }

    [Fact]
    public void Resolve_WhenDifficultTerrainExceedsRemainingMovement_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter(
            difficultTerrainPositions:
            [
                new GridPosition(2, 1),
                new GridPosition(3, 1),
                new GridPosition(4, 1),
                new GridPosition(5, 1)
            ]);

        Assert.Throws<InvalidOperationException>(() =>
            EncounterMovementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    [
                        new GridPosition(2, 1),
                        new GridPosition(3, 1),
                        new GridPosition(4, 1),
                        new GridPosition(5, 1)
                    ])));

        AssertStateUnchanged(state);
    }

    [Fact]
    public void Resolve_WhenPathEntersBlockedPosition_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter(
            blockedPositions:
            [
                new GridPosition(2, 1)
            ]);

        Assert.Throws<InvalidOperationException>(() =>
            EncounterMovementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    [
                        new GridPosition(2, 1)
                    ])));

        AssertStateUnchanged(state);
    }

    [Fact]
    public void Resolve_WhenPathEntersOccupiedPosition_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter(
            enemyPosition:
                new GridPosition(2, 1));

        Assert.Throws<InvalidOperationException>(() =>
            EncounterMovementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    [
                        new GridPosition(2, 1)
                    ])));

        AssertStateUnchanged(state);
    }

    [Fact]
    public void Resolve_WhenPathLeavesBattlefield_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterMovementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    [
                        new GridPosition(0, 1),
                        new GridPosition(-1, 1)
                    ])));

        AssertStateUnchanged(state);
    }

    [Fact]
    public void Resolve_WhenPathContainsNonadjacentStep_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<ArgumentException>(() =>
            EncounterMovementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    [
                        new GridPosition(3, 1)
                    ])));

        AssertStateUnchanged(state);
    }

    [Fact]
    public void Resolve_WhenPathRepeatsCurrentPosition_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<ArgumentException>(() =>
            EncounterMovementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    [
                        new GridPosition(1, 1)
                    ])));

        AssertStateUnchanged(state);
    }

    [Fact]
    public void Resolve_WithEmptyPath_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<ArgumentException>(() =>
            EncounterMovementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    [])));

        AssertStateUnchanged(state);
    }

    [Fact]
    public void Resolve_WithNullPath_ThrowsBeforeTransition()
    {
        EncounterState state = CreateEncounter();

        EncounterMovementCommand command =
            CreateCommand(
                state,
                [
                    new GridPosition(2, 1)
                ]) with
            {
                Path = null!
            };

        Assert.Throws<ArgumentNullException>(() =>
            EncounterMovementRules.Resolve(
                state,
                command));

        AssertStateUnchanged(state);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Resolve_WithBlankActorCombatantId_ThrowsBeforeTransition(
        string actorCombatantId)
    {
        EncounterState state = CreateEncounter();

        EncounterMovementCommand command =
            CreateCommand(
                state,
                [
                    new GridPosition(2, 1)
                ]) with
            {
                ActorCombatantId =
                    actorCombatantId
            };

        Assert.Throws<ArgumentException>(() =>
            EncounterMovementRules.Resolve(
                state,
                command));

        AssertStateUnchanged(state);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Resolve_WithInvalidExpectedRevision_ThrowsBeforeTransition(
        long expectedRevision)
    {
        EncounterState state = CreateEncounter();

        EncounterMovementCommand command =
            CreateCommand(
                state,
                [
                    new GridPosition(2, 1)
                ]) with
            {
                ExpectedRevision =
                    expectedRevision
            };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterMovementRules.Resolve(
                state,
                command));

        AssertStateUnchanged(state);
    }

    [Fact]
    public void Resolve_WithNullState_Throws()
    {
        EncounterMovementCommand command =
            new()
            {
                ExpectedRevision = 1,
                ActorCombatantId =
                    "combatant.hero",
                Path =
                [
                    new GridPosition(2, 1)
                ]
            };

        Assert.Throws<ArgumentNullException>(() =>
            EncounterMovementRules.Resolve(
                null!,
                command));
    }

    [Fact]
    public void Resolve_WithNullCommand_Throws()
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<ArgumentNullException>(() =>
            EncounterMovementRules.Resolve(
                state,
                null!));

        AssertStateUnchanged(state);
    }

    private static EncounterState CreateEncounter(
        GridPosition? enemyPosition = null,
        IReadOnlyList<GridPosition>?
            blockedPositions = null,
        IReadOnlyList<GridPosition>?
            difficultTerrainPositions = null)
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
                combatantId: "combatant.hero",
                sideId: "side.party",
                position: new GridPosition(1, 1)),
            CreateParticipant(
                combatantId: "combatant.enemy",
                sideId: "side.enemies",
                position:
                    enemyPosition
                    ?? new GridPosition(10, 10))
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
            new EncounterBattlefieldState
            {
                BattlefieldId =
                    "battlefield.test",
                Width = 12,
                Height = 12,
                BlockedPositions =
                    blockedPositions
                    ?? Array.Empty<GridPosition>(),
                DifficultTerrainPositions =
                    difficultTerrainPositions
                    ?? Array.Empty<GridPosition>()
            },
            participants,
            initiativeOrder);
    }

    private static EncounterParticipantSetup
        CreateParticipant(
            string combatantId,
            string sideId,
            GridPosition position)
    {
        return new EncounterParticipantSetup
        {
            Combatant = CombatantRules.Create(
                combatantId,
                maximumHitPoints: 10,
                CombatantZeroHitPointPolicy
                    .DeathSavingThrows),
            CombatProfile =
                new EncounterCombatProfile
                {
                    ArmorClass = 10
                },
            SideId = sideId,
            MovementSpeedFeet = 30,
            StartingPosition = position
        };
    }

    private static EncounterMovementCommand
        CreateCommand(
            EncounterState state,
            IReadOnlyList<GridPosition> path)
    {
        return new EncounterMovementCommand
        {
            ExpectedRevision = state.Revision,
            ActorCombatantId =
                "combatant.hero",
            Path = path
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
        EncounterParticipantState actor =
            FindParticipant(
                state,
                "combatant.hero");

        Assert.Equal(
            expectedRevision,
            state.Revision);
        Assert.Equal(
            new GridPosition(1, 1),
            actor.Position);
        Assert.Equal(
            0,
            actor.TurnResources.MovementSpentFeet);
        Assert.Equal(
            30,
            actor.TurnResources.MovementRemainingFeet);
    }
}

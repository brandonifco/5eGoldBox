using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

public sealed class EncounterRulesTests
{
    [Fact]
    public void Start_WithValidParticipants_CreatesActiveEncounter()
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
            combatantId: "combatant.hero",
            sideId: "side.party",
            movementSpeedFeet: 30),
        CreateParticipant(
            combatantId: "combatant.enemy",
            sideId: "side.enemies",
            movementSpeedFeet: 25)
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

        EncounterState state = EncounterRules.Start(
            encounterId: "encounter.test",
            participants,
            initiativeOrder);

        Assert.Equal("encounter.test", state.EncounterId);
        Assert.Equal(1, state.Revision);
        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);
        Assert.Equal(2, state.Participants.Count);
        Assert.Equal(2, state.InitiativeOrder.Count);
        Assert.Equal(1, state.TurnState.RoundNumber);
        Assert.Equal(1, state.TurnState.ActivePosition);
        Assert.Equal(
            "combatant.hero",
            state.ActiveCombatantId);

        Assert.True(
            state.Participants[0]
                .TurnResources.HasActionAvailable);
        Assert.True(
            state.Participants[0]
                .TurnResources.HasBonusActionAvailable);
        Assert.True(
            state.Participants[0]
                .TurnResources.HasReactionAvailable);
        Assert.Equal(
            30,
            state.Participants[0]
                .TurnResources.MovementSpeedFeet);
        Assert.Equal(
            0,
            state.Participants[0]
                .TurnResources.MovementSpentFeet);
        Assert.Equal(
            30,
            state.Participants[0]
                .TurnResources.MovementRemainingFeet);

        Assert.Equal(
            25,
            state.Participants[1]
                .TurnResources.MovementSpeedFeet);
        Assert.Equal(
            25,
            state.Participants[1]
                .TurnResources.MovementRemainingFeet);
    }

    [Fact]
    public void Start_WithUnorderedInitiativeEntries_OrdersByPosition()
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
                combatantId: "combatant.hero",
                sideId: "side.party"),
            CreateParticipant(
                combatantId: "combatant.enemy",
                sideId: "side.enemies")
        ];

        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(
                combatantId: "combatant.enemy",
                position: 2,
                total: 10),
            CreateInitiativeEntry(
                combatantId: "combatant.hero",
                position: 1,
                total: 15)
        ];

        EncounterState state = EncounterRules.Start(
            encounterId: "encounter.test",
            participants,
            initiativeOrder);

        Assert.Equal(
            "combatant.hero",
            state.InitiativeOrder[0].CombatantId);
        Assert.Equal(
            "combatant.enemy",
            state.InitiativeOrder[1].CombatantId);
        Assert.Equal(
            "combatant.hero",
            state.ActiveCombatantId);
    }

    [Fact]
    public void Start_ProtectsParticipantsFromSourceCollectionMutation()
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
                combatantId: "combatant.hero",
                sideId: "side.party"),
            CreateParticipant(
                combatantId: "combatant.enemy",
                sideId: "side.enemies")
        ];

        EncounterState state = EncounterRules.Start(
            encounterId: "encounter.test",
            participants,
            CreateInitiativeOrder());

        participants[0] = CreateParticipant(
            combatantId: "combatant.replacement",
            sideId: "side.party");

        Assert.Equal(
            "combatant.hero",
            state.Participants[0].Combatant.CombatantId);
    }

    [Fact]
    public void Start_ProtectsInitiativeOrderFromSourceCollectionMutation()
    {
        InitiativeOrderEntry[] initiativeOrder =
            CreateInitiativeOrder();

        EncounterState state = EncounterRules.Start(
            encounterId: "encounter.test",
            CreateParticipants(),
            initiativeOrder);

        initiativeOrder[0] = CreateInitiativeEntry(
            combatantId: "combatant.enemy",
            position: 1,
            total: 10);

        Assert.Equal(
            "combatant.hero",
            state.InitiativeOrder[0].CombatantId);
        Assert.Equal(
            "combatant.hero",
            state.ActiveCombatantId);
    }
    private static EncounterParticipantSetup[]
        CreateParticipants()
    {
        return
        [
            CreateParticipant(
            combatantId: "combatant.hero",
            sideId: "side.party"),
        CreateParticipant(
            combatantId: "combatant.enemy",
            sideId: "side.enemies")
        ];
    }

    private static EncounterParticipantSetup
        CreateParticipant(
            string combatantId,
            string sideId,
            int movementSpeedFeet = 30)
    {
        return new EncounterParticipantSetup
        {
            Combatant = CombatantRules.Create(
                combatantId,
                maximumHitPoints: 10,
                CombatantZeroHitPointPolicy
                    .DeathSavingThrows),
            SideId = sideId,
            MovementSpeedFeet = movementSpeedFeet
        };
    }

    private static InitiativeOrderEntry[]
        CreateInitiativeOrder()
    {
        return
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
            Initiative = InitiativeRules.ResolveInitiative(
                D20RollMode.Normal,
                firstRoll: total,
                secondRoll: null,
                initiativeBonus: 0),
            Position = position,
            HasTiedInitiative = false
        };
    }
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Start_WithBlankEncounterId_Throws(
    string encounterId)
    {
        Assert.Throws<ArgumentException>(() =>
            EncounterRules.Start(
                encounterId,
                CreateParticipants(),
                CreateInitiativeOrder()));
    }

    [Fact]
    public void Start_WithNoParticipants_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                participants:
                    Array.Empty<EncounterParticipantSetup>(),
                initiativeOrder:
                    Array.Empty<InitiativeOrderEntry>()));
    }

    [Fact]
    public void Start_WithOnlyOneSide_Throws()
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
            combatantId: "combatant.hero.one",
            sideId: "side.party"),
        CreateParticipant(
            combatantId: "combatant.hero.two",
            sideId: "side.party")
        ];

        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(
            combatantId: "combatant.hero.one",
            position: 1,
            total: 15),
        CreateInitiativeEntry(
            combatantId: "combatant.hero.two",
            position: 2,
            total: 10)
        ];

        Assert.Throws<ArgumentException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                participants,
                initiativeOrder));
    }

    [Fact]
    public void Start_WithDuplicateCombatantIds_Throws()
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
            combatantId: "combatant.duplicate",
            sideId: "side.party"),
        CreateParticipant(
            combatantId: "combatant.duplicate",
            sideId: "side.enemies")
        ];

        Assert.Throws<ArgumentException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                participants,
                CreateInitiativeOrder()));
    }

    [Fact]
    public void Start_WithBlankSideId_Throws()
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
            combatantId: "combatant.hero",
            sideId: "side.party"),
        CreateParticipant(
            combatantId: "combatant.enemy",
            sideId: " ")
        ];

        Assert.Throws<ArgumentException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                participants,
                CreateInitiativeOrder()));
    }

    [Fact]
    public void Start_WithTerminalCombatant_Throws()
    {
        CombatantState defeatedCombatant =
            CombatantRules.ResolveDamage(
                CombatantRules.Create(
                    combatantId: "combatant.enemy",
                    maximumHitPoints: 10,
                    CombatantZeroHitPointPolicy.Defeated),
                damageAmount: 10,
                isCriticalHit: false)
            .State;

        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
            combatantId: "combatant.hero",
            sideId: "side.party"),
        new EncounterParticipantSetup
        {
            Combatant = defeatedCombatant,
            SideId = "side.enemies",
            MovementSpeedFeet = 30
        }
        ];

        Assert.Throws<ArgumentException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                participants,
                CreateInitiativeOrder()));
    }
    [Fact]
    public void Start_WhenInitiativeCountDoesNotMatchParticipants_Throws()
    {
        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(
            combatantId: "combatant.hero",
            position: 1,
            total: 15)
        ];

        Assert.Throws<ArgumentException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                CreateParticipants(),
                initiativeOrder));
    }

    [Fact]
    public void Start_WithDuplicateInitiativeCombatantIds_Throws()
    {
        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(
            combatantId: "combatant.hero",
            position: 1,
            total: 15),
        CreateInitiativeEntry(
            combatantId: "combatant.hero",
            position: 2,
            total: 10)
        ];

        Assert.Throws<ArgumentException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                CreateParticipants(),
                initiativeOrder));
    }

    [Fact]
    public void Start_WithInitiativeCombatantOutsideEncounter_Throws()
    {
        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(
            combatantId: "combatant.hero",
            position: 1,
            total: 15),
        CreateInitiativeEntry(
            combatantId: "combatant.outsider",
            position: 2,
            total: 10)
        ];

        Assert.Throws<ArgumentException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                CreateParticipants(),
                initiativeOrder));
    }

    [Fact]
    public void Start_WithDuplicateInitiativePositions_Throws()
    {
        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(
            combatantId: "combatant.hero",
            position: 1,
            total: 15),
        CreateInitiativeEntry(
            combatantId: "combatant.enemy",
            position: 1,
            total: 10)
        ];

        Assert.Throws<ArgumentException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                CreateParticipants(),
                initiativeOrder));
    }

    [Fact]
    public void Start_WithNoncontiguousInitiativePositions_Throws()
    {
        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(
            combatantId: "combatant.hero",
            position: 1,
            total: 15),
        CreateInitiativeEntry(
            combatantId: "combatant.enemy",
            position: 3,
            total: 10)
        ];

        Assert.Throws<ArgumentException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                CreateParticipants(),
                initiativeOrder));
    }
    [Theory]
    [InlineData(EncounterLifecycleState.Victory)]
    [InlineData(EncounterLifecycleState.Defeat)]
    public void DeclareOutcome_WhenEncounterIsActive_ReturnsCompletedState(
    EncounterLifecycleState outcome)
    {
        EncounterState state = EncounterRules.Start(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder());

        EncounterState result =
            EncounterRules.DeclareOutcome(
                state,
                outcome);

        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);
        Assert.Equal(outcome, result.LifecycleState);
        Assert.Equal(
            state.Revision + 1,
            result.Revision);
        Assert.Equal(
            state.EncounterId,
            result.EncounterId);
        Assert.Equal(
            state.ActiveCombatantId,
            result.ActiveCombatantId);
        Assert.NotSame(state, result);
    }

    [Fact]
    public void DeclareOutcome_WithActiveOutcome_Throws()
    {
        EncounterState state = EncounterRules.Start(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterRules.DeclareOutcome(
                state,
                EncounterLifecycleState.Active));
    }

    [Fact]
    public void DeclareOutcome_WithUnsupportedOutcome_Throws()
    {
        EncounterState state = EncounterRules.Start(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterRules.DeclareOutcome(
                state,
                (EncounterLifecycleState)999));
    }

    [Fact]
    public void DeclareOutcome_WhenEncounterIsAlreadyComplete_Throws()
    {
        EncounterState state = EncounterRules.Start(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder());

        state = EncounterRules.DeclareOutcome(
            state,
            EncounterLifecycleState.Victory);

        Assert.Throws<InvalidOperationException>(() =>
            EncounterRules.DeclareOutcome(
                state,
                EncounterLifecycleState.Defeat));
    }
    [Fact]
    public void DeclareOutcome_WhenRoundNumberIsInvalid_ThrowsBeforeTransition()
    {
        EncounterState state = EncounterRules.Start(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder())
            with
        {
            TurnState = EncounterRules.Start(
                    encounterId: "encounter.valid",
                    CreateParticipants(),
                    CreateInitiativeOrder())
                    .TurnState with
            {
                RoundNumber = 0
            }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterRules.DeclareOutcome(
                state,
                EncounterLifecycleState.Victory));

        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);
    }

    [Fact]
    public void DeclareOutcome_WhenActivePositionIsInvalid_ThrowsBeforeTransition()
    {
        EncounterState state = EncounterRules.Start(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder());

        state = state with
        {
            TurnState = state.TurnState with
            {
                ActivePosition = 0
            }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterRules.DeclareOutcome(
                state,
                EncounterLifecycleState.Victory));

        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);
    }

    [Fact]
    public void DeclareOutcome_WhenLifecycleStateIsUnsupported_ThrowsBeforeTransition()
    {
        EncounterState state = EncounterRules.Start(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder())
            with
        {
            LifecycleState =
                    (EncounterLifecycleState)999
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterRules.DeclareOutcome(
                state,
                EncounterLifecycleState.Victory));
    }

    [Fact]
    public void DeclareOutcome_WhenInitiativeOrderNoLongerMatchesParticipants_ThrowsBeforeTransition()
    {
        EncounterState state = EncounterRules.Start(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder());

        InitiativeOrderEntry[] invalidInitiativeOrder =
        [
            CreateInitiativeEntry(
            combatantId: "combatant.hero",
            position: 1,
            total: 15),
        CreateInitiativeEntry(
            combatantId: "combatant.outsider",
            position: 2,
            total: 10)
        ];

        state = state with
        {
            TurnState = state.TurnState with
            {
                InitiativeOrder =
                    Array.AsReadOnly(
                        invalidInitiativeOrder)
            }
        };

        Assert.Throws<ArgumentException>(() =>
            EncounterRules.DeclareOutcome(
                state,
                EncounterLifecycleState.Victory));
    }
    [Fact]
    public void DeclareOutcome_WhenInitiativeListIsNotOrderedByPosition_ThrowsBeforeTransition()
    {
        EncounterState state = EncounterRules.Start(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder());

        InitiativeOrderEntry[] unorderedInitiativeOrder =
        [
            CreateInitiativeEntry(
            combatantId: "combatant.enemy",
            position: 2,
            total: 10),
        CreateInitiativeEntry(
            combatantId: "combatant.hero",
            position: 1,
            total: 15)
        ];

        state = state with
        {
            TurnState = state.TurnState with
            {
                InitiativeOrder =
                    Array.AsReadOnly(
                        unorderedInitiativeOrder)
            }
        };

        Assert.Throws<ArgumentException>(() =>
            EncounterRules.DeclareOutcome(
                state,
                EncounterLifecycleState.Victory));

        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);
    }
    [Fact]
    public void DeclareOutcome_WhenParticipantBecameTerminal_AcceptsEncounterState()
    {
        EncounterState state = EncounterRules.Start(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder());

        CombatantState defeatedEnemy =
            CombatantRules.ResolveDamage(
                state.Participants[1].Combatant with
                {
                    ZeroHitPointPolicy =
                        CombatantZeroHitPointPolicy.Defeated
                },
                damageAmount: 10,
                isCriticalHit: false)
            .State;

        EncounterParticipantState[] updatedParticipants =
        [
            state.Participants[0],
        state.Participants[1] with
        {
            Combatant = defeatedEnemy
        }
        ];

        state = state with
        {
            Participants =
                Array.AsReadOnly(updatedParticipants)
        };

        EncounterState result =
            EncounterRules.DeclareOutcome(
                state,
                EncounterLifecycleState.Victory);

        Assert.Equal(
            EncounterLifecycleState.Victory,
            result.LifecycleState);
        Assert.Equal(
            CombatantLifecycleState.Defeated,
            result.Participants[1]
                .Combatant.LifecycleState);
    }
    [Fact]
    public void Start_WithNegativeMovementSpeed_Throws()
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
            combatantId: "combatant.hero",
            sideId: "side.party",
            movementSpeedFeet: -1),
        CreateParticipant(
            combatantId: "combatant.enemy",
            sideId: "side.enemies")
        ];

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                participants,
                CreateInitiativeOrder()));
    }
    [Fact]
    public void DeclareOutcome_WhenParticipantTurnResourcesAreInvalid_ThrowsBeforeTransition()
    {
        EncounterState state = EncounterRules.Start(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder());

        EncounterParticipantState[] invalidParticipants =
        [
            state.Participants[0] with
        {
            TurnResources =
                state.Participants[0].TurnResources with
                {
                    MovementSpentFeet = 31
                }
        },
        state.Participants[1]
        ];

        state = state with
        {
            Participants =
                Array.AsReadOnly(invalidParticipants)
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterRules.DeclareOutcome(
                state,
                EncounterLifecycleState.Victory));

        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);
    }
    [Fact]
    public void DeclareOutcome_WhenRevisionIsInvalid_ThrowsBeforeTransition()
    {
        EncounterState state = EncounterRules.Start(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder())
            with
        {
            Revision = 0
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterRules.DeclareOutcome(
                state,
                EncounterLifecycleState.Victory));

        Assert.Equal(0, state.Revision);
        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);
    }
    [Fact]
    public void DeclareOutcome_WhenRevisionCannotIncrement_ThrowsBeforeTransition()
    {
        EncounterState state = EncounterRules.Start(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder())
            with
        {
            Revision = long.MaxValue
        };

        Assert.Throws<OverflowException>(() =>
            EncounterRules.DeclareOutcome(
                state,
                EncounterLifecycleState.Victory));

        Assert.Equal(long.MaxValue, state.Revision);
        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);
    }
}

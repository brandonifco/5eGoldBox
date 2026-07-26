using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

/// Completing an encounter: which side wins, and what Complete refuses
/// to do to a state that is not ready to end.
public sealed partial class EncounterRulesTests
{
    [Fact]
    public void Complete_WhenEncounterIsActive_ReturnsCompletedState()
    {
        EncounterState state = StartEncounter(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder());

        EncounterState result =
            EncounterRules.Complete(
                state,
                winningSideId: "side.party");

        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);
        Assert.Null(state.WinningSideId);
        Assert.Equal(
            EncounterLifecycleState.Completed,
            result.LifecycleState);
        Assert.Equal(
            "side.party",
            result.WinningSideId);
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
    public void Complete_WithNullWinningSideId_ReturnsCompletedStateWithoutWinner()
    {
        EncounterState state = StartEncounter(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder());

        EncounterState result =
            EncounterRules.Complete(
                state,
                winningSideId: null);

        Assert.Equal(
            EncounterLifecycleState.Completed,
            result.LifecycleState);
        Assert.Null(result.WinningSideId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Complete_WithBlankWinningSideId_Throws(
        string winningSideId)
    {
        EncounterState state = StartEncounter(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder());

        Assert.Throws<ArgumentException>(() =>
            EncounterRules.Complete(
                state,
                winningSideId));
    }

    [Fact]
    public void Complete_WithUnrepresentedWinningSideId_Throws()
    {
        EncounterState state = StartEncounter(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder());

        Assert.Throws<ArgumentException>(() =>
            EncounterRules.Complete(
                state,
                winningSideId: "side.outsiders"));
    }

    [Fact]
    public void Complete_WhenEncounterIsAlreadyComplete_Throws()
    {
        EncounterState state = StartEncounter(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder());

        state = EncounterRules.Complete(
            state,
            winningSideId: "side.party");

        Assert.Throws<InvalidOperationException>(() =>
            EncounterRules.Complete(
                state,
                winningSideId: "side.enemies"));
    }

    [Fact]
    public void Complete_WhenRoundNumberIsInvalid_ThrowsBeforeTransition()
    {
        EncounterState state = StartEncounter(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder())
            with
        {
            TurnState = StartEncounter(
                    encounterId: "encounter.valid",
                    CreateParticipants(),
                    CreateInitiativeOrder())
                    .TurnState with
            {
                RoundNumber = 0
            }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterRules.Complete(
                state,
                winningSideId: "side.party"));

        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);
    }

    [Fact]
    public void Complete_WhenActivePositionIsInvalid_ThrowsBeforeTransition()
    {
        EncounterState state = StartEncounter(
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
            EncounterRules.Complete(
                state,
                winningSideId: "side.party"));

        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);
    }

    [Fact]
    public void Complete_WhenLifecycleStateIsUnsupported_ThrowsBeforeTransition()
    {
        EncounterState state = StartEncounter(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder())
            with
        {
            LifecycleState =
                    (EncounterLifecycleState)999
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterRules.Complete(
                state,
                winningSideId: "side.party"));
    }

    [Fact]
    public void Complete_WhenInitiativeOrderNoLongerMatchesParticipants_ThrowsBeforeTransition()
    {
        EncounterState state = StartEncounter(
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
            EncounterRules.Complete(
                state,
                winningSideId: "side.party"));
    }

    [Fact]
    public void Complete_WhenInitiativeListIsNotOrderedByPosition_ThrowsBeforeTransition()
    {
        EncounterState state = StartEncounter(
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
            EncounterRules.Complete(
                state,
                winningSideId: "side.party"));

        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);
    }

    [Fact]
    public void Complete_WhenParticipantBecameTerminal_AcceptsEncounterState()
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
                combatantId: "combatant.hero",
                sideId: "side.party",
                startingPosition:
                    new GridPosition(1, 1)),
            CreateParticipant(
                combatantId: "combatant.ally",
                sideId: "side.party",
                startingPosition:
                    new GridPosition(1, 2)),
            CreateParticipant(
                combatantId: "combatant.enemy",
                sideId: "side.enemies",
                startingPosition:
                    new GridPosition(2, 1))
        ];

        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(
                combatantId: "combatant.hero",
                position: 1,
                total: 15),
            CreateInitiativeEntry(
                combatantId: "combatant.ally",
                position: 2,
                total: 12),
            CreateInitiativeEntry(
                combatantId: "combatant.enemy",
                position: 3,
                total: 10)
        ];

        EncounterState state = StartEncounter(
            encounterId: "encounter.test",
            participants,
            initiativeOrder);

        GridPosition originalPosition =
            state.Participants[1].Position;

        CombatantState defeatedAlly =
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
                Combatant = defeatedAlly
            },
            state.Participants[2]
        ];

        state = state with
        {
            Participants =
                Array.AsReadOnly(updatedParticipants)
        };

        EncounterState result =
            EncounterRules.Complete(
                state,
                winningSideId: "side.party");

        Assert.Equal(
            EncounterLifecycleState.Completed,
            result.LifecycleState);
        Assert.Equal(
            "side.party",
            result.WinningSideId);
        Assert.Equal(
            CombatantLifecycleState.Defeated,
            result.Participants[1]
                .Combatant.LifecycleState);
        Assert.Equal(
            originalPosition,
            result.Participants[1].Position);
    }

    [Fact]
    public void Complete_WhenParticipantTurnResourcesAreInvalid_ThrowsBeforeTransition()
    {
        EncounterState state = StartEncounter(
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
            EncounterRules.Complete(
                state,
                winningSideId: "side.party"));

        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);
    }

    [Fact]
    public void Complete_WhenRevisionIsInvalid_ThrowsBeforeTransition()
    {
        EncounterState state = StartEncounter(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder())
            with
        {
            Revision = 0
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterRules.Complete(
                state,
                winningSideId: "side.party"));

        Assert.Equal(0, state.Revision);
        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);
    }

    [Fact]
    public void Complete_WhenRevisionCannotIncrement_ThrowsBeforeTransition()
    {
        EncounterState state = StartEncounter(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder())
            with
        {
            Revision = long.MaxValue
        };

        Assert.Throws<OverflowException>(() =>
            EncounterRules.Complete(
                state,
                winningSideId: "side.party"));

        Assert.Equal(long.MaxValue, state.Revision);
        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);
    }

    [Fact]
    public void Complete_WhenBattlefieldIsInvalid_ThrowsBeforeTransition()
    {
        EncounterState state = StartEncounter(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder());

        state = state with
        {
            Battlefield = state.Battlefield with
            {
                Width = 0
            }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterRules.Complete(
                state,
                winningSideId: "side.party"));

        Assert.Equal(1, state.Revision);
        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);
    }

    [Fact]
    public void Complete_WhenParticipantsSharePosition_ThrowsBeforeTransition()
    {
        EncounterState state = StartEncounter(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder());

        EncounterParticipantState[] participants =
        [
            state.Participants[0],
        state.Participants[1] with
        {
            Position = state.Participants[0].Position
        }
        ];

        state = state with
        {
            Participants =
                Array.AsReadOnly(participants)
        };

        Assert.Throws<ArgumentException>(() =>
            EncounterRules.Complete(
                state,
                winningSideId: "side.party"));

        Assert.Equal(1, state.Revision);
        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);
    }

    [Fact]
    public void Complete_WhenParticipantIsNull_ThrowsBeforeTransition()
    {
        EncounterState state = StartEncounter(
            encounterId: "encounter.test",
            CreateParticipants(),
            CreateInitiativeOrder());

        EncounterParticipantState[] invalidParticipants =
            new EncounterParticipantState[]
            {
                state.Participants[0],
                null!
            };

        state = state with
        {
            Participants =
                Array.AsReadOnly(invalidParticipants)
        };

        Assert.Throws<ArgumentNullException>(() =>
            EncounterRules.Complete(
                state,
                winningSideId: "side.party"));

        Assert.Equal(1, state.Revision);
        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);
    }
}

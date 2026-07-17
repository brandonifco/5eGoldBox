using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

public sealed class EncounterTurnAdvancementRulesTests
{
    [Fact]
    public void Resolve_WithValidCommand_AdvancesAndRefreshesNextTurnResources()
    {
        EncounterState state = CreateEncounter();

        state = ReplaceParticipant(
            state,
            "combatant.hero",
            participant => participant with
            {
                TurnResources =
                    CombatTurnResourceRules.SpendAction(
                        participant.TurnResources)
            });

        state = ReplaceParticipant(
            state,
            "combatant.enemy",
            participant => participant with
            {
                TurnResources =
                    SpendAllResources(
                        participant.TurnResources)
            });

        CombatTurnResources endedTurnResources =
            state.Participants[0].TurnResources;

        EncounterTurnAdvancementResult result =
            EncounterTurnAdvancementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    "combatant.hero"));

        Assert.Equal(
            "combatant.hero",
            result.EndedTurnCombatantId);
        Assert.Equal(
            "combatant.enemy",
            result.ActiveCombatantId);
        Assert.Equal(1, result.PreviousRoundNumber);
        Assert.Equal(1, result.RoundNumber);
        Assert.Equal(1, result.PreviousActivePosition);
        Assert.Equal(2, result.ActivePosition);
        Assert.False(result.StartedNewRound);
        Assert.Empty(result.SkippedCombatantIds);

        Assert.Equal(2, result.State.Revision);
        Assert.Equal(
            "combatant.enemy",
            result.State.ActiveCombatantId);
        Assert.Equal(1, result.State.TurnState.RoundNumber);
        Assert.Equal(2, result.State.TurnState.ActivePosition);

        Assert.Equal(
            endedTurnResources,
            result.State.Participants[0].TurnResources);

        CombatTurnResources refreshedResources =
            result.State.Participants[1].TurnResources;

        Assert.True(refreshedResources.HasActionAvailable);
        Assert.True(
            refreshedResources.HasBonusActionAvailable);
        Assert.True(refreshedResources.HasReactionAvailable);
        Assert.Equal(25, refreshedResources.MovementSpeedFeet);
        Assert.Equal(0, refreshedResources.MovementSpentFeet);
        Assert.Equal(
            25,
            refreshedResources.MovementRemainingFeet);

        Assert.Equal(1, state.Revision);
        Assert.Equal(
            "combatant.hero",
            state.ActiveCombatantId);
        Assert.False(
            state.Participants[1]
                .TurnResources.HasActionAvailable);
        Assert.Equal(
            25,
            state.Participants[1]
                .TurnResources.MovementSpentFeet);
    }

    [Fact]
    public void Resolve_WhenLastCombatantEndsTurn_StartsNewRound()
    {
        EncounterState state = CreateEncounter();

        state = ReplaceParticipant(
            state,
            "combatant.hero",
            participant => participant with
            {
                TurnResources =
                    SpendAllResources(
                        participant.TurnResources)
            });

        state = state with
        {
            Revision = 7,
            TurnState =
                CombatTurnRules.AdvanceTurn(
                    state.TurnState)
        };

        EncounterTurnAdvancementResult result =
            EncounterTurnAdvancementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    "combatant.enemy"));

        Assert.Equal(1, result.PreviousRoundNumber);
        Assert.Equal(2, result.RoundNumber);
        Assert.Equal(2, result.PreviousActivePosition);
        Assert.Equal(1, result.ActivePosition);
        Assert.True(result.StartedNewRound);
        Assert.Equal(
            "combatant.hero",
            result.ActiveCombatantId);
        Assert.Equal(8, result.State.Revision);

        CombatTurnResources refreshedResources =
            result.State.Participants[0].TurnResources;

        Assert.True(refreshedResources.HasActionAvailable);
        Assert.True(
            refreshedResources.HasBonusActionAvailable);
        Assert.True(refreshedResources.HasReactionAvailable);
        Assert.Equal(0, refreshedResources.MovementSpentFeet);
        Assert.Equal(
            refreshedResources.MovementSpeedFeet,
            refreshedResources.MovementRemainingFeet);
    }

    [Fact]
    public void Resolve_WhenNextCombatantIsTerminal_SkipsCombatant()
    {
        EncounterState state =
            CreateThreeCombatantEncounter();

        state = ReplaceParticipant(
            state,
            "combatant.enemy.one",
            participant => participant with
            {
                Combatant =
                    CreateCombatantWithLifecycle(
                        "combatant.enemy.one",
                        CombatantLifecycleState.Defeated)
            });

        state = ReplaceParticipant(
            state,
            "combatant.enemy.two",
            participant => participant with
            {
                TurnResources =
                    SpendAllResources(
                        participant.TurnResources)
            });

        EncounterTurnAdvancementResult result =
            EncounterTurnAdvancementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    "combatant.hero"));

        Assert.Equal(
            "combatant.enemy.two",
            result.ActiveCombatantId);
        Assert.Equal(3, result.ActivePosition);
        Assert.Equal(
            new[] { "combatant.enemy.one" },
            result.SkippedCombatantIds);

        CombatTurnResources refreshedResources =
            result.State.Participants[2].TurnResources;

        Assert.True(refreshedResources.HasActionAvailable);
        Assert.True(
            refreshedResources.HasBonusActionAvailable);
        Assert.True(refreshedResources.HasReactionAvailable);
        Assert.Equal(0, refreshedResources.MovementSpentFeet);

        IList<string> skippedCombatantIds =
            Assert.IsAssignableFrom<IList<string>>(
                result.SkippedCombatantIds);

        Assert.Throws<NotSupportedException>(() =>
            skippedCombatantIds.Add(
                "combatant.replacement"));
    }

    [Fact]
    public void Resolve_WhenTerminalCombatantIsSkippedAcrossRoundBoundary_StartsNewRound()
    {
        EncounterState state =
            CreateThreeCombatantEncounter();

        state = ReplaceParticipant(
            state,
            "combatant.enemy.two",
            participant => participant with
            {
                Combatant =
                    CreateCombatantWithLifecycle(
                        "combatant.enemy.two",
                        CombatantLifecycleState.Dead)
            });

        state = state with
        {
            TurnState =
                CombatTurnRules.AdvanceTurn(
                    state.TurnState)
        };

        EncounterTurnAdvancementResult result =
            EncounterTurnAdvancementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    "combatant.enemy.one"));

        Assert.Equal(
            "combatant.hero",
            result.ActiveCombatantId);
        Assert.Equal(
            new[] { "combatant.enemy.two" },
            result.SkippedCombatantIds);
        Assert.Equal(1, result.PreviousRoundNumber);
        Assert.Equal(2, result.RoundNumber);
        Assert.Equal(2, result.PreviousActivePosition);
        Assert.Equal(1, result.ActivePosition);
        Assert.True(result.StartedNewRound);
    }

    [Theory]
    [InlineData(CombatantLifecycleState.Dying)]
    [InlineData(CombatantLifecycleState.Stable)]
    public void Resolve_WhenNextCombatantIsUnconscious_DoesNotSkipCombatant(
        CombatantLifecycleState lifecycleState)
    {
        EncounterState state = CreateEncounter();

        state = ReplaceParticipant(
            state,
            "combatant.enemy",
            participant => participant with
            {
                Combatant =
                    CreateCombatantWithLifecycle(
                        "combatant.enemy",
                        lifecycleState)
            });

        EncounterTurnAdvancementResult result =
            EncounterTurnAdvancementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    "combatant.hero"));

        Assert.Equal(
            "combatant.enemy",
            result.ActiveCombatantId);
        Assert.Equal(2, result.ActivePosition);
        Assert.Empty(result.SkippedCombatantIds);
        Assert.Equal(
            lifecycleState,
            result.State.Participants[1]
                .Combatant.LifecycleState);
    }

    [Theory]
    [InlineData(CombatantLifecycleState.Dying)]
    [InlineData(CombatantLifecycleState.Stable)]
    [InlineData(CombatantLifecycleState.Dead)]
    [InlineData(CombatantLifecycleState.Defeated)]
    public void Resolve_WhenActiveCombatantCannotAct_AllowsTurnToEnd(
        CombatantLifecycleState lifecycleState)
    {
        EncounterState state = CreateEncounter();

        state = ReplaceParticipant(
            state,
            "combatant.hero",
            participant => participant with
            {
                Combatant =
                    CreateCombatantWithLifecycle(
                        "combatant.hero",
                        lifecycleState)
            });

        EncounterTurnAdvancementResult result =
            EncounterTurnAdvancementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    "combatant.hero"));

        Assert.Equal(
            "combatant.enemy",
            result.ActiveCombatantId);
        Assert.Equal(2, result.ActivePosition);
    }

    [Fact]
    public void Resolve_WhenNoOtherNonterminalCombatantExists_ThrowsWithoutTransition()
    {
        EncounterState state = CreateEncounter();

        state = ReplaceParticipant(
            state,
            "combatant.enemy",
            participant => participant with
            {
                Combatant =
                    CreateCombatantWithLifecycle(
                        "combatant.enemy",
                        CombatantLifecycleState.Defeated)
            });

        Assert.Throws<InvalidOperationException>(() =>
            EncounterTurnAdvancementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    "combatant.hero")));

        Assert.Equal(1, state.Revision);
        Assert.Equal(
            "combatant.hero",
            state.ActiveCombatantId);
        Assert.Equal(1, state.TurnState.RoundNumber);
        Assert.Equal(1, state.TurnState.ActivePosition);
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
            EncounterTurnAdvancementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    "combatant.hero")));
    }

    [Fact]
    public void Resolve_WithStaleRevision_ThrowsWithoutTransition()
    {
        EncounterState state = CreateEncounter();

        EncounterTurnAdvancementCommand command = new()
        {
            ExpectedRevision = state.Revision + 1,
            ActorCombatantId = "combatant.hero"
        };

        Assert.Throws<InvalidOperationException>(() =>
            EncounterTurnAdvancementRules.Resolve(
                state,
                command));

        Assert.Equal(1, state.Revision);
        Assert.Equal(
            "combatant.hero",
            state.ActiveCombatantId);
    }

    [Fact]
    public void Resolve_WhenActorIsNotParticipant_Throws()
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<ArgumentException>(() =>
            EncounterTurnAdvancementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    "combatant.outsider")));
    }

    [Fact]
    public void Resolve_WhenActorIsNotActive_Throws()
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<InvalidOperationException>(() =>
            EncounterTurnAdvancementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    "combatant.enemy")));

        Assert.Equal(
            "combatant.hero",
            state.ActiveCombatantId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Resolve_WithInvalidExpectedRevision_Throws(
        long expectedRevision)
    {
        EncounterState state = CreateEncounter();

        EncounterTurnAdvancementCommand command = new()
        {
            ExpectedRevision = expectedRevision,
            ActorCombatantId = "combatant.hero"
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterTurnAdvancementRules.Resolve(
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
        EncounterState state = CreateEncounter();

        EncounterTurnAdvancementCommand command = new()
        {
            ExpectedRevision = state.Revision,
            ActorCombatantId = actorCombatantId
        };

        Assert.Throws<ArgumentException>(() =>
            EncounterTurnAdvancementRules.Resolve(
                state,
                command));
    }

    [Fact]
    public void Resolve_WithNullActorCombatantId_Throws()
    {
        EncounterState state = CreateEncounter();

        EncounterTurnAdvancementCommand command = new()
        {
            ExpectedRevision = state.Revision,
            ActorCombatantId = null!
        };

        Assert.Throws<ArgumentException>(() =>
            EncounterTurnAdvancementRules.Resolve(
                state,
                command));
    }

    [Fact]
    public void Resolve_WithNullState_Throws()
    {
        EncounterTurnAdvancementCommand command = new()
        {
            ExpectedRevision = 1,
            ActorCombatantId = "combatant.hero"
        };

        Assert.Throws<ArgumentNullException>(() =>
            EncounterTurnAdvancementRules.Resolve(
                null!,
                command));
    }

    [Fact]
    public void Resolve_WithNullCommand_Throws()
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<ArgumentNullException>(() =>
            EncounterTurnAdvancementRules.Resolve(
                state,
                null!));
    }

    [Fact]
    public void Resolve_WhenRevisionWouldOverflow_ThrowsWithoutTransition()
    {
        EncounterState state = CreateEncounter() with
        {
            Revision = long.MaxValue
        };

        Assert.Throws<OverflowException>(() =>
            EncounterTurnAdvancementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    "combatant.hero")));

        Assert.Equal(long.MaxValue, state.Revision);
        Assert.Equal(
            "combatant.hero",
            state.ActiveCombatantId);
    }

    [Fact]
    public void Resolve_WhenRoundNumberWouldOverflow_ThrowsWithoutTransition()
    {
        EncounterState state = CreateEncounter();

        state = state with
        {
            TurnState = state.TurnState with
            {
                RoundNumber = int.MaxValue,
                ActivePosition = 2
            }
        };

        Assert.Throws<OverflowException>(() =>
            EncounterTurnAdvancementRules.Resolve(
                state,
                CreateCommand(
                    state,
                    "combatant.enemy")));

        Assert.Equal(1, state.Revision);
        Assert.Equal(
            int.MaxValue,
            state.TurnState.RoundNumber);
        Assert.Equal(2, state.TurnState.ActivePosition);
        Assert.Equal(
            "combatant.enemy",
            state.ActiveCombatantId);
    }

    private static EncounterState CreateEncounter()
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
                combatantId: "combatant.hero",
                sideId: "side.party",
                movementSpeedFeet: 30,
                position: new GridPosition(1, 1)),
            CreateParticipant(
                combatantId: "combatant.enemy",
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

    private static EncounterState
        CreateThreeCombatantEncounter()
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
                combatantId: "combatant.hero",
                sideId: "side.party",
                movementSpeedFeet: 30,
                position: new GridPosition(1, 1)),
            CreateParticipant(
                combatantId: "combatant.enemy.one",
                sideId: "side.enemies",
                movementSpeedFeet: 25,
                position: new GridPosition(2, 1)),
            CreateParticipant(
                combatantId: "combatant.enemy.two",
                sideId: "side.enemies",
                movementSpeedFeet: 20,
                position: new GridPosition(3, 1))
        ];

        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(
                combatantId: "combatant.hero",
                position: 1,
                total: 15),
            CreateInitiativeEntry(
                combatantId: "combatant.enemy.one",
                position: 2,
                total: 12),
            CreateInitiativeEntry(
                combatantId: "combatant.enemy.two",
                position: 3,
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
            string combatantId,
            string sideId,
            int movementSpeedFeet,
            GridPosition position)
    {
        return new EncounterParticipantSetup
        {
            Combatant = CombatantRules.Create(
                combatantId,
                maximumHitPoints: 10,
                CombatantZeroHitPointPolicy
                    .DeathSavingThrows),
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

    private static EncounterTurnAdvancementCommand
        CreateCommand(
            EncounterState state,
            string actorCombatantId)
    {
        return new EncounterTurnAdvancementCommand
        {
            ExpectedRevision = state.Revision,
            ActorCombatantId = actorCombatantId
        };
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

    private static CombatTurnResources SpendAllResources(
        CombatTurnResources resources)
    {
        CombatTurnResources resolved =
            CombatTurnResourceRules.SpendAction(
                resources);

        resolved =
            CombatTurnResourceRules.SpendBonusAction(
                resolved);

        resolved =
            CombatTurnResourceRules.SpendReaction(
                resolved);

        return CombatTurnResourceRules.SpendMovement(
            resolved,
            resolved.MovementRemainingFeet);
    }

    private static CombatantState
        CreateCombatantWithLifecycle(
            string combatantId,
            CombatantLifecycleState lifecycleState)
    {
        CombatantState conscious =
            CombatantRules.Create(
                combatantId,
                maximumHitPoints: 10,
                CombatantZeroHitPointPolicy
                    .DeathSavingThrows);

        CombatantState dying =
            CombatantRules.ResolveDamage(
                conscious,
                damageAmount: 10,
                isCriticalHit: false)
            .State;

        return lifecycleState switch
        {
            CombatantLifecycleState.Conscious =>
                conscious,

            CombatantLifecycleState.Dying =>
                dying,

            CombatantLifecycleState.Stable =>
                dying with
                {
                    Health = dying.Health with
                    {
                        DeathSavingThrows =
                            DeathSavingThrowRules.Create()
                            with
                            {
                                IsStable = true
                            }
                    }
                },

            CombatantLifecycleState.Dead =>
                CombatantRules.ResolveDamage(
                    conscious,
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
                "Unsupported combatant lifecycle state.")
        };
    }
}

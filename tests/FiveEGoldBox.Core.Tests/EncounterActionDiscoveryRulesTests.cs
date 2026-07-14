using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

public sealed class EncounterActionDiscoveryRulesTests
{
    [Fact]
    public void Discover_WithAvailableActiveActorOptions_ReturnsLegalEvaluations()
    {
        EncounterState state = CreateEncounter();

        EncounterActionCandidate[] candidates =
        [
            CreateCandidate(
                actionOptionId: "option.action",
                actorCombatantId: "combatant.hero",
                EncounterActionTiming.Action),
            CreateCandidate(
                actionOptionId: "option.bonus_action",
                actorCombatantId: "combatant.hero",
                EncounterActionTiming.BonusAction),
            CreateCandidate(
                actionOptionId: "option.movement",
                actorCombatantId: "combatant.hero",
                EncounterActionTiming.Movement)
        ];

        EncounterActionDiscoveryResult result =
            EncounterActionDiscoveryRules.Discover(
                state,
                candidates);

        Assert.Equal(state.EncounterId, result.EncounterId);
        Assert.Equal(state.Revision, result.EncounterRevision);
        Assert.Equal(3, result.Evaluations.Count);

        Assert.All(
            result.Evaluations,
            evaluation =>
            {
                Assert.True(evaluation.IsCommonlyLegal);
                Assert.Equal(
                    EncounterActionUnavailabilityReason.None,
                    evaluation.UnavailabilityReason);
                Assert.Equal(
                    state.Revision,
                    evaluation.EncounterRevision);
            });
    }

    private static EncounterState CreateEncounter()
    {
        EncounterParticipantSetup[] participants =
        [
            new EncounterParticipantSetup
            {
                Combatant = CombatantRules.Create(
                    combatantId: "combatant.hero",
                    maximumHitPoints: 10,
                    CombatantZeroHitPointPolicy
                        .DeathSavingThrows),
                SideId = "side.party",
                MovementSpeedFeet = 30
            },
            new EncounterParticipantSetup
            {
                Combatant = CombatantRules.Create(
                    combatantId: "combatant.enemy",
                    maximumHitPoints: 10,
                    CombatantZeroHitPointPolicy
                        .DeathSavingThrows),
                SideId = "side.enemies",
                MovementSpeedFeet = 30
            }
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
            participants,
            initiativeOrder);
    }

    private static EncounterActionCandidate
        CreateCandidate(
            string actionOptionId,
            string actorCombatantId,
            EncounterActionTiming timing)
    {
        return new EncounterActionCandidate
        {
            ActionOptionId = actionOptionId,
            ActorCombatantId = actorCombatantId,
            Timing = timing
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
            Initiative = InitiativeRules.ResolveInitiative(
                D20RollMode.Normal,
                firstRoll: total,
                secondRoll: null,
                initiativeBonus: 0),
            Position = position,
            HasTiedInitiative = false
        };
    }
    [Fact]
    public void Discover_WhenEncounterIsComplete_ReturnsEncounterCompleted()
    {
        EncounterState state = EncounterRules.DeclareOutcome(
            CreateEncounter(),
            EncounterLifecycleState.Victory);

        EncounterActionCandidate[] candidates =
        [
            CreateCandidate(
            actionOptionId: "option.action",
            actorCombatantId: "combatant.hero",
            EncounterActionTiming.Action)
        ];

        EncounterActionDiscoveryResult result =
            EncounterActionDiscoveryRules.Discover(
                state,
                candidates);

        EncounterActionEvaluation evaluation =
            Assert.Single(result.Evaluations);

        Assert.False(evaluation.IsCommonlyLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason
                .EncounterCompleted,
            evaluation.UnavailabilityReason);
        Assert.Equal(
            state.Revision,
            evaluation.EncounterRevision);
    }
    [Fact]
    public void Discover_WhenActorIsNotParticipant_ReturnsActorNotParticipant()
    {
        EncounterState state = CreateEncounter();

        EncounterActionCandidate[] candidates =
        [
            CreateCandidate(
            actionOptionId: "option.action",
            actorCombatantId: "combatant.outsider",
            EncounterActionTiming.Action)
        ];

        EncounterActionDiscoveryResult result =
            EncounterActionDiscoveryRules.Discover(
                state,
                candidates);

        EncounterActionEvaluation evaluation =
            Assert.Single(result.Evaluations);

        Assert.False(evaluation.IsCommonlyLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason
                .ActorNotParticipant,
            evaluation.UnavailabilityReason);
    }
    [Fact]
    public void Discover_WhenActorIsNotActive_ReturnsActorNotActive()
    {
        EncounterState state = CreateEncounter();

        EncounterActionCandidate[] candidates =
        [
            CreateCandidate(
            actionOptionId: "option.action",
            actorCombatantId: "combatant.enemy",
            EncounterActionTiming.Action),
        CreateCandidate(
            actionOptionId: "option.bonus_action",
            actorCombatantId: "combatant.enemy",
            EncounterActionTiming.BonusAction),
        CreateCandidate(
            actionOptionId: "option.movement",
            actorCombatantId: "combatant.enemy",
            EncounterActionTiming.Movement)
        ];

        EncounterActionDiscoveryResult result =
            EncounterActionDiscoveryRules.Discover(
                state,
                candidates);

        Assert.All(
            result.Evaluations,
            evaluation =>
            {
                Assert.False(evaluation.IsCommonlyLegal);
                Assert.Equal(
                    EncounterActionUnavailabilityReason
                        .ActorNotActive,
                    evaluation.UnavailabilityReason);
            });
    }
    [Fact]
    public void Discover_WhenActionIsSpent_ReturnsActionUnavailable()
    {
        EncounterState state = CreateEncounter();

        EncounterParticipantState[] participants =
        [
            state.Participants[0] with
        {
            TurnResources =
                CombatTurnResourceRules.SpendAction(
                    state.Participants[0].TurnResources)
        },
        state.Participants[1]
        ];

        state = state with
        {
            Participants = Array.AsReadOnly(participants)
        };

        EncounterActionDiscoveryResult result =
            EncounterActionDiscoveryRules.Discover(
                state,
                [
                    CreateCandidate(
                    actionOptionId: "option.action",
                    actorCombatantId: "combatant.hero",
                    EncounterActionTiming.Action)
                ]);

        EncounterActionEvaluation evaluation =
            Assert.Single(result.Evaluations);

        Assert.False(evaluation.IsCommonlyLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason
                .ActionUnavailable,
            evaluation.UnavailabilityReason);
    }
    [Fact]
    public void Discover_WhenBonusActionIsSpent_ReturnsBonusActionUnavailable()
    {
        EncounterState state = CreateEncounter();

        EncounterParticipantState[] participants =
        [
            state.Participants[0] with
        {
            TurnResources =
                CombatTurnResourceRules.SpendBonusAction(
                    state.Participants[0].TurnResources)
        },
        state.Participants[1]
        ];

        state = state with
        {
            Participants = Array.AsReadOnly(participants)
        };

        EncounterActionDiscoveryResult result =
            EncounterActionDiscoveryRules.Discover(
                state,
                [
                    CreateCandidate(
                    actionOptionId: "option.bonus_action",
                    actorCombatantId: "combatant.hero",
                    EncounterActionTiming.BonusAction)
                ]);

        EncounterActionEvaluation evaluation =
            Assert.Single(result.Evaluations);

        Assert.False(evaluation.IsCommonlyLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason
                .BonusActionUnavailable,
            evaluation.UnavailabilityReason);
    }

    [Fact]
    public void Discover_WhenReactionIsAvailable_ReturnsReactionWindowRequired()
    {
        EncounterState state = CreateEncounter();

        EncounterActionDiscoveryResult result =
            EncounterActionDiscoveryRules.Discover(
                state,
                [
                    CreateCandidate(
                    actionOptionId: "option.reaction",
                    actorCombatantId: "combatant.enemy",
                    EncounterActionTiming.Reaction)
                ]);

        EncounterActionEvaluation evaluation =
            Assert.Single(result.Evaluations);

        Assert.False(evaluation.IsCommonlyLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason
                .ReactionWindowRequired,
            evaluation.UnavailabilityReason);
    }

    [Fact]
    public void Discover_WhenReactionIsSpent_ReturnsReactionUnavailable()
    {
        EncounterState state = CreateEncounter();

        EncounterParticipantState[] participants =
        [
            state.Participants[0],
        state.Participants[1] with
        {
            TurnResources =
                CombatTurnResourceRules.SpendReaction(
                    state.Participants[1].TurnResources)
        }
        ];

        state = state with
        {
            Participants = Array.AsReadOnly(participants)
        };

        EncounterActionDiscoveryResult result =
            EncounterActionDiscoveryRules.Discover(
                state,
                [
                    CreateCandidate(
                    actionOptionId: "option.reaction",
                    actorCombatantId: "combatant.enemy",
                    EncounterActionTiming.Reaction)
                ]);

        EncounterActionEvaluation evaluation =
            Assert.Single(result.Evaluations);

        Assert.False(evaluation.IsCommonlyLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason
                .ReactionUnavailable,
            evaluation.UnavailabilityReason);
    }

    [Fact]
    public void Discover_WhenMovementIsFullySpent_ReturnsMovementUnavailable()
    {
        EncounterState state = CreateEncounter();

        EncounterParticipantState[] participants =
        [
            state.Participants[0] with
        {
            TurnResources =
                CombatTurnResourceRules.SpendMovement(
                    state.Participants[0].TurnResources,
                    movementFeet: 30)
        },
        state.Participants[1]
        ];

        state = state with
        {
            Participants = Array.AsReadOnly(participants)
        };

        EncounterActionDiscoveryResult result =
            EncounterActionDiscoveryRules.Discover(
                state,
                [
                    CreateCandidate(
                    actionOptionId: "option.movement",
                    actorCombatantId: "combatant.hero",
                    EncounterActionTiming.Movement)
                ]);

        EncounterActionEvaluation evaluation =
            Assert.Single(result.Evaluations);

        Assert.False(evaluation.IsCommonlyLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason
                .MovementUnavailable,
            evaluation.UnavailabilityReason);
    }

    [Fact]
    public void Discover_WithTurnBoundaryTiming_ReturnsUnsupportedTiming()
    {
        EncounterState state = CreateEncounter();

        EncounterActionDiscoveryResult result =
            EncounterActionDiscoveryRules.Discover(
                state,
                [
                    CreateCandidate(
                    actionOptionId: "option.end_turn",
                    actorCombatantId: "combatant.hero",
                    EncounterActionTiming.TurnBoundary)
                ]);

        EncounterActionEvaluation evaluation =
            Assert.Single(result.Evaluations);

        Assert.False(evaluation.IsCommonlyLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason
                .UnsupportedTiming,
            evaluation.UnavailabilityReason);
    }
    [Fact]
    public void Discover_PreservesCandidateOrder()
    {
        EncounterState state = CreateEncounter();

        EncounterActionCandidate[] candidates =
        [
            CreateCandidate(
            actionOptionId: "option.movement",
            actorCombatantId: "combatant.hero",
            EncounterActionTiming.Movement),
        CreateCandidate(
            actionOptionId: "option.action",
            actorCombatantId: "combatant.hero",
            EncounterActionTiming.Action),
        CreateCandidate(
            actionOptionId: "option.bonus_action",
            actorCombatantId: "combatant.hero",
            EncounterActionTiming.BonusAction)
        ];

        EncounterActionDiscoveryResult result =
            EncounterActionDiscoveryRules.Discover(
                state,
                candidates);

        Assert.Equal(
            [
                "option.movement",
            "option.action",
            "option.bonus_action"
            ],
            result.Evaluations
                .Select(evaluation =>
                    evaluation.ActionOptionId));
    }

    [Fact]
    public void Discover_ProtectsEvaluationsFromCandidateCollectionMutation()
    {
        EncounterState state = CreateEncounter();

        EncounterActionCandidate[] candidates =
        [
            CreateCandidate(
            actionOptionId: "option.original",
            actorCombatantId: "combatant.hero",
            EncounterActionTiming.Action)
        ];

        EncounterActionDiscoveryResult result =
            EncounterActionDiscoveryRules.Discover(
                state,
                candidates);

        candidates[0] = CreateCandidate(
            actionOptionId: "option.replacement",
            actorCombatantId: "combatant.enemy",
            EncounterActionTiming.Reaction);

        EncounterActionEvaluation evaluation =
            Assert.Single(result.Evaluations);

        Assert.Equal(
            "option.original",
            evaluation.ActionOptionId);
        Assert.Equal(
            "combatant.hero",
            evaluation.ActorCombatantId);
    }

    [Fact]
    public void Discover_DoesNotMutateEncounterState()
    {
        EncounterState state = CreateEncounter();

        EncounterActionDiscoveryResult result =
            EncounterActionDiscoveryRules.Discover(
                state,
                [
                    CreateCandidate(
                    actionOptionId: "option.action",
                    actorCombatantId: "combatant.hero",
                    EncounterActionTiming.Action)
                ]);

        Assert.Equal(1, state.Revision);
        Assert.True(
            state.Participants[0]
                .TurnResources.HasActionAvailable);
        Assert.Equal(
            0,
            state.Participants[0]
                .TurnResources.MovementSpentFeet);
        Assert.Equal(
            state.Revision,
            result.EncounterRevision);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Discover_WithBlankActionOptionId_Throws(
        string actionOptionId)
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<ArgumentException>(() =>
            EncounterActionDiscoveryRules.Discover(
                state,
                [
                    CreateCandidate(
                    actionOptionId,
                    actorCombatantId: "combatant.hero",
                    EncounterActionTiming.Action)
                ]));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Discover_WithBlankActorCombatantId_Throws(
        string actorCombatantId)
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<ArgumentException>(() =>
            EncounterActionDiscoveryRules.Discover(
                state,
                [
                    CreateCandidate(
                    actionOptionId: "option.action",
                    actorCombatantId,
                    EncounterActionTiming.Action)
                ]));
    }

    [Fact]
    public void Discover_WithDuplicateActionOptionIds_Throws()
    {
        EncounterState state = CreateEncounter();

        EncounterActionCandidate[] candidates =
        [
            CreateCandidate(
            actionOptionId: "option.duplicate",
            actorCombatantId: "combatant.hero",
            EncounterActionTiming.Action),
        CreateCandidate(
            actionOptionId: "option.duplicate",
            actorCombatantId: "combatant.enemy",
            EncounterActionTiming.Reaction)
        ];

        Assert.Throws<ArgumentException>(() =>
            EncounterActionDiscoveryRules.Discover(
                state,
                candidates));
    }

    [Fact]
    public void Discover_WithUnsupportedTiming_Throws()
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterActionDiscoveryRules.Discover(
                state,
                [
                    CreateCandidate(
                    actionOptionId: "option.invalid",
                    actorCombatantId: "combatant.hero",
                    (EncounterActionTiming)999)
                ]));
    }
    [Theory]
    [InlineData(CombatantLifecycleState.Dying)]
    [InlineData(CombatantLifecycleState.Stable)]
    [InlineData(CombatantLifecycleState.Dead)]
    [InlineData(CombatantLifecycleState.Defeated)]
    public void Discover_WhenActorCannotAct_ReturnsActorCannotAct(
    CombatantLifecycleState lifecycleState)
    {
        EncounterState state = CreateEncounter();

        EncounterParticipantState[] participants =
        [
            state.Participants[0] with
        {
            Combatant =
                CreateCombatantWithLifecycle(
                    lifecycleState)
        },
        state.Participants[1]
        ];

        state = state with
        {
            Participants = Array.AsReadOnly(participants)
        };

        EncounterActionDiscoveryResult result =
            EncounterActionDiscoveryRules.Discover(
                state,
                [
                    CreateCandidate(
                    actionOptionId: "option.action",
                    actorCombatantId: "combatant.hero",
                    EncounterActionTiming.Action)
                ]);

        EncounterActionEvaluation evaluation =
            Assert.Single(result.Evaluations);

        Assert.False(evaluation.IsCommonlyLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason
                .ActorCannotAct,
            evaluation.UnavailabilityReason);
    }

    private static CombatantState
        CreateCombatantWithLifecycle(
            CombatantLifecycleState lifecycleState)
    {
        CombatantState conscious =
            CombatantRules.Create(
                combatantId: "combatant.hero",
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
            CombatantLifecycleState.Dying =>
                dying,

            CombatantLifecycleState.Stable =>
                dying with
                {
                    Health = dying.Health with
                    {
                        DeathSavingThrows =
                            DeathSavingThrowRules.Create() with
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
                        combatantId: "combatant.hero",
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
    [Fact]
    public void Discover_WithNoCandidates_ReturnsEmptyResult()
    {
        EncounterState state = CreateEncounter();

        EncounterActionDiscoveryResult result =
            EncounterActionDiscoveryRules.Discover(
                state,
                Array.Empty<EncounterActionCandidate>());

        Assert.Equal(state.EncounterId, result.EncounterId);
        Assert.Equal(state.Revision, result.EncounterRevision);
        Assert.Empty(result.Evaluations);
    }

    [Fact]
    public void Discover_WithNullCandidate_Throws()
    {
        EncounterState state = CreateEncounter();

        EncounterActionCandidate[] candidates =
        [
            null!
        ];

        Assert.Throws<ArgumentNullException>(() =>
            EncounterActionDiscoveryRules.Discover(
                state,
                candidates));
    }

    [Fact]
    public void Discover_WhenEncounterRevisionIsInvalid_Throws()
    {
        EncounterState state = CreateEncounter() with
        {
            Revision = 0
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterActionDiscoveryRules.Discover(
                state,
                [
                    CreateCandidate(
                    actionOptionId: "option.action",
                    actorCombatantId: "combatant.hero",
                    EncounterActionTiming.Action)
                ]));
    }

    [Fact]
    public void Discover_WhenCompletedAndActorIsNotParticipant_ReturnsEncounterCompleted()
    {
        EncounterState state = EncounterRules.DeclareOutcome(
            CreateEncounter(),
            EncounterLifecycleState.Victory);

        EncounterActionDiscoveryResult result =
            EncounterActionDiscoveryRules.Discover(
                state,
                [
                    CreateCandidate(
                    actionOptionId: "option.action",
                    actorCombatantId: "combatant.outsider",
                    EncounterActionTiming.Action)
                ]);

        EncounterActionEvaluation evaluation =
            Assert.Single(result.Evaluations);

        Assert.Equal(
            EncounterActionUnavailabilityReason
                .EncounterCompleted,
            evaluation.UnavailabilityReason);
    }
}

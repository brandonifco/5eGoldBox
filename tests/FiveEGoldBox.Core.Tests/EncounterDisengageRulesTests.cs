using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

public sealed class EncounterDisengageRulesTests
{
    private const string HeroId = "combatant.hero";
    private const string EnemyId = "combatant.enemy";

    [Fact]
    public void Resolve_SpendsTheActionAndMarksTheTurnAsDisengaged()
    {
        EncounterState state = CreateEncounter();

        EncounterDisengageResult result =
            EncounterDisengageRules.Resolve(
                state,
                new EncounterDisengageCommand
                {
                    ExpectedRevision = state.Revision,
                    ActorCombatantId = HeroId
                });

        EncounterParticipantState hero = FindParticipant(result.State, HeroId);

        Assert.False(hero.TurnResources.HasActionAvailable);
        Assert.True(hero.TurnResources.HasDisengaged);
        Assert.Equal(state.Revision + 1, result.State.Revision);
    }

    /// Movement is untouched — that is the point. Disengage buys safe
    /// movement, it does not replace or spend it.
    [Fact]
    public void Resolve_LeavesMovementAndTheReactionAlone()
    {
        EncounterState state = CreateEncounter();

        EncounterDisengageResult result =
            EncounterDisengageRules.Resolve(
                state,
                new EncounterDisengageCommand
                {
                    ExpectedRevision = state.Revision,
                    ActorCombatantId = HeroId
                });

        EncounterParticipantState hero = FindParticipant(result.State, HeroId);

        Assert.Equal(0, hero.TurnResources.MovementSpentFeet);
        Assert.Equal(30, hero.TurnResources.MovementRemainingFeet);
        Assert.True(hero.TurnResources.HasReactionAvailable);
    }

    [Fact]
    public void Resolve_Twice_IsRefusedRatherThanQuietlySpendingASecondAction()
    {
        EncounterState state = CreateEncounter();
        EncounterState afterFirst = EncounterDisengageRules.Resolve(
            state,
            new EncounterDisengageCommand
            {
                ExpectedRevision = state.Revision,
                ActorCombatantId = HeroId
            }).State;

        Assert.Throws<InvalidOperationException>(
            () => EncounterDisengageRules.Resolve(
                afterFirst,
                new EncounterDisengageCommand
                {
                    ExpectedRevision = afterFirst.Revision,
                    ActorCombatantId = HeroId
                }));
    }

    [Fact]
    public void Resolve_ByANonActiveCombatant_Throws()
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<InvalidOperationException>(
            () => EncounterDisengageRules.Resolve(
                state,
                new EncounterDisengageCommand
                {
                    ExpectedRevision = state.Revision,
                    ActorCombatantId = EnemyId
                }));
    }

    [Fact]
    public void CanDisengage_IsTrueForTheActiveCombatantAndFalseAfterwards()
    {
        EncounterState state = CreateEncounter();

        Assert.True(EncounterDisengageRules.CanDisengage(state, HeroId));
        Assert.False(EncounterDisengageRules.CanDisengage(state, EnemyId));

        EncounterState afterDisengage = EncounterDisengageRules.Resolve(
            state,
            new EncounterDisengageCommand
            {
                ExpectedRevision = state.Revision,
                ActorCombatantId = HeroId
            }).State;

        Assert.False(
            EncounterDisengageRules.CanDisengage(afterDisengage, HeroId));
    }

    /// Ending the turn has to clear it, or one Disengage would make every
    /// later turn's movement free.
    [Fact]
    public void StartTurn_ClearsTheDisengagedFlag()
    {
        CombatTurnResources disengaged = CombatTurnResourceRules.Disengage(
            CombatTurnResourceRules.StartTurn(movementSpeedFeet: 30));

        Assert.True(disengaged.HasDisengaged);
        Assert.False(
            CombatTurnResourceRules.StartTurn(movementSpeedFeet: 30)
                .HasDisengaged);
    }

    private static EncounterParticipantState FindParticipant(
        EncounterState state,
        string combatantId)
    {
        return Assert.Single(
            state.Participants,
            participant => string.Equals(
                participant.Combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal));
    }

    private static EncounterState CreateEncounter()
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(HeroId, "side.party", new GridPosition(1, 1)),
            CreateParticipant(EnemyId, "side.enemies", new GridPosition(5, 5))
        ];

        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(HeroId, position: 1, total: 15),
            CreateInitiativeEntry(EnemyId, position: 2, total: 10)
        ];

        return EncounterRules.Start(
            "encounter.test",
            new EncounterBattlefieldState
            {
                BattlefieldId = "battlefield.test",
                Width = 12,
                Height = 12,
                BlockedPositions = Array.Empty<GridPosition>(),
                DifficultTerrainPositions = Array.Empty<GridPosition>()
            },
            participants,
            initiativeOrder);
    }

    private static EncounterParticipantSetup CreateParticipant(
        string combatantId,
        string sideId,
        GridPosition position)
    {
        return new EncounterParticipantSetup
        {
            Combatant = CombatantRules.Create(
                combatantId,
                maximumHitPoints: 10,
                CombatantZeroHitPointPolicy.DeathSavingThrows),
            CombatProfile = new EncounterCombatProfile
            {
                ArmorClass = 10
            },
            SideId = sideId,
            MovementSpeedFeet = 30,
            StartingPosition = position
        };
    }

    private static InitiativeOrderEntry CreateInitiativeEntry(
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
}

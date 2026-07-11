using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CombatTurnRulesTests
{
    [Fact]
    public void StartCombat_WithInitiativeOrder_StartsAtRoundOneAndPositionOne()
    {
        IReadOnlyList<InitiativeOrderEntry> initiativeOrder =
        [
            CreateEntry("combatant.first", position: 1),
            CreateEntry("combatant.second", position: 2)
        ];

        CombatTurnState result = CombatTurnRules.StartCombat(initiativeOrder);

        Assert.Same(initiativeOrder, result.InitiativeOrder);
        Assert.Equal(1, result.RoundNumber);
        Assert.Equal(1, result.ActivePosition);
    }

    [Fact]
    public void GetActiveCombatant_ReturnsCombatantAtActivePosition()
    {
        CombatTurnState state = new()
        {
            InitiativeOrder =
            [
                CreateEntry("combatant.first", position: 1),
                CreateEntry("combatant.second", position: 2),
                CreateEntry("combatant.third", position: 3)
            ],
            RoundNumber = 2,
            ActivePosition = 2
        };

        InitiativeOrderEntry result = CombatTurnRules.GetActiveCombatant(state);

        Assert.Equal("combatant.second", result.CombatantId);
        Assert.Equal(2, result.Position);
    }

    [Fact]
    public void AdvanceTurn_WhenActivePositionIsNotLast_MovesToNextPosition()
    {
        CombatTurnState state = new()
        {
            InitiativeOrder =
            [
                CreateEntry("combatant.first", position: 1),
                CreateEntry("combatant.second", position: 2),
                CreateEntry("combatant.third", position: 3)
            ],
            RoundNumber = 1,
            ActivePosition = 1
        };

        CombatTurnState result = CombatTurnRules.AdvanceTurn(state);

        Assert.Equal(1, result.RoundNumber);
        Assert.Equal(2, result.ActivePosition);
    }

    [Fact]
    public void AdvanceTurn_WhenActivePositionIsLast_WrapsToFirstPositionAndAdvancesRound()
    {
        CombatTurnState state = new()
        {
            InitiativeOrder =
            [
                CreateEntry("combatant.first", position: 1),
                CreateEntry("combatant.second", position: 2)
            ],
            RoundNumber = 1,
            ActivePosition = 2
        };

        CombatTurnState result = CombatTurnRules.AdvanceTurn(state);

        Assert.Equal(2, result.RoundNumber);
        Assert.Equal(1, result.ActivePosition);
    }

    [Fact]
    public void StartCombat_WithEmptyInitiativeOrder_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CombatTurnRules.StartCombat([]));
    }

    [Fact]
    public void AdvanceTurn_WithRoundNumberLessThanOne_Throws()
    {
        CombatTurnState state = new()
        {
            InitiativeOrder =
            [
                CreateEntry("combatant.first", position: 1)
            ],
            RoundNumber = 0,
            ActivePosition = 1
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CombatTurnRules.AdvanceTurn(state));
    }

    [Fact]
    public void AdvanceTurn_WithActivePositionOutsideInitiativeOrder_Throws()
    {
        CombatTurnState state = new()
        {
            InitiativeOrder =
            [
                CreateEntry("combatant.first", position: 1),
                CreateEntry("combatant.second", position: 2)
            ],
            RoundNumber = 1,
            ActivePosition = 3
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CombatTurnRules.AdvanceTurn(state));
    }

    [Fact]
    public void StartCombat_WithNonContiguousPositions_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CombatTurnRules.StartCombat(
            [
                CreateEntry("combatant.first", position: 1),
                CreateEntry("combatant.second", position: 3)
            ]));
    }

    private static InitiativeOrderEntry CreateEntry(
        string combatantId,
        int position)
    {
        return new InitiativeOrderEntry
        {
            CombatantId = combatantId,
            Initiative = new InitiativeRollResult
            {
                RollMode = D20RollMode.Normal,
                FirstRoll = 10,
                SecondRoll = null,
                NaturalRoll = 10,
                InitiativeBonus = 0,
                Total = 10
            },
            Position = position,
            HasTiedInitiative = false
        };
    }
}

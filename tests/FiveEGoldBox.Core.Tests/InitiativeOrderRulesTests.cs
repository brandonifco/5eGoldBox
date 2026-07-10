using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class InitiativeOrderRulesTests
{
    [Fact]
    public void ResolveOrder_SortsCombatantsByInitiativeTotalDescending()
    {
        IReadOnlyList<InitiativeOrderEntry> result = InitiativeOrderRules.ResolveOrder(
        [
            CreateCombatant("combatant.slow", total: 8),
            CreateCombatant("combatant.fast", total: 18),
            CreateCombatant("combatant.middle", total: 12)
        ]);

        Assert.Equal(3, result.Count);

        Assert.Equal("combatant.fast", result[0].CombatantId);
        Assert.Equal(1, result[0].Position);
        Assert.Equal(18, result[0].Initiative.Total);
        Assert.False(result[0].HasTiedInitiative);

        Assert.Equal("combatant.middle", result[1].CombatantId);
        Assert.Equal(2, result[1].Position);
        Assert.Equal(12, result[1].Initiative.Total);
        Assert.False(result[1].HasTiedInitiative);

        Assert.Equal("combatant.slow", result[2].CombatantId);
        Assert.Equal(3, result[2].Position);
        Assert.Equal(8, result[2].Initiative.Total);
        Assert.False(result[2].HasTiedInitiative);
    }

    [Fact]
    public void ResolveOrder_WithTiedInitiativeTotals_PreservesInputOrderAndMarksTies()
    {
        IReadOnlyList<InitiativeOrderEntry> result = InitiativeOrderRules.ResolveOrder(
        [
            CreateCombatant("combatant.first", total: 15),
            CreateCombatant("combatant.second", total: 15),
            CreateCombatant("combatant.third", total: 10)
        ]);

        Assert.Equal(3, result.Count);

        Assert.Equal("combatant.first", result[0].CombatantId);
        Assert.Equal(1, result[0].Position);
        Assert.Equal(15, result[0].Initiative.Total);
        Assert.True(result[0].HasTiedInitiative);

        Assert.Equal("combatant.second", result[1].CombatantId);
        Assert.Equal(2, result[1].Position);
        Assert.Equal(15, result[1].Initiative.Total);
        Assert.True(result[1].HasTiedInitiative);

        Assert.Equal("combatant.third", result[2].CombatantId);
        Assert.Equal(3, result[2].Position);
        Assert.Equal(10, result[2].Initiative.Total);
        Assert.False(result[2].HasTiedInitiative);
    }

    [Fact]
    public void ResolveOrder_WithNoCombatants_ReturnsEmptyOrder()
    {
        IReadOnlyList<InitiativeOrderEntry> result = InitiativeOrderRules.ResolveOrder([]);

        Assert.Empty(result);
    }

    [Fact]
    public void ResolveOrder_WithDuplicateCombatantId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            InitiativeOrderRules.ResolveOrder(
            [
                CreateCombatant("combatant.duplicate", total: 12),
                CreateCombatant("combatant.duplicate", total: 18)
            ]));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ResolveOrder_WithMissingCombatantId_Throws(string combatantId)
    {
        Assert.Throws<ArgumentException>(() =>
            InitiativeOrderRules.ResolveOrder(
            [
                CreateCombatant(combatantId, total: 12)
            ]));
    }

    private static InitiativeOrderCombatant CreateCombatant(
        string combatantId,
        int total)
    {
        return new InitiativeOrderCombatant
        {
            CombatantId = combatantId,
            Initiative = new InitiativeRollResult
            {
                RollMode = D20RollMode.Normal,
                FirstRoll = total,
                SecondRoll = null,
                NaturalRoll = total,
                InitiativeBonus = 0,
                Total = total
            }
        };
    }
}
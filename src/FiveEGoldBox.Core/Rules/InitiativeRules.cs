namespace FiveEGoldBox.Core.Rules;

public static class InitiativeRules
{
    public static InitiativeRollResult ResolveInitiative(
        D20RollMode rollMode,
        int firstRoll,
        int? secondRoll,
        int initiativeBonus)
    {
        int naturalRoll = D20Rules.ResolveNaturalRoll(
            rollMode,
            firstRoll,
            secondRoll);

        return new InitiativeRollResult
        {
            RollMode = rollMode,
            FirstRoll = firstRoll,
            SecondRoll = secondRoll,
            NaturalRoll = naturalRoll,
            InitiativeBonus = initiativeBonus,
            Total = naturalRoll + initiativeBonus
        };
    }
}
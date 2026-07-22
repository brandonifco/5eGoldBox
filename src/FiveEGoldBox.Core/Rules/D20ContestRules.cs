namespace FiveEGoldBox.Core.Rules;

public static class D20ContestRules
{
    public static D20ContestOutcome ResolveOutcome(
        int firstTotal,
        int secondTotal)
    {
        if (firstTotal > secondTotal)
        {
            return D20ContestOutcome.FirstWins;
        }

        if (secondTotal > firstTotal)
        {
            return D20ContestOutcome.SecondWins;
        }

        return D20ContestOutcome.Tie;
    }

    public static D20ContestResult ResolveContest(
        D20RollMode firstRollMode,
        int firstRoll,
        int? firstSecondRoll,
        int firstBonus,
        D20RollMode secondRollMode,
        int secondRoll,
        int? secondSecondRoll,
        int secondBonus)
    {
        D20Rules.ValidateRollMode(
            firstRollMode,
            nameof(firstRollMode));

        D20Rules.ValidateRollMode(
            secondRollMode,
            nameof(secondRollMode));

        D20ContestantResult firstContestant = ResolveContestant(
            firstRollMode,
            firstRoll,
            firstSecondRoll,
            firstBonus);

        D20ContestantResult secondContestant = ResolveContestant(
            secondRollMode,
            secondRoll,
            secondSecondRoll,
            secondBonus);

        return new D20ContestResult
        {
            FirstContestant = firstContestant,
            SecondContestant = secondContestant,
            Outcome = ResolveOutcome(
                firstContestant.Total,
                secondContestant.Total)
        };
    }

    private static D20ContestantResult ResolveContestant(
        D20RollMode rollMode,
        int firstRoll,
        int? secondRoll,
        int bonus)
    {
        int naturalRoll = D20Rules.ResolveNaturalRoll(
            rollMode,
            firstRoll,
            secondRoll);

        int total = D20Rules.ResolveTotal(
            naturalRoll,
            bonus);

        return new D20ContestantResult
        {
            RollMode = rollMode,
            FirstRoll = firstRoll,
            SecondRoll = secondRoll,
            NaturalRoll = naturalRoll,
            Bonus = bonus,
            Total = total
        };
    }
}

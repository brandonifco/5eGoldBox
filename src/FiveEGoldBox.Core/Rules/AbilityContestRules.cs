namespace FiveEGoldBox.Core.Rules;

public static class AbilityContestRules
{
    public static AbilityContestResult ResolveAbilityContest(
        Ability firstAbility,
        D20RollMode firstRollMode,
        int firstRoll,
        int? firstSecondRoll,
        int firstBonus,
        Ability secondAbility,
        D20RollMode secondRollMode,
        int secondRoll,
        int? secondSecondRoll,
        int secondBonus)
    {
        D20ContestResult contest = D20ContestRules.ResolveContest(
            firstRollMode,
            firstRoll,
            firstSecondRoll,
            firstBonus,
            secondRollMode,
            secondRoll,
            secondSecondRoll,
            secondBonus);

        return new AbilityContestResult
        {
            FirstContestant = new AbilityContestantResult
            {
                Ability = firstAbility,
                Contestant = contest.FirstContestant
            },
            SecondContestant = new AbilityContestantResult
            {
                Ability = secondAbility,
                Contestant = contest.SecondContestant
            },
            Outcome = contest.Outcome
        };
    }
}
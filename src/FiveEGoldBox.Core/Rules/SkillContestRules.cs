namespace FiveEGoldBox.Core.Rules;

public static class SkillContestRules
{
    public static SkillContestResult ResolveSkillContest(
        string firstSkillId,
        Ability firstAbility,
        D20RollMode firstRollMode,
        int firstRoll,
        int? firstSecondRoll,
        int firstBonus,
        string secondSkillId,
        Ability secondAbility,
        D20RollMode secondRollMode,
        int secondRoll,
        int? secondSecondRoll,
        int secondBonus)
    {
        if (string.IsNullOrWhiteSpace(firstSkillId))
        {
            throw new ArgumentException(
                "First skill ID is required.",
                nameof(firstSkillId));
        }

        if (string.IsNullOrWhiteSpace(secondSkillId))
        {
            throw new ArgumentException(
                "Second skill ID is required.",
                nameof(secondSkillId));
        }

        D20ContestResult contest = D20ContestRules.ResolveContest(
            firstRollMode,
            firstRoll,
            firstSecondRoll,
            firstBonus,
            secondRollMode,
            secondRoll,
            secondSecondRoll,
            secondBonus);

        return new SkillContestResult
        {
            FirstContestant = new SkillContestantResult
            {
                SkillId = firstSkillId,
                Ability = firstAbility,
                Contestant = contest.FirstContestant
            },
            SecondContestant = new SkillContestantResult
            {
                SkillId = secondSkillId,
                Ability = secondAbility,
                Contestant = contest.SecondContestant
            },
            Outcome = contest.Outcome
        };
    }
}
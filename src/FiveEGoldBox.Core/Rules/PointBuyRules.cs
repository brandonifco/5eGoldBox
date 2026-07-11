namespace FiveEGoldBox.Core.Rules;

public static class PointBuyRules
{
    public const int MinimumScore = 8;
    public const int MaximumScore = 15;
    public const int MaximumTotalCost = 27;

    private static readonly IReadOnlyDictionary<int, int> ScoreCosts =
        new Dictionary<int, int>
        {
            [8] = 0,
            [9] = 1,
            [10] = 2,
            [11] = 3,
            [12] = 4,
            [13] = 5,
            [14] = 7,
            [15] = 9
        };

    public static int GetCost(int score)
    {
        if (!ScoreCosts.TryGetValue(score, out int cost))
        {
            throw new ArgumentOutOfRangeException(
                nameof(score),
                score,
                $"Point-buy score must be between {MinimumScore} and {MaximumScore}.");
        }

        return cost;
    }

    public static int GetTotalCost(IReadOnlyDictionary<Ability, int> scores)
    {
        ArgumentNullException.ThrowIfNull(scores);

        int total = 0;

        foreach (Ability ability in Enum.GetValues<Ability>())
        {
            if (!scores.TryGetValue(ability, out int score))
            {
                throw new ArgumentException(
                    $"Missing score for {ability}.",
                    nameof(scores));
            }

            total += GetCost(score);
        }

        return total;
    }

    public static bool IsValid(IReadOnlyDictionary<Ability, int> scores)
    {
        ArgumentNullException.ThrowIfNull(scores);

        return GetTotalCost(scores) <= MaximumTotalCost;
    }
}

namespace FiveEGoldBox.Core.Rules;

public static class StandardArrayRules
{
    public static IReadOnlyList<int> Scores { get; } =
        new[] { 15, 14, 13, 12, 10, 8 };

    public static bool IsValid(IReadOnlyDictionary<Ability, int> scores)
    {
        ArgumentNullException.ThrowIfNull(scores);

        if (scores.Count != Scores.Count)
        {
            return false;
        }

        foreach (Ability ability in Enum.GetValues<Ability>())
        {
            if (!scores.ContainsKey(ability))
            {
                return false;
            }
        }

        List<int> actual = scores.Values.OrderBy(value => value).ToList();
        List<int> expected = Scores.OrderBy(value => value).ToList();

        return actual.SequenceEqual(expected);
    }
}
namespace FiveEGoldBox.Core.Rules;

/// How much experience a party needs to reach each level.
///
/// Real 5e XP-to-level thresholds. Authored through level 5 for headroom, even
/// though nothing yet drives a party past level 2 -- the table costs nothing
/// to have ready, and a level-up is just "which threshold does the new total
/// clear," not something worth re-deriving per slice.
public static class AdvancementRules
{
    public const int MinimumLevel = 1;
    public const int MaximumLevel = 5;

    private static readonly IReadOnlyDictionary<int, int> ExperienceThresholds =
        new Dictionary<int, int>
        {
            [1] = 0,
            [2] = 300,
            [3] = 900,
            [4] = 2_700,
            [5] = 6_500
        };

    /// The highest level whose threshold a total experience value clears.
    /// Never exceeds MaximumLevel -- there is no threshold beyond it to clear,
    /// so additional experience past that point simply has nowhere to land
    /// yet.
    public static int GetLevelForExperience(
        int experienceTotal)
    {
        if (experienceTotal < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(experienceTotal),
                experienceTotal,
                "Experience total must not be negative.");
        }

        int level = MinimumLevel;

        for (int candidate = MinimumLevel + 1;
            candidate <= MaximumLevel;
            candidate++)
        {
            if (experienceTotal < ExperienceThresholds[candidate])
            {
                break;
            }

            level = candidate;
        }

        return level;
    }
}

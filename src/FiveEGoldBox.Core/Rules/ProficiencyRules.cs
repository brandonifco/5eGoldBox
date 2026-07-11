namespace FiveEGoldBox.Core.Rules;

public static class ProficiencyRules
{
    public const int MinimumLevel = 1;
    public const int MaximumLevel = 20;

    public static int GetBonus(int level)
    {
        if (level is < MinimumLevel or > MaximumLevel)
        {
            throw new ArgumentOutOfRangeException(
                nameof(level),
                level,
                $"Character level must be between {MinimumLevel} and {MaximumLevel}.");
        }

        return 2 + ((level - 1) / 4);
    }
}

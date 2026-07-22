namespace FiveEGoldBox.Core.Rules;

public static class AbilityRules
{
    public const int MinimumScore = 1;
    public const int MaximumScore = 30;

    public static int GetModifier(int score)
    {
        if (score is < MinimumScore or > MaximumScore)
        {
            throw new ArgumentOutOfRangeException(
                nameof(score),
                score,
                $"Ability score must be between {MinimumScore} and {MaximumScore}.");
        }

        return (int)Math.Floor((score - 10) / 2.0);
    }

    internal static void ValidateAbility(
        Ability ability,
        string parameterName)
    {
        if (!Enum.IsDefined(ability))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                ability,
                "Ability is not supported.");
        }
    }
}

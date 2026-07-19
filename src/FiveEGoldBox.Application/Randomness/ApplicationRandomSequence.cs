namespace FiveEGoldBox.Application.Randomness;

internal static class ApplicationRandomSequence
{
    internal static IReadOnlyList<int> GenerateD20Rolls(
        int seed,
        int valuesConsumed,
        int rollCount,
        out int updatedValuesConsumed)
    {
        if (valuesConsumed < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valuesConsumed),
                valuesConsumed,
                "Random values consumed must not be negative.");
        }

        if (rollCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rollCount),
                rollCount,
                "Roll count must be greater than 0.");
        }

        Random random = new(seed);

        for (int index = 0;
            index < valuesConsumed;
            index++)
        {
            random.Next(1, 21);
        }

        int[] rolls = new int[rollCount];

        for (int index = 0;
            index < rollCount;
            index++)
        {
            rolls[index] = random.Next(1, 21);
        }

        updatedValuesConsumed = checked(
            valuesConsumed + rollCount);

        return Array.AsReadOnly(rolls);
    }
}

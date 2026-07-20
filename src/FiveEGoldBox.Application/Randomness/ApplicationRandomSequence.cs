using System.Buffers.Binary;
using System.Security.Cryptography;

namespace FiveEGoldBox.Application.Randomness;

internal static class ApplicationRandomSequence
{
    private const int HashInputLength = 16;

    private const int HashOutputLength = 32;

    internal static ApplicationRandomRoll GenerateDie(
        int seed,
        int valuesConsumed,
        int sides)
    {
        if (valuesConsumed < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valuesConsumed),
                valuesConsumed,
                "Random values consumed must not be negative.");
        }

        if (sides is not (6 or 8 or 12 or 20))
        {
            throw new ArgumentOutOfRangeException(
                nameof(sides),
                sides,
                "Only d6, d8, d12, and d20 are supported by the bounded random sequence.");
        }

        int updatedValuesConsumed = checked(
            valuesConsumed + 1);

        int attempt = 0;
        ulong modulus = (ulong)sides;
        ulong rejectionThreshold =
            unchecked(0UL - modulus) % modulus;

        Span<byte> input =
            stackalloc byte[HashInputLength];

        Span<byte> hash =
            stackalloc byte[HashOutputLength];

        while (true)
        {
            BinaryPrimitives.WriteInt32LittleEndian(
                input,
                seed);
            BinaryPrimitives.WriteInt32LittleEndian(
                input[4..],
                valuesConsumed);
            BinaryPrimitives.WriteInt32LittleEndian(
                input[8..],
                sides);
            BinaryPrimitives.WriteInt32LittleEndian(
                input[12..],
                attempt);

            SHA256.HashData(input, hash);

            ulong sample =
                BinaryPrimitives.ReadUInt64LittleEndian(
                    hash);

            if (sample >= rejectionThreshold)
            {
                return new ApplicationRandomRoll(
                    Ordinal: updatedValuesConsumed,
                    Sides: sides,
                    Value: checked(
                        (int)(sample % modulus) + 1),
                    UpdatedValuesConsumed:
                        updatedValuesConsumed);
            }

            attempt = checked(attempt + 1);
        }
    }

    internal static IReadOnlyList<int> GenerateD20Rolls(
        int seed,
        int valuesConsumed,
        int rollCount,
        out int updatedValuesConsumed)
    {
        if (rollCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rollCount),
                rollCount,
                "Roll count must be greater than 0.");
        }

        int[] rolls = new int[rollCount];
        int cursor = valuesConsumed;

        for (int index = 0;
            index < rollCount;
            index++)
        {
            ApplicationRandomRoll roll = GenerateDie(
                seed,
                cursor,
                sides: 20);

            rolls[index] = roll.Value;
            cursor = roll.UpdatedValuesConsumed;
        }

        updatedValuesConsumed = cursor;

        return Array.AsReadOnly(rolls);
    }
}

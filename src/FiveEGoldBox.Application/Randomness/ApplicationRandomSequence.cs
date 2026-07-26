using System.Buffers.Binary;
using System.Security.Cryptography;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Application.Randomness;

internal static class ApplicationRandomSequence
{
    private const int HashInputLength = 16;

    private const int HashOutputLength = 32;

    /// Every die Core defines can be rolled here.
    ///
    /// The die is named by its type rather than a side count so that the set
    /// this understands cannot drift from the set Core accepts: there is one
    /// list, and it is DieType. A die Core gains later will not silently
    /// become rollable — it has no case here until someone gives it one — but
    /// nor can a die Core already has be quietly missing.
    internal static ApplicationRandomRoll GenerateDie(
        int seed,
        int valuesConsumed,
        DieType die)
    {
        if (valuesConsumed < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valuesConsumed),
                valuesConsumed,
                "Random values consumed must not be negative.");
        }

        if (!IsSupported(die))
        {
            throw new ArgumentOutOfRangeException(
                nameof(die),
                die,
                "The die is not one the bounded random sequence can roll.");
        }

        // The side count, not the enum member, goes into the hash. DieType's
        // values are the side counts, so sequences generated before the
        // parameter became a die type are reproduced exactly.
        int sides = (int)die;
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

    /// Whether this die can be rolled. Content validation asks before play
    /// begins, so an unrollable die is refused at load rather than partway
    /// through a fight.
    internal static bool IsSupported(
        DieType die)
    {
        return die switch
        {
            DieType.D4
                or DieType.D6
                or DieType.D8
                or DieType.D10
                or DieType.D12
                or DieType.D20 => true,
            _ => false
        };
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
                DieType.D20);

            rolls[index] = roll.Value;
            cursor = roll.UpdatedValuesConsumed;
        }

        updatedValuesConsumed = cursor;

        return Array.AsReadOnly(rolls);
    }
}

namespace FiveEGoldBox.Core.Internal;

internal static class CoreEnumValidation
{
    internal static void RequireDefined<TEnum>(
        TEnum value,
        string paramName,
        string? message = null)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                message ?? $"{typeof(TEnum).Name} value is not supported.");
        }
    }
}

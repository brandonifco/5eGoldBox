using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Characters;

public sealed record CharacterSense
{
    public required SenseType Type { get; init; }

    public required int RangeFeet { get; init; }
}
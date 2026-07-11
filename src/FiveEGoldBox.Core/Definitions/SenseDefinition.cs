using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Definitions;

public sealed record SenseDefinition
{
    public required SenseType Type { get; init; }

    public required int RangeFeet { get; init; }
}

using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Definitions;

public sealed record DamageDice
{
    public required int Count { get; init; }

    public required DieType Die { get; init; }
}

using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Core.Rules;

public sealed record DamageRollResult
{
    public required DamageDice DamageDice { get; init; }

    public required IReadOnlyList<int> Rolls { get; init; }

    public required int DiceTotal { get; init; }

    public required int DamageBonus { get; init; }

    public required int Total { get; init; }
}
namespace FiveEGoldBox.Core.Rules;

public sealed record AttackRollResult
{
    public required D20RollMode RollMode { get; init; }

    public required int FirstRoll { get; init; }

    public int? SecondRoll { get; init; }

    public required int NaturalRoll { get; init; }

    public required int AttackBonus { get; init; }

    public required int Total { get; init; }

    public required int TargetArmorClass { get; init; }

    public required AttackRollOutcome Outcome { get; init; }
}
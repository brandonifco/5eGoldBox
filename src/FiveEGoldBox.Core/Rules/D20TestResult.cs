namespace FiveEGoldBox.Core.Rules;

public sealed record D20TestResult
{
    public required D20RollMode RollMode { get; init; }

    public required int FirstRoll { get; init; }

    public int? SecondRoll { get; init; }

    public required int NaturalRoll { get; init; }

    public required int Bonus { get; init; }

    public required int Total { get; init; }

    public required int DifficultyClass { get; init; }

    public required D20TestOutcome Outcome { get; init; }
}
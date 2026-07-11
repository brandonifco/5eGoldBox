namespace FiveEGoldBox.Core.Rules;

public sealed record D20ContestantResult
{
    public required D20RollMode RollMode { get; init; }

    public required int FirstRoll { get; init; }

    public int? SecondRoll { get; init; }

    public required int NaturalRoll { get; init; }

    public required int Bonus { get; init; }

    public required int Total { get; init; }
}

namespace FiveEGoldBox.Core.Rules;

public sealed record DeathSavingThrowResult
{
    public required D20RollMode RollMode { get; init; }

    public required int FirstRoll { get; init; }

    public int? SecondRoll { get; init; }

    public required int NaturalRoll { get; init; }

    public required int SavingThrowBonus { get; init; }

    public required int Total { get; init; }

    public required int DifficultyClass { get; init; }

    public required DeathSavingThrowOutcome Outcome { get; init; }

    public required DeathSavingThrowState State { get; init; }
}

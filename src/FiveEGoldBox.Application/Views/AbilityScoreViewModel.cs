namespace FiveEGoldBox.Application.Views;

/// One ability score, for a client to draw a Character screen from.
public sealed record AbilityScoreViewModel
{
    public required string AbilityName { get; init; }

    public required int Score { get; init; }

    public required int Modifier { get; init; }
}

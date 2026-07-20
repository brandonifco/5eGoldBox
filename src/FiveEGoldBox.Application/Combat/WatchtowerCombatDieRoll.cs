namespace FiveEGoldBox.Application.Combat;

public sealed record WatchtowerCombatDieRoll
{
    public required int Ordinal { get; init; }

    public required int Sides { get; init; }

    public required int Value { get; init; }

    public required WatchtowerCombatDiePurpose Purpose { get; init; }
}

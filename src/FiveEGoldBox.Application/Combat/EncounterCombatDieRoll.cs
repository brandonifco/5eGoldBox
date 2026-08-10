namespace FiveEGoldBox.Application.Combat;

internal sealed record EncounterCombatDieRoll
{
    public required int Ordinal { get; init; }

    public required int Sides { get; init; }

    public required int Value { get; init; }

    public required CombatDiePurpose Purpose { get; init; }
}

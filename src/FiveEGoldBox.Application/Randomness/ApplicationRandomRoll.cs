namespace FiveEGoldBox.Application.Randomness;

internal readonly record struct ApplicationRandomRoll(
    int Ordinal,
    int Sides,
    int Value,
    int UpdatedValuesConsumed);

namespace FiveEGoldBox.Core.Characters;

public sealed record CurrencyAmount
{
    public int CopperPieces { get; init; }

    public int SilverPieces { get; init; }

    public int ElectrumPieces { get; init; }

    public int GoldPieces { get; init; }

    public int PlatinumPieces { get; init; }
}
namespace FiveEGoldBox.Application.Views;

/// The party's shared purse, denomination by denomination.
public sealed record CurrencyViewModel
{
    public required int CopperPieces { get; init; }

    public required int SilverPieces { get; init; }

    public required int ElectrumPieces { get; init; }

    public required int GoldPieces { get; init; }

    public required int PlatinumPieces { get; init; }
}

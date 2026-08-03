namespace FiveEGoldBox.Application.Persistence.V1;

/// Added after the format was first written. A document from before it
/// omits the property entirely and loads with every denomination at zero,
/// which is exactly right for a party that never carried any currency.
internal sealed record SaveCurrencyV1
{
    public int CopperPieces { get; init; }

    public int SilverPieces { get; init; }

    public int ElectrumPieces { get; init; }

    public int GoldPieces { get; init; }

    public int PlatinumPieces { get; init; }
}

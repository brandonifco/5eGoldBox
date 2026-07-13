namespace FiveEGoldBox.Core.Characters;

public sealed partial class CharacterResolver
{
    private static bool HasNegativeCurrencyAmount(CurrencyAmount currency)
    {
        return currency.CopperPieces < 0
            || currency.SilverPieces < 0
            || currency.ElectrumPieces < 0
            || currency.GoldPieces < 0
            || currency.PlatinumPieces < 0;
    }

    private static decimal CalculateCurrencyWeight(CurrencyAmount currency)
    {
        int totalCoins = currency.CopperPieces
            + currency.SilverPieces
            + currency.ElectrumPieces
            + currency.GoldPieces
            + currency.PlatinumPieces;

        return totalCoins / 50m;
    }

    private static int CalculateCurrencyValueInCopperPieces(CurrencyAmount currency)
    {
        return currency.CopperPieces
            + (currency.SilverPieces * 10)
            + (currency.ElectrumPieces * 50)
            + (currency.GoldPieces * 100)
            + (currency.PlatinumPieces * 1000);
    }
}

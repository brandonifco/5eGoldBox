namespace FiveEGoldBox.Application.Content.V1;

internal sealed record ShopItemDefinitionV1
{
    public required string ItemId { get; init; }

    public required int PriceGoldPieces { get; init; }
}

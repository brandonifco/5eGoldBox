namespace FiveEGoldBox.Application.Persistence.V1;

internal sealed record SavePartyInventoryItemV1
{
    public required string ItemId { get; init; }

    public required int Quantity { get; init; }
}

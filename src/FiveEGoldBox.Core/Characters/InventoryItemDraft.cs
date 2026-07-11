namespace FiveEGoldBox.Core.Characters;

public sealed record InventoryItemDraft
{
    public required string ItemId { get; init; }

    public required int Quantity { get; init; }
}

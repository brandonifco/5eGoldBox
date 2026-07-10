namespace FiveEGoldBox.Core.Characters;

public sealed record InventoryItemSnapshot
{
    public required string ItemId { get; init; }

    public required string ItemName { get; init; }

    public required int Quantity { get; init; }

    public required decimal UnitWeightPounds { get; init; }

    public required decimal TotalWeightPounds { get; init; }

    public IReadOnlyList<string> Tags { get; init; }
        = Array.Empty<string>();
}
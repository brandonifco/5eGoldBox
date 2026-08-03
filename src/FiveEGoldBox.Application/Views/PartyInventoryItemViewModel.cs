namespace FiveEGoldBox.Application.Views;

/// One stack of a shared party inventory item, named the way the ruleset
/// names it rather than by its raw content ID.
public sealed record PartyInventoryItemViewModel
{
    public required string ItemId { get; init; }

    public required string DisplayName { get; init; }

    public required int Quantity { get; init; }
}

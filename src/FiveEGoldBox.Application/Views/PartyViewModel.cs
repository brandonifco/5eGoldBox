namespace FiveEGoldBox.Application.Views;

/// The party's currency and shared inventory, for a client to draw an
/// Inventory/Character screen from.
public sealed record PartyViewModel
{
    public required CurrencyViewModel Currency { get; init; }

    public required IReadOnlyList<PartyInventoryItemViewModel> InventoryItems { get; init; }
}

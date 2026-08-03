namespace FiveEGoldBox.Application.Views;

/// The party's roster, currency, and shared inventory, for a client to draw
/// a Character/Inventory screen from.
public sealed record PartyViewModel
{
    public required IReadOnlyList<PartyMemberViewModel> Members { get; init; }

    public required CurrencyViewModel Currency { get; init; }

    public required IReadOnlyList<PartyInventoryItemViewModel> InventoryItems { get; init; }
}

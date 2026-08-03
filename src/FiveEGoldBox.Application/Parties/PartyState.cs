using FiveEGoldBox.Core.Characters;

namespace FiveEGoldBox.Application.Parties;

public sealed record PartyState
{
    public required string PartyId { get; init; }

    public required IReadOnlyList<PartyMemberState> Members { get; init; }

    /// A shared party purse, not per-character -- a deliberate
    /// simplification; individual PartyMemberState entries do not carry
    /// their own currency.
    public CurrencyAmount Currency { get; init; } = new();

    public IReadOnlyList<PartyInventoryItemState> InventoryItems { get; init; }
        = Array.Empty<PartyInventoryItemState>();
}

namespace FiveEGoldBox.Application.Parties;

/// One stack of a shared-party-purse inventory item. Distinct from
/// Core.Characters.InventoryItemDraft, which is a build-time draft/
/// validation type for starting gear, not runtime session state.
///
/// Public, matching PartyState's own visibility -- a public record cannot
/// expose a less-accessible member type, and PartyState (which this hangs
/// off of via PartyState.InventoryItems) is already public.
public sealed record PartyInventoryItemState
{
    public required string ItemId { get; init; }

    public required int Quantity { get; init; }
}

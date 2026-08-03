namespace FiveEGoldBox.Application.Parties;

/// Adding an item to the party's shared inventory -- shared by treasure
/// collection and shop purchases, the two ways an item enters play.
internal static class PartyInventoryRules
{
    /// Increments an existing stack of itemId if one exists, or appends a
    /// new one.
    internal static IReadOnlyList<PartyInventoryItemState> AddItem(
        IReadOnlyList<PartyInventoryItemState> items,
        string itemId,
        int quantity)
    {
        bool foundExistingStack = false;
        List<PartyInventoryItemState> updatedItems = new(items.Count + 1);

        foreach (PartyInventoryItemState item in items)
        {
            if (!foundExistingStack
                && string.Equals(
                    item.ItemId,
                    itemId,
                    StringComparison.Ordinal))
            {
                foundExistingStack = true;
                updatedItems.Add(item with
                {
                    Quantity = item.Quantity + quantity
                });
            }
            else
            {
                updatedItems.Add(item);
            }
        }

        if (!foundExistingStack)
        {
            updatedItems.Add(new PartyInventoryItemState
            {
                ItemId = itemId,
                Quantity = quantity
            });
        }

        return updatedItems;
    }
}

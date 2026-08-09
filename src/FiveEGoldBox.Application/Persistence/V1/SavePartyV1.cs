namespace FiveEGoldBox.Application.Persistence.V1;

internal sealed record SavePartyV1
{
    public required string PartyId { get; init; }

    public required IReadOnlyList<SavePartyMemberV1> Members { get; init; }

    /// Added after the format was first written. A document from before it
    /// omits the property entirely and loads with a new(), all-zero
    /// SaveCurrencyV1, exactly right for a party that never carried any.
    public SaveCurrencyV1 Currency { get; init; } = new();

    /// Added after the format was first written. A document from before it
    /// omits the property entirely and loads with no inventory items, which
    /// is exactly right for a party that had none.
    public IReadOnlyList<SavePartyInventoryItemV1> InventoryItems { get; init; }
        = Array.Empty<SavePartyInventoryItemV1>();

    /// Added after the format was first written. A document from before it
    /// omits both properties entirely and loads at 0 XP, level 1 -- exactly
    /// right for a party that predates advancement existing at all.
    public int ExperienceTotal { get; init; }

    public int Level { get; init; } = 1;
}

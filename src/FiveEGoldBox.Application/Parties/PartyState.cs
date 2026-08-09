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

    /// Same simplification as Currency, applied to advancement: the whole
    /// party fights every encounter together with nobody benched, so a
    /// shared total and level produce the same practical outcome as
    /// per-character tracking without four independent level-up moments
    /// potentially drifting out of sync.
    public int ExperienceTotal { get; init; }

    public int Level { get; init; } = 1;
}

namespace FiveEGoldBox.Application.Parties;

public sealed record PartyState
{
    public required string PartyId { get; init; }

    public required IReadOnlyList<PartyMemberState> Members { get; init; }
}

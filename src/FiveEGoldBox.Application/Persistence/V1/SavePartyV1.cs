namespace FiveEGoldBox.Application.Persistence.V1;

internal sealed record SavePartyV1
{
    public required string PartyId { get; init; }

    public required IReadOnlyList<SavePartyMemberV1> Members { get; init; }
}

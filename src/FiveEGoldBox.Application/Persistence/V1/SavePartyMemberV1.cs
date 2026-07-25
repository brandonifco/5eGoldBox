namespace FiveEGoldBox.Application.Persistence.V1;

internal sealed record SavePartyMemberV1
{
    public required string PartyMemberId { get; init; }

    public required string CharacterDefinitionId { get; init; }

    public required string DisplayName { get; init; }

    public required string ClassId { get; init; }

    public required SaveZeroHitPointPolicyV1 ZeroHitPointPolicy { get; init; }

    public required SaveHealthV1 Health { get; init; }

    public SaveAmmunitionV1? Ammunition { get; init; }
}

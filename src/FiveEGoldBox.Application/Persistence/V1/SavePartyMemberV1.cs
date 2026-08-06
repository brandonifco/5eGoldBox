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

    /// Added after the format was first written. A document from before it
    /// omits the property entirely and loads with no resources, which is
    /// exactly right for a party that had none.
    public IReadOnlyList<SaveCharacterResourceV1> Resources { get; init; }
        = Array.Empty<SaveCharacterResourceV1>();

    /// Added after the format was first written. A document from before it
    /// omits the property entirely and loads with null, exactly right for
    /// every roster character -- CustomBuild only exists at all for a
    /// character CharacterCreationRules created.
    public SaveCharacterDraftV1? CustomBuild { get; init; }
}

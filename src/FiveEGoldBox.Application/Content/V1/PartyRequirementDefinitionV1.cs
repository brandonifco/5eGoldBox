namespace FiveEGoldBox.Application.Content.V1;

internal sealed record PartyRequirementDefinitionV1
{
    public required int MinimumMembers { get; init; }

    public required int MaximumMembers { get; init; }

    public required int MinimumConsciousMembers { get; init; }
}

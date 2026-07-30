namespace FiveEGoldBox.Application.Content.V1;

internal sealed record EffectDefinitionV1
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public IReadOnlyList<RollContributionDefinitionV1> Contributions { get; init; }
        = Array.Empty<RollContributionDefinitionV1>();
}

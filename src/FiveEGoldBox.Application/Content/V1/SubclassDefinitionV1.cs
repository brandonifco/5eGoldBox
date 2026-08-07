namespace FiveEGoldBox.Application.Content.V1;

internal sealed record SubclassDefinitionV1
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }
}

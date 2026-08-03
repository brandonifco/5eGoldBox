namespace FiveEGoldBox.Application.Content.V1;

internal sealed record NpcDefinitionV1
{
    public required string NpcId { get; init; }

    public required GridPositionV1 Position { get; init; }

    public required string Name { get; init; }

    public required string DialogueText { get; init; }
}

namespace FiveEGoldBox.Application.Content.V1;

internal sealed record DoorDefinitionV1
{
    public required string DoorId { get; init; }

    public required GridPositionV1 Position { get; init; }

    public required ExplorationFacingV1 Side { get; init; }

    public required bool IsSecret { get; init; }

    public required bool IsLocked { get; init; }
}

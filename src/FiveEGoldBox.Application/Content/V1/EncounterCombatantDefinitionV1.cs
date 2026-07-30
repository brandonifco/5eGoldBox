namespace FiveEGoldBox.Application.Content.V1;

internal sealed record EncounterCombatantDefinitionV1
{
    public required string CombatantId { get; init; }

    public required string MonsterId { get; init; }

    public required string SideId { get; init; }

    public required GridPositionV1 StartingPosition { get; init; }
}

namespace FiveEGoldBox.Application.Content.V1;

internal sealed record TreasureDefinitionV1
{
    public required string TreasureId { get; init; }

    public required GridPositionV1 Position { get; init; }

    public string? ItemId { get; init; }

    public int? GoldPieces { get; init; }
}

namespace FiveEGoldBox.Core.Definitions;

public sealed record EquipmentItemDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public decimal WeightPounds { get; init; }

    public int? CostInCopperPieces { get; init; }

    public IReadOnlyList<string> Tags { get; init; }
        = Array.Empty<string>();
}
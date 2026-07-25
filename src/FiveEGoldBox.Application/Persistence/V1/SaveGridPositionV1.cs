namespace FiveEGoldBox.Application.Persistence.V1;

internal sealed record SaveGridPositionV1
{
    public required int X { get; init; }

    public required int Y { get; init; }
}

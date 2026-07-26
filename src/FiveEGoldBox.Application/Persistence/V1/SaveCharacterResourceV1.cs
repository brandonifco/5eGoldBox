namespace FiveEGoldBox.Application.Persistence.V1;

internal sealed record SaveCharacterResourceV1
{
    public required string ResourceId { get; init; }

    public required int Remaining { get; init; }

    public required int Maximum { get; init; }
}

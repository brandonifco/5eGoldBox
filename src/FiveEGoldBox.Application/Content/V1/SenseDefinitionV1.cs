namespace FiveEGoldBox.Application.Content.V1;

internal sealed record SenseDefinitionV1
{
    public required SenseTypeV1 Type { get; init; }

    public required int RangeFeet { get; init; }
}

namespace FiveEGoldBox.Application.Content.V1;

internal sealed record DamageResponseDefinitionV1
{
    public required string DamageType { get; init; }

    public required DamageResponseTypeV1 ResponseType { get; init; }
}

namespace FiveEGoldBox.Application.Content.V1;

internal sealed record DamageDiceV1
{
    public required int Count { get; init; }

    public required DieTypeV1 Die { get; init; }
}

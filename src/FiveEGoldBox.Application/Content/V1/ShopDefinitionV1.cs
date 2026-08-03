namespace FiveEGoldBox.Application.Content.V1;

internal sealed record ShopDefinitionV1
{
    public required string ShopId { get; init; }

    public required string DisplayName { get; init; }

    public required string LocationId { get; init; }

    public required IReadOnlyList<ShopItemDefinitionV1> Items { get; init; }
}

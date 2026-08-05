using FiveEGoldBox.Application.Content.V1;

namespace FiveEGoldBox.ContentEditor.Models;

/// A mutable, two-way-bindable stand-in for ShopDefinitionV1 -- see
/// ScenarioLocationFormModel's header comment for why this is internal, not
/// public, unlike the ruleset form models.
internal sealed class ShopFormModel
{
    public string ShopId { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string LocationId { get; set; } = "";

    public List<ShopItemFormModel> Items { get; set; } = [];

    public static ShopFormModel FromDefinition(
        ShopDefinitionV1 shop)
    {
        return new ShopFormModel
        {
            ShopId = shop.ShopId,
            DisplayName = shop.DisplayName,
            LocationId = shop.LocationId,
            Items = shop.Items.Select(ShopItemFormModel.FromDefinition).ToList()
        };
    }

    public ShopDefinitionV1 ToDefinition()
    {
        return new ShopDefinitionV1
        {
            ShopId = ShopId,
            DisplayName = DisplayName,
            LocationId = LocationId,
            Items = Items.Select(row => row.ToDefinition()).ToList()
        };
    }
}

internal sealed class ShopItemFormModel
{
    public string ItemId { get; set; } = "";

    public int PriceGoldPieces { get; set; }

    public static ShopItemFormModel FromDefinition(
        ShopItemDefinitionV1 item)
    {
        return new ShopItemFormModel
        {
            ItemId = item.ItemId,
            PriceGoldPieces = item.PriceGoldPieces
        };
    }

    public ShopItemDefinitionV1 ToDefinition()
    {
        return new ShopItemDefinitionV1
        {
            ItemId = ItemId,
            PriceGoldPieces = PriceGoldPieces
        };
    }
}

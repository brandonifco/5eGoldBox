using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

internal static partial class ScenarioDefinitionValidator
{
    private static void AddShopIssues(
        ScenarioDefinition definition,
        ValidatedRuleset? ruleset,
        List<ValidationIssue> issues)
    {
        AddDuplicateIdIssues(
            issues,
            definition.Shops.Select(shop => shop.ShopId),
            "scenario.shops.duplicate_id",
            "shop ID");

        HashSet<string> locations = ToSet(
            definition.Locations.Select(location => location.LocationId));

        foreach (ShopDefinition shop in definition.Shops)
        {
            string subject = $"Shop '{shop.ShopId}'";

            AddIfBlank(
                issues,
                shop.ShopId,
                "scenario.shops.id_required",
                "Shop IDs must not be blank.");

            AddIfBlank(
                issues,
                shop.DisplayName,
                "scenario.shops.display_name_required",
                $"{subject} must have a display name.");

            if (!locations.Contains(shop.LocationId))
            {
                issues.Add(Error(
                    "scenario.shops.location_unknown",
                    $"{subject} is offered at undeclared location '{shop.LocationId}'."));
            }

            if (shop.Items.Count == 0)
            {
                issues.Add(Error(
                    "scenario.shops.no_items",
                    $"{subject} sells nothing."));
            }

            AddDuplicateIdIssues(
                issues,
                shop.Items.Select(item => item.ItemId),
                "scenario.shops.duplicate_item",
                $"item on {subject}");

            foreach (ShopItemDefinition item in shop.Items)
            {
                AddIfBlank(
                    issues,
                    item.ItemId,
                    "scenario.shops.item_id_required",
                    $"An item on {subject} has a blank item ID.");

                if (item.PriceGoldPieces <= 0)
                {
                    issues.Add(Error(
                        "scenario.shops.item_price_not_positive",
                        $"{subject} prices '{item.ItemId}' at a non-positive amount."));
                }

                if (ruleset is not null
                    && !string.IsNullOrWhiteSpace(item.ItemId)
                    && !ruleset.Definition.EquipmentItems.Any(equipmentItem =>
                        string.Equals(
                            equipmentItem.Id,
                            item.ItemId,
                            StringComparison.Ordinal)))
                {
                    issues.Add(Error(
                        "scenario.shops.item_id_unresolved",
                        $"{subject} sells '{item.ItemId}', which the scenario's ruleset does not define."));
                }
            }
        }
    }
}

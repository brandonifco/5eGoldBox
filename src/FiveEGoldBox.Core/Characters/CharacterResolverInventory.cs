using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Core.Characters;

public sealed partial class CharacterResolver
{
    private static bool HasNegativeCurrencyAmount(CurrencyAmount currency)
    {
        return currency.CopperPieces < 0
            || currency.SilverPieces < 0
            || currency.ElectrumPieces < 0
            || currency.GoldPieces < 0
            || currency.PlatinumPieces < 0;
    }

    private static decimal CalculateEquippedWeight(
        ArmorDefinition? equippedArmor,
        ArmorDefinition? equippedShield,
        IReadOnlyList<WeaponDefinition> equippedWeapons)
    {
        decimal totalWeight = 0m;

        if (equippedArmor is not null)
        {
            totalWeight += equippedArmor.WeightPounds;
        }

        if (equippedShield is not null)
        {
            totalWeight += equippedShield.WeightPounds;
        }

        totalWeight += equippedWeapons.Sum(weapon => weapon.WeightPounds);

        return totalWeight;
    }

    private IReadOnlyList<InventoryItemSnapshot> ResolveInventoryItems(CharacterDraft draft)
    {
        if (_rulesetIndex is null || _rulesetIndex.EquipmentItemsById.Count == 0)
        {
            return Array.Empty<InventoryItemSnapshot>();
        }

        return draft.InventoryItems
            .Select(inventoryItem =>
            {
                EquipmentItemDefinition? definition = _rulesetIndex.EquipmentItemsById
                    .GetValueOrDefault(inventoryItem.ItemId);

                if (definition is null)
                {
                    return null;
                }

                return new InventoryItemSnapshot
                {
                    ItemId = definition.Id,
                    ItemName = definition.Name,
                    Quantity = inventoryItem.Quantity,
                    UnitWeightPounds = definition.WeightPounds,
                    TotalWeightPounds = definition.WeightPounds * inventoryItem.Quantity,
                    UnitValueInCopperPieces = definition.CostInCopperPieces,
                    TotalValueInCopperPieces = definition.CostInCopperPieces is null
                        ? null
                        : definition.CostInCopperPieces * inventoryItem.Quantity,
                    Tags = definition.Tags
                };
            })
            .Where(item => item is not null)
            .Cast<InventoryItemSnapshot>()
            .ToArray();
    }

    private decimal CalculateInventoryWeight(CharacterDraft draft)
    {
        if (_rulesetIndex is null || _rulesetIndex.EquipmentItemsById.Count == 0)
        {
            return 0m;
        }

        return draft.InventoryItems
            .Where(inventoryItem => inventoryItem.Quantity > 0)
            .Select(inventoryItem =>
            {
                EquipmentItemDefinition? definition = _rulesetIndex.EquipmentItemsById
                    .GetValueOrDefault(inventoryItem.ItemId);

                return definition is null
                    ? 0m
                    : definition.WeightPounds * inventoryItem.Quantity;
            })
            .Sum();
    }

    private static decimal CalculateCurrencyWeight(CurrencyAmount currency)
    {
        int totalCoins = currency.CopperPieces
            + currency.SilverPieces
            + currency.ElectrumPieces
            + currency.GoldPieces
            + currency.PlatinumPieces;

        return totalCoins / 50m;
    }

    private static int CalculateCurrencyValueInCopperPieces(CurrencyAmount currency)
    {
        return currency.CopperPieces
            + (currency.SilverPieces * 10)
            + (currency.ElectrumPieces * 50)
            + (currency.GoldPieces * 100)
            + (currency.PlatinumPieces * 1000);
    }
}

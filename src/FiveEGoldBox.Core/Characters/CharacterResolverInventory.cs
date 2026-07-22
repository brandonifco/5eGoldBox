using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Internal;

namespace FiveEGoldBox.Core.Characters;

public sealed partial class CharacterResolver
{

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
            return CoreCollectionProtection.ProtectList(
                Array.Empty<InventoryItemSnapshot>());
        }

        IEnumerable<InventoryItemSnapshot> items = draft.InventoryItems
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
                    Tags = CoreCollectionProtection.ProtectList(definition.Tags)
                };
            })
            .Where(item => item is not null)
            .Cast<InventoryItemSnapshot>();

        return CoreCollectionProtection.ProtectList(items);
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
}

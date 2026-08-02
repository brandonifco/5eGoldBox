using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.ContentEditor.Models;

/// A mutable, two-way-bindable stand-in for EquipmentItemDefinition -- see
/// WeaponFormModel's header comment for why this exists.
public sealed class EquipmentItemFormModel
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public decimal WeightPounds { get; set; }

    public int? CostInCopperPieces { get; set; }

    /// Tags are free text (no fixed set exists, unlike weapon properties),
    /// edited as a comma-separated list.
    public string TagsText { get; set; } = "";

    public static EquipmentItemFormModel FromDefinition(
        EquipmentItemDefinition item)
    {
        return new EquipmentItemFormModel
        {
            Id = item.Id,
            Name = item.Name,
            WeightPounds = item.WeightPounds,
            CostInCopperPieces = item.CostInCopperPieces,
            TagsText = string.Join(", ", item.Tags)
        };
    }

    public EquipmentItemDefinition ToDefinition()
    {
        return new EquipmentItemDefinition
        {
            Id = Id,
            Name = Name,
            WeightPounds = WeightPounds,
            CostInCopperPieces = CostInCopperPieces,
            Tags = TagsText
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList()
        };
    }
}

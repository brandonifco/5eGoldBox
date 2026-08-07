using FiveEGoldBox.Application.Content.V1;

namespace FiveEGoldBox.ContentEditor.Models;

/// Mutable, bindable projections of the four per-floor map features. The V1
/// DTOs are records with init-only properties, which Blazor's @bind can't
/// write to, so the grid editor edits these and maps back on save.
///
/// The nullable fields here carry a real corruption risk and are handled
/// deliberately: a treasure with no ItemId must render with the property
/// *omitted*, not as "ItemId": "". An empty string from an untouched text box
/// therefore normalizes back to null rather than round-tripping as "".
internal sealed class StairFormModel
{
    public int X { get; set; }

    public int Y { get; set; }

    public string DestinationFloor { get; set; } = "";

    public int DestinationX { get; set; }

    public int DestinationY { get; set; }

    public static StairFormModel FromDefinition(
        StairDefinitionV1 stair)
    {
        return new StairFormModel
        {
            X = stair.Position.X,
            Y = stair.Position.Y,
            DestinationFloor = stair.DestinationFloor,
            DestinationX = stair.DestinationPosition.X,
            DestinationY = stair.DestinationPosition.Y
        };
    }

    public StairDefinitionV1 ToDefinition()
    {
        return new StairDefinitionV1
        {
            Position = new GridPositionV1 { X = X, Y = Y },
            DestinationFloor = DestinationFloor,
            DestinationPosition = new GridPositionV1 { X = DestinationX, Y = DestinationY }
        };
    }
}

internal sealed class DoorFormModel
{
    public string DoorId { get; set; } = "";

    public int X { get; set; }

    public int Y { get; set; }

    public ExplorationFacingV1 Side { get; set; }

    public bool IsSecret { get; set; }

    public bool IsLocked { get; set; }

    public static DoorFormModel FromDefinition(
        DoorDefinitionV1 door)
    {
        return new DoorFormModel
        {
            DoorId = door.DoorId,
            X = door.Position.X,
            Y = door.Position.Y,
            Side = door.Side,
            IsSecret = door.IsSecret,
            IsLocked = door.IsLocked
        };
    }

    public DoorDefinitionV1 ToDefinition()
    {
        return new DoorDefinitionV1
        {
            DoorId = DoorId,
            Position = new GridPositionV1 { X = X, Y = Y },
            Side = Side,
            IsSecret = IsSecret,
            IsLocked = IsLocked
        };
    }
}

internal sealed class TreasureFormModel
{
    public string TreasureId { get; set; } = "";

    public int X { get; set; }

    public int Y { get; set; }

    public string? ItemId { get; set; }

    public int? GoldPieces { get; set; }

    public int? Quantity { get; set; }

    public static TreasureFormModel FromDefinition(
        TreasureDefinitionV1 treasure)
    {
        return new TreasureFormModel
        {
            TreasureId = treasure.TreasureId,
            X = treasure.Position.X,
            Y = treasure.Position.Y,
            ItemId = treasure.ItemId,
            GoldPieces = treasure.GoldPieces,
            Quantity = treasure.Quantity
        };
    }

    public TreasureDefinitionV1 ToDefinition()
    {
        return new TreasureDefinitionV1
        {
            TreasureId = TreasureId,
            Position = new GridPositionV1 { X = X, Y = Y },
            ItemId = string.IsNullOrWhiteSpace(ItemId) ? null : ItemId,
            GoldPieces = GoldPieces,
            Quantity = Quantity
        };
    }
}

internal sealed class NpcFormModel
{
    public string NpcId { get; set; } = "";

    public int X { get; set; }

    public int Y { get; set; }

    public string Name { get; set; } = "";

    public string DialogueText { get; set; } = "";

    public static NpcFormModel FromDefinition(
        NpcDefinitionV1 npc)
    {
        return new NpcFormModel
        {
            NpcId = npc.NpcId,
            X = npc.Position.X,
            Y = npc.Position.Y,
            Name = npc.Name,
            DialogueText = npc.DialogueText
        };
    }

    public NpcDefinitionV1 ToDefinition()
    {
        return new NpcDefinitionV1
        {
            NpcId = NpcId,
            Position = new GridPositionV1 { X = X, Y = Y },
            Name = Name,
            DialogueText = DialogueText
        };
    }
}

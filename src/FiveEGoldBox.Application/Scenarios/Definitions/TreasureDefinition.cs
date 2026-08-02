using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// Declares an item/gold reward for content-authoring intent and future use,
/// but v1 has no inventory or currency system anywhere in this engine --
/// collecting a treasure only flips a "collected" flag, it grants nothing.
internal sealed record TreasureDefinition
{
    internal required string TreasureId { get; init; }

    internal required GridPosition Position { get; init; }

    internal string? ItemId { get; init; }

    internal int? GoldPieces { get; init; }
}

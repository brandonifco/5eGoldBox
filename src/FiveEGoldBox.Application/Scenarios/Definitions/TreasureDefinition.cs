using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// Declares an item/gold reward. Collecting the treasure adds GoldPieces to
/// the party's shared currency and, if ItemId is set, adds Quantity (or 1,
/// if Quantity is null) of that item to the party's shared inventory -- see
/// ExplorationRules.CollectTreasure.
internal sealed record TreasureDefinition
{
    internal required string TreasureId { get; init; }

    internal required GridPosition Position { get; init; }

    internal string? ItemId { get; init; }

    internal int? GoldPieces { get; init; }

    /// How many of ItemId the treasure grants. Meaningless without ItemId;
    /// treated as 1 if ItemId is set and this is null.
    internal int? Quantity { get; init; }
}

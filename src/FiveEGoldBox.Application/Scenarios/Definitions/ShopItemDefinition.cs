namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// One good a shop sells. Price is gold pieces only in v1 -- no
/// silver/copper fractions -- matching the campaign ruleset's own
/// equipment prices closely enough not to need denomination math yet.
internal sealed record ShopItemDefinition
{
    internal required string ItemId { get; init; }

    internal required int PriceGoldPieces { get; init; }
}

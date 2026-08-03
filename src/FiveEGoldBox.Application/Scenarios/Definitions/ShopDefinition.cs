namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// A hub-level service the party can spend its shared purse at -- always
/// available whenever the party occupies its location, unlike a decision,
/// which is a one-time choice gated by progress. Deliberately minimal: a
/// flat list of goods, no stock limits, no restocking, no haggling.
internal sealed record ShopDefinition
{
    internal required string ShopId { get; init; }

    internal required string DisplayName { get; init; }

    internal required string LocationId { get; init; }

    internal required IReadOnlyList<ShopItemDefinition> Items { get; init; }
}

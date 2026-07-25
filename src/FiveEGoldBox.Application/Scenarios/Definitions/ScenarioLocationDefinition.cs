namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// A place the party can be. A location either supports grid exploration, in
/// which case it carries a map, or it is a hub the party simply occupies.
internal sealed record ScenarioLocationDefinition
{
    internal required string LocationId { get; init; }

    internal required string DisplayName { get; init; }

    /// Null for hub locations such as an outpost.
    internal ExplorationMapDefinition? ExplorationMap { get; init; }

    /// Progress markers at which the party may be exploring here. Empty for
    /// hubs, and for maps the scenario only opens at particular points.
    internal IReadOnlyList<string> ExplorableProgressIds { get; init; } =
        Array.Empty<string>();
}

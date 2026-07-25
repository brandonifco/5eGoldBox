namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// A complete scenario as authored data rather than code.
///
/// Nothing here describes a party. Party composition is campaign-declared, so a
/// scenario states only what it requires of whoever attempts it, via
/// <see cref="PartyRequirement"/>.
internal sealed record ScenarioDefinition
{
    internal required string ScenarioId { get; init; }

    internal required string DisplayName { get; init; }

    /// The ruleset this scenario's content is authored against.
    internal required string RulesetId { get; init; }

    /// Where a new session begins.
    internal required string StartingLocationId { get; init; }

    internal required ScenarioProgressDefinition Progress { get; init; }

    internal required PartyRequirementDefinition PartyRequirement { get; init; }

    internal required IReadOnlyList<ScenarioLocationDefinition> Locations { get; init; }

    internal required IReadOnlyList<TravelRouteDefinition> Routes { get; init; }

    internal required IReadOnlyList<EncounterDefinition> Encounters { get; init; }

    /// Things in the world the party interacts with to move forward.
    internal required IReadOnlyList<ScenarioTriggerDefinition> Triggers { get; init; }

    /// Choices the party is offered for being somewhere.
    internal required IReadOnlyList<ScenarioDecisionDefinition> Decisions { get; init; }
}

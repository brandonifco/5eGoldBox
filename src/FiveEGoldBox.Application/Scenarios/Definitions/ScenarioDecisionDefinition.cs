namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// A choice the party is offered at a location — taking a commission, refusing
/// it, picking which road to walk.
///
/// Distinct from a trigger: a trigger is something in the world the party
/// interacts with at a particular square, whereas a decision is put to the party
/// simply for being somewhere.
internal sealed record ScenarioDecisionDefinition
{
    internal required string DecisionId { get; init; }

    internal required string DisplayName { get; init; }

    internal required string LocationId { get; init; }

    /// Progress markers from which this decision is offered.
    internal required IReadOnlyList<string> RequiredProgressIds { get; init; }

    internal required IReadOnlyList<ScenarioDecisionOptionDefinition> Options { get; init; }
}

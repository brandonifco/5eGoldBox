using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// Something the party can do at a particular spot that moves the scenario on —
/// pulling a lever, reading an inscription, springing an ambush.
///
/// Conditions are all optional except the location: a trigger with no position
/// applies anywhere in that location.
internal sealed record ScenarioTriggerDefinition
{
    internal required string TriggerId { get; init; }

    internal required string DisplayName { get; init; }

    internal required string LocationId { get; init; }

    internal ExplorationFloor? Floor { get; init; }

    internal GridPosition? Position { get; init; }

    /// Some interactions only work when facing them.
    internal ExplorationFacing? RequiredFacing { get; init; }

    /// Progress markers from which this trigger is available.
    internal required IReadOnlyList<string> RequiredProgressIds { get; init; }

    /// Progress the scenario moves to when the trigger fires.
    internal required string ResultingProgressId { get; init; }

    /// Encounter this trigger starts, if any.
    internal string? EncounterId { get; init; }
}

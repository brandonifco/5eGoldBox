using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// A one-way link between floors. Authoring both directions is deliberate:
/// stairs that only go up are a legitimate thing to want.
internal sealed record StairDefinition
{
    internal required GridPosition Position { get; init; }

    internal required string DestinationFloor { get; init; }

    internal required GridPosition DestinationPosition { get; init; }
}

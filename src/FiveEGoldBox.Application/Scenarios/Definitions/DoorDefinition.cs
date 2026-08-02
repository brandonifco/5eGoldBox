using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// A door blocks its own tile until opened; a secret door additionally
/// requires being found (revealed) before it can be opened at all. A locked
/// door has no unlock path in v1 -- it is a permanent binary blocker, not a
/// puzzle with a key waiting to be authored later.
internal sealed record DoorDefinition
{
    internal required string DoorId { get; init; }

    internal required GridPosition Position { get; init; }

    internal required bool IsSecret { get; init; }

    internal required bool IsLocked { get; init; }
}

using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal sealed record EncounterCombatMovementDestinationOption
{
    public required GridPosition Destination { get; init; }

    public required IReadOnlyList<GridPosition> Path { get; init; }

    public required int MovementSpentFeet { get; init; }
}

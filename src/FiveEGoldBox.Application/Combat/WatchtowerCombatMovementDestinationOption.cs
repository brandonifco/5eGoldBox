using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

public sealed record WatchtowerCombatMovementDestinationOption
{
    public required GridPosition Destination { get; init; }

    public required IReadOnlyList<GridPosition> Path { get; init; }

    public required int MovementSpentFeet { get; init; }
}

using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// Where a combatant moved and what it cost.
public sealed record CombatMovementStepDetail
{
    internal CombatMovementStepDetail(
        GridPosition startingPosition,
        GridPosition endingPosition,
        IReadOnlyList<GridPosition> path,
        int movementSpentFeet)
    {
        ArgumentNullException.ThrowIfNull(path);

        StartingPosition = startingPosition;
        EndingPosition = endingPosition;
        Path = Array.AsReadOnly(path.ToArray());
        MovementSpentFeet = movementSpentFeet;
    }

    public GridPosition StartingPosition { get; }

    public GridPosition EndingPosition { get; }

    public IReadOnlyList<GridPosition> Path { get; }

    public int MovementSpentFeet { get; }
}

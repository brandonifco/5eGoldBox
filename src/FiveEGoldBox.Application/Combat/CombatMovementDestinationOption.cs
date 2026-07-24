using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

public sealed record CombatMovementDestinationOption
{
    internal CombatMovementDestinationOption(
        GridPosition destination,
        IReadOnlyList<GridPosition> path,
        int movementCostFeet)
    {
        ArgumentNullException.ThrowIfNull(path);

        GridPosition[] protectedPath = path.ToArray();

        if (protectedPath.Length == 0)
        {
            throw new ArgumentException(
                "A combat movement destination requires a nonempty path.",
                nameof(path));
        }

        if (protectedPath[^1] != destination)
        {
            throw new ArgumentException(
                "A combat movement path must end at its destination.",
                nameof(path));
        }

        if (movementCostFeet <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(movementCostFeet),
                movementCostFeet,
                "Combat movement cost must be positive.");
        }

        Destination = destination;
        Path = Array.AsReadOnly(protectedPath);
        MovementCostFeet = movementCostFeet;
    }

    public GridPosition Destination { get; }

    public IReadOnlyList<GridPosition> Path { get; }

    public int MovementCostFeet { get; }
}

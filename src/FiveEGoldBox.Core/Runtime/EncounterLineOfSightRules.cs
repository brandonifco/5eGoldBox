namespace FiveEGoldBox.Core.Runtime;

public static class EncounterLineOfSightRules
{
    public static EncounterLineOfSightResult Evaluate(
        EncounterBattlefieldState battlefield,
        GridPosition sourcePosition,
        GridPosition targetPosition)
    {
        ArgumentNullException.ThrowIfNull(battlefield);

        EncounterRules.ValidateBattlefield(
            battlefield);

        EncounterRules.ValidatePositionWithinBattlefield(
            battlefield,
            sourcePosition,
            nameof(sourcePosition));

        EncounterRules.ValidatePositionWithinBattlefield(
            battlefield,
            targetPosition,
            nameof(targetPosition));

        HashSet<GridPosition> blockedPositions =
            battlefield.BlockedPositions.ToHashSet();

        if (blockedPositions.Contains(
            sourcePosition))
        {
            throw new ArgumentException(
                $"Source position '{sourcePosition}' cannot be blocked.",
                nameof(sourcePosition));
        }

        if (blockedPositions.Contains(
            targetPosition))
        {
            throw new ArgumentException(
                $"Target position '{targetPosition}' cannot be blocked.",
                nameof(targetPosition));
        }

        GridPosition[] intermediatePositions =
            GetIntermediatePositions(
                sourcePosition,
                targetPosition);

        GridPosition? blockingPosition = null;

        foreach (GridPosition position
            in intermediatePositions)
        {
            if (!blockedPositions.Contains(position))
            {
                continue;
            }

            blockingPosition = position;
            break;
        }

        return new EncounterLineOfSightResult
        {
            SourcePosition = sourcePosition,
            TargetPosition = targetPosition,
            HasLineOfSight =
                blockingPosition is null,
            BlockingPosition =
                blockingPosition,
            IntermediatePositions =
                Array.AsReadOnly(
                    intermediatePositions)
        };
    }

    private static GridPosition[]
        GetIntermediatePositions(
            GridPosition sourcePosition,
            GridPosition targetPosition)
    {
        if (sourcePosition == targetPosition)
        {
            return [];
        }

        int deltaX =
            targetPosition.X
            - sourcePosition.X;

        int deltaY =
            targetPosition.Y
            - sourcePosition.Y;

        int absoluteDeltaX =
            Math.Abs(deltaX);

        int absoluteDeltaY =
            Math.Abs(deltaY);

        int stepX = Math.Sign(deltaX);
        int stepY = Math.Sign(deltaY);

        int currentX = sourcePosition.X;
        int currentY = sourcePosition.Y;

        int crossedVerticalBoundaries = 0;
        int crossedHorizontalBoundaries = 0;

        List<GridPosition> positions = [];

        while (currentX != targetPosition.X
            || currentY != targetPosition.Y)
        {
            bool moveHorizontally;
            bool moveVertically;

            if (absoluteDeltaX == 0)
            {
                moveHorizontally = false;
                moveVertically = true;
            }
            else if (absoluteDeltaY == 0)
            {
                moveHorizontally = true;
                moveVertically = false;
            }
            else
            {
                Int128 nextVerticalBoundary =
                    (((Int128)
                        crossedVerticalBoundaries
                        * 2) + 1)
                    * absoluteDeltaY;

                Int128 nextHorizontalBoundary =
                    (((Int128)
                        crossedHorizontalBoundaries
                        * 2) + 1)
                    * absoluteDeltaX;

                moveHorizontally =
                    nextVerticalBoundary
                    <= nextHorizontalBoundary;

                moveVertically =
                    nextHorizontalBoundary
                    <= nextVerticalBoundary;
            }

            if (moveHorizontally)
            {
                currentX += stepX;
                crossedVerticalBoundaries++;
            }

            if (moveVertically)
            {
                currentY += stepY;
                crossedHorizontalBoundaries++;
            }

            if (currentX == targetPosition.X
                && currentY == targetPosition.Y)
            {
                break;
            }

            positions.Add(
                new GridPosition(
                    currentX,
                    currentY));
        }

        return positions.ToArray();
    }
}

namespace FiveEGoldBox.Core.Runtime;

public static class EncounterCoverRules
{
    private const int HalfCoverBonus = 2;
    private const int ThreeQuartersCoverBonus = 5;

    public static EncounterCoverEvaluation Evaluate(
        EncounterBattlefieldState battlefield,
        EncounterLineOfSightResult lineOfSight)
    {
        ArgumentNullException.ThrowIfNull(battlefield);
        ArgumentNullException.ThrowIfNull(lineOfSight);

        EncounterRules.ValidateBattlefield(battlefield);
        ValidateLineOfSight(
            battlefield,
            lineOfSight);

        Dictionary<GridPosition, EncounterCoverLevel>
            coverLevelsByPosition =
                battlefield.CoverPositions.ToDictionary(
                    coverPosition =>
                        coverPosition.Position,
                    coverPosition =>
                        coverPosition.CoverLevel);

        EncounterCoverLevel strongestCoverLevel =
            EncounterCoverLevel.None;

        GridPosition? strongestCoverPosition = null;

        foreach (GridPosition position
            in lineOfSight.IntermediatePositions)
        {
            if (!coverLevelsByPosition.TryGetValue(
                position,
                out EncounterCoverLevel coverLevel))
            {
                continue;
            }

            if (coverLevel <= strongestCoverLevel)
            {
                continue;
            }

            strongestCoverLevel = coverLevel;
            strongestCoverPosition = position;
        }

        int coverBonus =
            GetCoverBonus(strongestCoverLevel);

        return new EncounterCoverEvaluation
        {
            CoverLevel = strongestCoverLevel,
            ArmorClassBonus = coverBonus,
            DexteritySavingThrowBonus = coverBonus,
            CoverPosition = strongestCoverPosition
        };
    }

    private static void ValidateLineOfSight(
        EncounterBattlefieldState battlefield,
        EncounterLineOfSightResult lineOfSight)
    {
        ArgumentNullException.ThrowIfNull(
            lineOfSight.IntermediatePositions);

        EncounterLineOfSightResult expected =
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                lineOfSight.SourcePosition,
                lineOfSight.TargetPosition);

        if (!expected.HasLineOfSight)
        {
            throw new ArgumentException(
                "Cover cannot be evaluated when line of sight is blocked.",
                nameof(lineOfSight));
        }

        if (!lineOfSight.HasLineOfSight
            || lineOfSight.BlockingPosition is not null
            || !expected.IntermediatePositions.SequenceEqual(
                lineOfSight.IntermediatePositions))
        {
            throw new ArgumentException(
                "Line-of-sight result does not match the battlefield.",
                nameof(lineOfSight));
        }
    }

    private static int GetCoverBonus(
        EncounterCoverLevel coverLevel)
    {
        return coverLevel switch
        {
            EncounterCoverLevel.None => 0,

            EncounterCoverLevel.Half =>
                HalfCoverBonus,

            EncounterCoverLevel.ThreeQuarters =>
                ThreeQuartersCoverBonus,

            _ => throw new ArgumentOutOfRangeException(
                nameof(coverLevel),
                coverLevel,
                "Unsupported encounter cover level.")
        };
    }
}

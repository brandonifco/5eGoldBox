namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterCoverEvaluation
{
    public required EncounterCoverLevel CoverLevel { get; init; }

    public required int ArmorClassBonus { get; init; }

    public required int DexteritySavingThrowBonus { get; init; }

    public GridPosition? CoverPosition { get; init; }
}

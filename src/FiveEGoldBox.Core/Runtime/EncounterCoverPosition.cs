namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterCoverPosition
{
    public required GridPosition Position { get; init; }

    public required EncounterCoverLevel CoverLevel { get; init; }
}

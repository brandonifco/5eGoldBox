namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterActionCandidate
{
    public required string ActionOptionId { get; init; }

    public required string ActorCombatantId { get; init; }

    public required EncounterActionTiming Timing { get; init; }
}
